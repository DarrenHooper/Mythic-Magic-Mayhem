// Adapted from Prestige Plus HarmonyFix/ShowTHC.cs
// https://github.com/YLMstring/Prestige-Plus
// Copyright (c) 2023 YLMstring, MIT License.

using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Root;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.UI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TurnBased.Controllers;
using UnityEngine;

namespace MythicMagicMayhem.Mechanics
{
    /// <summary>
    /// Cursor estimate for turn-based attacks and spells. Collects the same bonuses the combat
    /// log uses, then stops before any d20 is rolled.
    /// </summary>
    [HarmonyPatch(typeof(CursorController), "SetCursor", typeof(CursorRoot.CursorType), typeof(Sprite), typeof(bool), typeof(bool), typeof(string), typeof(float))]
    internal static class EstimatedHitChance
    {
        const int MaxShownSaves = 3;
        static readonly Dictionary<BlueprintAbility, SaveInfo[]> SaveCache = new();
        static readonly HashSet<string> LoggedFailures = new();

        [HarmonyPrefix]
        static void Prefix(CursorRoot.CursorType cursorType, ref string text, CursorController __instance)
        {
            try
            {
                if (Game.Instance == null || !CombatController.IsInTurnBasedCombat() || !Main.EstimatedHitChance)
                    return;

                var attacker = CombatController.SelectedUnit;
                if (attacker == null)
                    return;

                var turn = Game.Instance.TurnBasedCombatController?.CurrentTurn;
                string estimate = null;
                if (cursorType == CursorRoot.CursorType.AttackCursor || cursorType == CursorRoot.CursorType.RangeAttackCursor)
                {
                    var target = __instance.m_HoveredUnitData ?? turn?.m_HighlightedUnit;
                    var fullAttack = text != null && text.IndexOf('×') >= 0;
                    estimate = AttackEstimate(attacker, target, fullAttack);
                }
                else if (cursorType == CursorRoot.CursorType.CastCursor)
                {
                    estimate = SpellEstimate(attacker, turn?.CurrentAbility, turn?.m_HighlightedUnit);
                }

                if (string.IsNullOrEmpty(estimate))
                    return;

                var largeEstimate = "<size=200%>" + estimate + "</size>";
                text = string.IsNullOrEmpty(text) ? largeEstimate : largeEstimate + "  " + text;
            }
            catch (Exception ex)
            {
                var key = ex.GetType().Name + ": " + ex.Message;
                if (LoggedFailures.Add(key))
                    Main.Logger.Error("Estimated hit chance failed.", ex);
            }
        }

        static string AttackEstimate(UnitEntityData attacker, UnitEntityData target, bool fullAttack)
        {
            if (target == null)
                return null;

            var weapon = attacker.Body.PrimaryHand.MaybeWeapon ?? attacker.Body.EmptyHandWeapon;
            if (weapon == null)
                return null;

            var parts = new List<string>(2);
            if (TryHitChance(attacker, target, weapon, null, out var hit))
                parts.Add((fullAttack ? "First hit " : "Hit ") + hit + "%");

            var conceal = ConcealmentMiss(attacker, target);
            if (conceal > 0)
                parts.Add("Conceal " + conceal + "%");

            return parts.Count == 0 ? null : string.Join("  ", parts);
        }

        static string SpellEstimate(UnitEntityData attacker, AbilityData ability, UnitEntityData target)
        {
            if (ability?.Blueprint == null || target == null)
                return null;

            AbilityExecutionContext context;
            try
            {
                context = ability.CreateExecutionContext(new TargetWrapper(target));
            }
            catch (Exception ex)
            {
                var key = "context: " + ex.GetType().Name + ": " + ex.Message;
                if (LoggedFailures.Add(key))
                    Main.Logger.Error("Estimated hit chance could not preview " + ability.Blueprint.name + ".", ex);
                return null;
            }

            var blueprint = ability.GetDeliverBlueprint() ?? ability.Blueprint;
            var parts = new List<string>(4);
            var attackRoll = false;
            using (var held = SpellWeapon(attacker, ability, blueprint))
            {
                if (held.Weapon != null && TryHitChance(attacker, target, held.Weapon, held.AttackStat, out var hit))
                {
                    parts.Add("Hit " + hit + "%");
                    attackRoll = true;
                }
            }

            foreach (var save in SavesFor(ability))
            {
                if (!TrySaveFail(target, context, save, out var fail, out var persistent))
                    continue;

                var label = new StringBuilder();
                label.Append(SaveName(save.Type)).Append(" fail ").Append(fail).Append('%');
                if (persistent)
                    label.Append(", persistent");
                parts.Add(label.ToString());
            }

            if (TrySpellResistance(attacker, target, ability, context, blueprint, out var resistance, out var immune))
                parts.Add(immune ? "SR immune" : "SR " + resistance + "%");

            if (attackRoll)
            {
                var conceal = ConcealmentMiss(attacker, target);
                if (conceal > 0)
                    parts.Add("Conceal " + conceal + "%");
            }

            return parts.Count == 0 ? null : string.Join("  ", parts);
        }

        static bool TryHitChance(UnitEntityData attacker, UnitEntityData target, ItemEntityWeapon weapon, StatType? attackStat, out int chance)
        {
            chance = 0;
            if (weapon?.Blueprint == null)
                return false;

            var attack = new RuleCalculateAttackBonus(attacker, target, weapon, 0)
            {
                DisableBattleLogSelf = true
            };
            var armor = new RuleCalculateAC(attacker, target, weapon.Blueprint.AttackType)
            {
                DisableBattleLogSelf = true
            };

            if (attackStat.HasValue)
            {
                using (ContextData<AttackBonusStatReplacement>.Request().Setup(attackStat.Value))
                {
                    Rulebook.Trigger(attack);
                    Rulebook.Trigger(armor);
                }
            }
            else
            {
                Rulebook.Trigger(attack);
                Rulebook.Trigger(armor);
            }

            chance = OpposedD20(attack.Result, armor.Result);
            return true;
        }

        /// <summary>
        /// Chance the target fails the save. Persistent spell rolls again after a success,
        /// so the spell lands unless both rolls are passed.
        /// </summary>
        static bool TrySaveFail(UnitEntityData target, AbilityExecutionContext context, SaveInfo save, out int fail, out bool persistent)
        {
            fail = 0;
            persistent = context.HasMetamagic(Metamagic.Persistent);
            var dc = context.Params.DC;
            if (save.Custom != null && save.Custom.HasCustomDC)
            {
                try
                {
                    dc = save.Custom.CustomDC.Calculate(context);
                }
                catch (Exception ex)
                {
                    var key = "dc: " + ex.GetType().Name + ": " + ex.Message;
                    if (LoggedFailures.Add(key))
                        Main.Logger.Error("Estimated hit chance could not read a custom save DC.", ex);
                }
            }

            var rule = new RuleSavingThrow(target, save.Type, dc)
            {
                Reason = context,
                Buff = SaveBuff(save)
            };

            var bonus = 0;
            var autoPass = false;
            Preview(rule, () =>
            {
                autoPass = rule.AutoPass;
                bonus = SaveBonus(target, rule) + rule.SuccessBonus;
                dc = rule.DifficultyClass;
            });

            if (autoPass)
            {
                fail = 0;
                persistent = false;
                return true;
            }

            var pass = OpposedD20(bonus, dc);
            fail = 100 - pass;
            if (!persistent)
                return true;

            fail = (int)Math.Round((10000 - pass * (double)pass) / 100.0);
            return true;
        }

        static int SaveBonus(UnitEntityData target, RuleSavingThrow rule)
        {
            var statType = RuleSavingThrow.GetStatFromSavingThrow(rule.Type);
            var stat = target.Stats.GetStat(statType);
            var replacement = target.Get<UnitPartSavingThrowsReplace>();
            if (replacement != null && replacement.SavingThrowStat == statType)
            {
                var alternative = target.Stats.GetStat(replacement.ReplaceToStat);
                if (alternative.ModifiedValue > stat.ModifiedValue)
                    stat = alternative;
            }

            using (stat.GetTemporaryModifiersScope(rule.AllBonuses))
                return stat.ModifiedValue;
        }

        static BlueprintBuff SaveBuff(SaveInfo save)
        {
            var actions = save.Effect?.Actions ?? save.Custom?.Actions;
            return actions == null ? null : ContextActionSavingThrow.FindApplyBuffAction(actions)?.Buff;
        }

        static bool TrySpellResistance(UnitEntityData attacker, UnitEntityData target, AbilityData ability, AbilityExecutionContext context, BlueprintAbility blueprint, out int chance, out bool immune)
        {
            chance = 0;
            immune = false;
            // AbilityData resolves variants, sticky touch, and other delivery abilities. Checking
            // only the delivery blueprint can incorrectly report that an SR-applicable spell ignores SR.
            if (ability == null || !ability.SpellResistance)
                return false;
            if (blueprint.IgnoreSpellResistanceForAlly && !attacker.IsEnemy(target))
                return false;
            if ((bool)attacker.Descriptor.State.Features.PrimalMagicEssence)
                return false;

            var part = target.Get<UnitPartSpellResistance>();
            if (part == null)
                return false;

            immune = part.IsImmune(context);
            if (immune)
                return true;

            var resistance = part.GetValue(context);
            if (resistance <= 0)
                return false;

            var rule = new RuleSpellResistanceCheck(context, target);
            var extraPenetration = 0;
            Preview(rule, () => extraPenetration = (int)rule.m_AdditionalSpellPenetration);

            var penetration = context.Params.CasterLevel + extraPenetration;
            var replacement = attacker.Get<UnitPartSpellResistanceCheckReplace>();
            if (replacement != null)
            {
                var fromStat = replacement.Progression.CalculateValue(attacker.Stats.GetStat(replacement.StatType).ModifiedValue);
                if (fromStat > context.Params.CasterLevel)
                    penetration = fromStat + extraPenetration;
            }

            chance = CasterLevelCheck(penetration, resistance);
            // Still show 100%: it confirms that the target has SR but the caster always beats it.
            return true;
        }

        static int ConcealmentMiss(UnitEntityData attacker, UnitEntityData target)
        {
            var concealment = UnitPartConcealment.Calculate(attacker, target, attack: true);
            if (concealment == Concealment.None)
                return 0;

            var rule = new RuleConcealmentCheck(attacker, target, attack: true);
            var miss = concealment == Concealment.Partial ? 20 : 50;
            Preview(rule, () =>
            {
                rule.missChance.Set(miss);
                miss = rule.missChance.Calculate();
            });
            return Math.Max(0, miss);
        }

        /// <summary>
        /// Runs the bonus-collection phase of a rule and then drops any temporary modifiers it added.
        /// Does not call OnTrigger, so nothing is rolled and no "after the roll" listeners run.
        /// </summary>
        static void Preview(RulebookEvent rule, Action read)
        {
            var context = Game.Instance.Rulebook.Context;
            rule.DisableBattleLogSelf = true;
            context.PushEvent(rule);
            try
            {
                try
                {
                    RulebookEventBus.OnEventAboutToTrigger(rule);
                    read();
                }
                finally
                {
                    rule.OnDidTrigger();
                }
            }
            finally
            {
                context.PopEvent(rule);
            }
        }

        static HeldWeapon SpellWeapon(UnitEntityData attacker, AbilityData ability, BlueprintAbility blueprint)
        {
            var touch = blueprint.GetComponent<AbilityDeliverTouch>();
            if (touch == null)
                touch = blueprint.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility?.GetComponent<AbilityDeliverTouch>();
            if (touch != null)
            {
                var weaponBlueprint = ability.HasMetamagic(Metamagic.Reach)
                    ? BlueprintRoot.Instance.SystemMechanics.RayWeapon
                    : touch.TouchWeapon;
                return HeldWeapon.Created(weaponBlueprint);
            }

            var delivered = blueprint.GetComponent<AbilityDeliveredByWeapon>();
            if (delivered != null)
                return HeldWeapon.Borrowed(attacker.Body.PrimaryHand.MaybeWeapon ?? attacker.Body.EmptyHandWeapon);

            var projectile = blueprint.GetComponent<AbilityDeliverProjectile>();
            if (projectile == null || !projectile.NeedAttackRoll || projectile.Type == AbilityProjectileType.Line || projectile.Type == AbilityProjectileType.Cone)
                return default;

            var stat = projectile.ReplaceAttackRollBonusStat ? projectile.AttackRollBonusStat : (StatType?)null;
            if (stat.HasValue && (bool)attacker.Descriptor.State.Features.DomainsByIntelligenceFeature)
                stat = StatType.Intelligence;

            if (projectile.IsHandOfTheApprentice)
                return HeldWeapon.Borrowed(attacker.GetThreatHandMelee()?.MaybeWeapon ?? attacker.Body.PrimaryHand.MaybeWeapon, stat);

            if (AbilityKineticist.IsArcherBlast(ability))
                return HeldWeapon.Borrowed(attacker.Body.PrimaryHand.MaybeWeapon, stat);

            return HeldWeapon.Created(projectile.Weapon ?? BlueprintRoot.Instance.SystemMechanics.RayWeapon, stat);
        }

        static IEnumerable<SaveInfo> SavesFor(AbilityData ability)
        {
            var found = new List<SaveInfo>();
            if (ability.MagicHackData != null)
                found.Add(new SaveInfo(ability.MagicHackData.SavingThrowType, null, null));

            AddSaves(found, ability.Blueprint);
            AddSaves(found, ability.GetDeliverBlueprint());
            AddSaves(found, ability.Blueprint.GetComponent<AbilityEffectStickyTouch>()?.TouchDeliveryAbility);
            var hack = ability.MagicHackData;
            if (hack != null)
            {
                AddSaves(found, hack.Spell1);
                AddSaves(found, hack.Spell2);
                AddSaves(found, hack.DeliverBlueprint);
            }

            var distinct = new List<SaveInfo>();
            foreach (var save in found)
            {
                if (save.Type == SavingThrowType.Unknown)
                    continue;

                var existing = distinct.FindIndex(other => other.Type == save.Type);
                if (existing < 0)
                    distinct.Add(save);
                else if (distinct[existing].Custom == null && save.Custom != null)
                    distinct[existing] = save;

                if (distinct.Count == MaxShownSaves)
                    break;
            }

            return distinct;
        }

        static void AddSaves(List<SaveInfo> found, BlueprintAbility blueprint)
        {
            if (blueprint == null)
                return;
            if (!SaveCache.TryGetValue(blueprint, out var cached))
            {
                var scanned = new List<SaveInfo>();
                var seen = new HashSet<object>();
                foreach (var component in blueprint.GetComponents<BlueprintComponent>())
                    Walk(component, scanned, seen, 0);
                cached = scanned.ToArray();
                SaveCache[blueprint] = cached;
            }

            found.AddRange(cached);
        }

        static void Walk(object node, List<SaveInfo> found, HashSet<object> seen, int depth)
        {
            if (node == null || depth > 8 || !seen.Add(node))
                return;

            switch (node)
            {
                case ContextActionSavingThrow save:
                    found.Add(new SaveInfo(save.Type, save, null));
                    Walk(save.Actions, found, seen, depth + 1);
                    return;
                case AbilityEffectRunAction effect:
                    found.Add(new SaveInfo(effect.SavingThrowType, null, effect));
                    Walk(effect.Actions, found, seen, depth + 1);
                    return;
                case ActionList list:
                    if (list.Actions == null)
                        return;
                    foreach (var action in list.Actions)
                        Walk(action, found, seen, depth + 1);
                    return;
            }

            if (node is not GameAction && node is not BlueprintComponent)
                return;

            foreach (var field in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!typeof(ActionList).IsAssignableFrom(field.FieldType))
                    continue;
                try
                {
                    Walk(field.GetValue(node), found, seen, depth + 1);
                }
                catch (Exception ex)
                {
                    var key = "walk: " + ex.GetType().Name + ": " + ex.Message;
                    if (LoggedFailures.Add(key))
                        Main.Logger.Error("Estimated hit chance could not read a spell action.", ex);
                }
            }
        }

        /// <summary>
        /// Chance a d20 + bonus meets the opposing number. A natural 1 fails and a natural 20 succeeds.
        /// </summary>
        static int OpposedD20(int bonus, int opposingNumber)
        {
            var chance = 105 - 5 * (opposingNumber - bonus);
            if (chance < 5)
                return 5;
            if (chance > 95)
                return 95;
            return chance;
        }

        /// <summary>
        /// Caster level checks have no automatic success or failure.
        /// </summary>
        static int CasterLevelCheck(int bonus, int difficulty)
        {
            var needed = difficulty - bonus;
            if (needed <= 1)
                return 100;
            if (needed > 20)
                return 0;
            return (21 - needed) * 5;
        }

        static string SaveName(SavingThrowType type)
        {
            switch (type)
            {
                case SavingThrowType.Fortitude: return "Fort";
                case SavingThrowType.Reflex: return "Ref";
                case SavingThrowType.Will: return "Will";
                default: return "Save";
            }
        }

        readonly struct SaveInfo
        {
            public readonly SavingThrowType Type;
            public readonly ContextActionSavingThrow Custom;
            public readonly AbilityEffectRunAction Effect;

            public SaveInfo(SavingThrowType type, ContextActionSavingThrow custom, AbilityEffectRunAction effect)
            {
                Type = type;
                Custom = custom;
                Effect = effect;
            }
        }

        readonly struct HeldWeapon : IDisposable
        {
            public readonly ItemEntityWeapon Weapon;
            public readonly StatType? AttackStat;
            readonly bool m_Dispose;

            HeldWeapon(ItemEntityWeapon weapon, StatType? attackStat, bool dispose)
            {
                Weapon = weapon;
                AttackStat = attackStat;
                m_Dispose = dispose;
            }

            public static HeldWeapon Borrowed(ItemEntityWeapon weapon, StatType? attackStat = null)
            {
                return new HeldWeapon(weapon, attackStat, false);
            }

            public static HeldWeapon Created(BlueprintItemWeapon blueprint, StatType? attackStat = null)
            {
                return new HeldWeapon(blueprint != null ? blueprint.CreateEntity<ItemEntityWeapon>() : null, attackStat, true);
            }

            public void Dispose()
            {
                if (m_Dispose)
                    Weapon?.Dispose();
            }
        }
    }
}
