using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.ContextEx;
using BlueprintCore.Utils.Types;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Enums.Damage;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Abilities.Components.Base;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Mechanics.Components;
using Kingmaker.UnitLogic.Mechanics.Conditions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using MythicMagicMayhem.Mechanics;
using MythicMagicMayhem.Menu;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MythicMagicMayhem.Azata
{
    internal static class AzataSpellBalance
    {
        private static bool Subscribed;

        public static void Patch()
        {
            if (Subscribed) return;
            Subscribed = true;
            EventBus.Subscribe(new RejuvenatingPoemWearinessReset());
            AbilityConfigurator.For(AbilityRefs.VoiceOfRenewal.Reference.Get())
                .SetActionType(UnitCommand.CommandType.Swift)
                .Configure();
            PatchWaterPush();
            PatchChaoticHealing();
            PatchWaterfall();
            PatchDeadlyBeauty();
            PatchDeadlyBeautyCommunal();
            PatchColorOfMagic();
            PatchElusiveNature();
            PatchRepulsiveNature();
            PatchFieldOfFlowers();
            PatchUnbreakableBond();
            PatchRainbowStarfall();
            PatchRainbowArrows();
            PatchBurstOfSonicEnergy();
            PatchSuddenSquall();
            PatchDragonDance();
            PatchWaterTorrent();
            PatchWindsOfTheFall();
            Main.Logger.Info("Azata spell balance: Rejuvenating Poem restarts travel weariness. Voice of Renewal is a swift action. Water Push hits enemies for 1d6 per 3 caster levels. Chaotic Healing chains 1d20 per 5 caster levels. Waterfall always applies cold vulnerability. Deadly Beauty is a touch spell and retaliates against a successful melee hit. Deadly Beauty (Communal) is a 5th-level spell that grants Deadly Beauty to allies within 30 feet. The Color of Magic is a 10th-level rainbow burst that damages and debilitates enemies within 30 feet. Elusive Nature turns the critical hit that starts blink into a miss. Repulsive Nature affects enemies only. Field of Flowers entangles on a Reflex save, sleeps on a Will save, and protects allies from nausea. Unbreakable Bond has a 30-foot radius. Rainbow Starfall calls later stars as a swift action. Rainbow Arrows deals the strongest energy for 1d8 per caster level and its Reflex save halves the damage. Burst of Sonic Energy hits enemies only, and adjacent enemies are flat-footed for 1 round. Sudden Squall is a 30-foot cone. Primordial Dragon Dance deals d8s. Water Torrent deals 1d6 per two caster levels and its bludgeoning weakness ignores the Fortitude save. Winds of the Fall hits enemies for 1d12 per two caster levels.");
        }

        /// <summary>
        /// The cone delivers to every creature in it. CanTargetFriends is ignored unless Selective is applied,
        /// so allies have to be rejected in the delivery check.
        /// </summary>
        private static void PatchWaterPush()
        {
            var ability = AbilityRefs.WaterPush.Reference.Get();
            var rank = ability.GetComponent<ContextRankConfig>();
            if (rank == null)
            {
                Main.Logger.Error("Water Push is missing its caster level rank.");
            }
            else
            {
                rank.m_StepLevel = 3;
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-water-push-desc",
                "The Azata creates a 30-foot cone of water that deals {g|Encyclopedia:Dice}1d6{/g} {g|Encyclopedia:Damage}damage{/g} per three {g|Encyclopedia:Caster_Level}caster levels{/g}. Allies are not affected. Every affected enemy must succeed at a {g|Encyclopedia:Saving_Throw}Fortitude saving throw{/g} or be pushed away 10 feet and knocked prone. If the affected creature succeeds at its saving throw, it is only pushed away 5 feet and becomes {g|ConditionSlowed}slowed{/g} for 1 {g|Encyclopedia:Combat_Round}round{/g}.");

            new Harmony("MythicMagicMayhem.WaterPush").Patch(
                AccessTools.Method(typeof(AbilityDeliverProjectile), "CanTargetUnit"),
                prefix: new HarmonyMethod(typeof(AzataSpellBalance), nameof(WaterPushEnemiesOnly)));
        }

        private static bool WaterPushEnemiesOnly(AbilityExecutionContext context, UnitEntityData unit, ref bool __result)
        {
            if (context?.AbilityBlueprint != AbilityRefs.WaterPush.Reference.Get())
                return true;

            var caster = context.MaybeCaster;
            __result = caster != null && caster.IsEnemy(unit);
            return false;
        }

        /// <summary>
        /// Same delivery as Chain Lightning: the clicked creature is the first target, and later
        /// targets are the closest valid creatures within 30 feet of that creature.
        /// </summary>
        private static void PatchChaoticHealing()
        {
            var ability = AbilityRefs.ChaoticHealing.Reference.Get();
            AbilityConfigurator.For(ability)
                .SetRange(AbilityRange.Long)
                .RemoveComponents(component => component is AbilityTargetsAround)
                .AddAbilityDeliverChain(
                    projectile: ProjectileRefs.BuffRay00_Conjuration.ToString(),
                    projectileFirst: ProjectileRefs.BuffRay00_Conjuration.ToString(),
                    radius: 30.Feet(),
                    targetDead: false,
                    targetsCount: ContextValues.Rank(AbilityRankType.ProjectilesCount),
                    targetType: TargetType.Ally,
                    usedTargetsAgain: false,
                    ignoreFirst: false)
                .AddContextRankConfig(ContextRankConfigs.CasterLevel(type: AbilityRankType.DamageDice).WithDivStepProgression(5))
                .AddContextRankConfig(ContextRankConfigs.CasterLevel(type: AbilityRankType.ProjectilesCount, min: 1).WithDivStepProgression(2))
                .Configure();

            var heal = FindAction<ContextActionHealTarget>(ability.GetComponent<AbilityEffectRunAction>()?.Actions, new HashSet<object>());
            if (heal == null)
            {
                Main.Logger.Error("Chaotic Healing is missing its heal action.");
            }
            else
            {
                heal.Value = ContextDice.Value(
                    DiceType.D20,
                    ContextValues.Rank(AbilityRankType.DamageDice),
                    ContextValues.Constant(0));
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-chaotic-healing-desc",
                "The Azata targets an ally and channels positive energy that {g|Encyclopedia:Healing}cures{/g} that ally, then chains to the nearest allies within 30 feet of them. It affects one target per two {g|Encyclopedia:Caster_Level}caster levels{/g}, including the first ally, for {g|Encyclopedia:Dice}1d20{/g} points of {g|Encyclopedia:Damage}damage{/g} per five caster levels. Each target is randomly granted an effect from one of the Azata's following {g|Encyclopedia:Spell}spells{/g}: rejuvenating poem, optimistic smile, or believe in yourself.");
        }

        /// <summary>
        /// The save still prevents the prone effect. Cold vulnerability is on both save results.
        /// </summary>
        private static void PatchWaterfall()
        {
            var ability = AbilityRefs.Waterfall.Reference.Get();
            var saved = FindAction<ContextActionConditionalSaved>(
                ability.GetComponent<AbilityEffectRunAction>()?.Actions,
                new HashSet<object>());
            var vulnerability = saved?.Failed.Actions?
                .OfType<ContextActionApplyBuff>()
                .FirstOrDefault(action => action.Buff != null && action.Buff.name == "WaterfallColdBuff");
            if (saved == null || vulnerability == null)
            {
                Main.Logger.Error("Waterfall is missing its cold vulnerability effect.");
                return;
            }

            saved.Succeed ??= new ActionList();
            var succeed = saved.Succeed.Actions ?? Array.Empty<GameAction>();
            if (!succeed.Contains(vulnerability))
                saved.Succeed.Actions = succeed.Concat(new GameAction[] { vulnerability }).ToArray();

            ability.m_Description = Helpers.CreateString(
                "mmm-waterfall-desc",
                "The Azata creates a waterfall that deals {g|Encyclopedia:Dice}1d10{/g} {g|Encyclopedia:Damage_Type}bludgeoning damage{/g} per four {g|Encyclopedia:Caster_Level}caster levels{/g}. The affected creature becomes {g|Encyclopedia:Energy_Vulnerability}vulnerable{/g} to cold for 1 minute per caster level, and must succeed at a {g|Encyclopedia:Saving_Throw}Fortitude saving throw{/g} or be knocked prone for 1 {g|Encyclopedia:Combat_Round}round{/g}.");
        }

        private static void PatchDeadlyBeauty()
        {
            var ability = AbilityRefs.DeadlyBeauty.Reference.Get();
            AbilityConfigurator.For(ability)
                .SetRange(AbilityRange.Touch)
                .AllowTargeting(point: false, enemies: false, friends: true, self: true)
                .Configure();

            var trigger = BuffRefs.DeadlyBeautyBuff.Reference.Get().GetComponent<AddTargetAttackWithWeaponTrigger>();
            if (trigger == null)
            {
                Main.Logger.Error("Deadly Beauty is missing its retaliation trigger.");
            }
            else
            {
                trigger.OnlyHit = true;
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-deadly-beauty-desc",
                "The Azata covers a touched creature in flowers with tiny bees. That creature gets {g|Encyclopedia:Damage_Reduction}DR{/g} 5/adamantine. Any creature that hits them with a {g|Encyclopedia:MeleeAttack}melee attack{/g} takes ({g|Encyclopedia:Dice}1d6{/g} + the Azata's {g|Encyclopedia:Caster_Level}caster level{/g}) points of {g|Encyclopedia:Damage_Type}piercing damage{/g}.");
        }

        /// <summary>
        /// The effect component is shared with Deadly Beauty, so this uses the same buff and duration.
        /// Ally includes the caster, and a personal area is centered on the caster.
        /// </summary>
        private static void PatchDeadlyBeautyCommunal()
        {
            var source = AbilityRefs.DeadlyBeauty.Reference.Get();
            if (source.GetComponent<AbilityEffectRunAction>() == null)
            {
                Main.Logger.Error("Deadly Beauty is missing its effect, so Deadly Beauty (Communal) was not created.");
                return;
            }

            var school = source.GetComponent<SpellComponent>()?.School ?? SpellSchool.Transmutation;
            var ability = AbilityConfigurator.NewSpell(
                    "NewSpell.DeadlyBeautyCommunal",
                    "{7E4B1C9A-2D58-4F63-A1E0-6C8B3D5F9A24}",
                    school,
                    canSpecialize: false)
                .CopyFrom(
                    source,
                    typeof(AbilityEffectRunAction),
                    typeof(ContextRankConfig),
                    typeof(AbilitySpawnFx),
                    typeof(SpellDescriptorComponent))
                .SetIcon(source.Icon)
                .SetAnimation(source.Animation)
                .SetRange(AbilityRange.Personal)
                .SetActionType(UnitCommand.CommandType.Standard)
                .AllowTargeting(point: false, enemies: false, friends: true, self: true)
                .SetAvailableMetamagic(Metamagic.CompletelyNormal, Metamagic.Extend, Metamagic.Heighten, Metamagic.Quicken)
                .AddAbilityTargetsAround(includeDead: false, targetType: TargetType.Ally, radius: 30.Feet(), spreadSpeed: 20.Feet())
                .Configure();

            ability.LocalizedDuration = source.LocalizedDuration;
            ability.LocalizedSavingThrow = source.LocalizedSavingThrow;
            ability.SpellResistance = source.SpellResistance;
            ability.m_DisplayName = Helpers.CreateString(
                "mmm-deadly-beauty-communal-name",
                "Deadly Beauty (Communal)");
            ability.m_Description = Helpers.CreateString(
                "mmm-deadly-beauty-communal-desc",
                "The Azata covers all allies within 30 feet in flowers with tiny bees. Each affected creature gets {g|Encyclopedia:Damage_Reduction}DR{/g} 5/adamantine. Any creature that hits them with a {g|Encyclopedia:MeleeAttack}melee attack{/g} takes ({g|Encyclopedia:Dice}1d6{/g} + the Azata's {g|Encyclopedia:Caster_Level}caster level{/g}) points of {g|Encyclopedia:Damage_Type}piercing damage{/g}.");

            if (FindAction<ContextActionApplyBuff>(ability.GetComponent<AbilityEffectRunAction>()?.Actions, new HashSet<object>()) == null)
                Main.Logger.Error("Deadly Beauty (Communal) is missing Deadly Beauty's buff.");

            SpellStuff.AddSpellLevel(ability, SpellListRefs.AzataMythicSpellsSpelllist.Reference, 5);
        }

        private static void PatchColorOfMagic()
        {
            var damage = ContextDice.Value(DiceType.D12, bonus: 0, diceCount: ContextValues.Constant(5));
            var effects = ActionsBuilder.New();
            AddRainbowProjectiles(effects);
            // Separate damage events, so Elemental Barrage sees the same loop as Elemental Assessor:
            // mark, consume, mark, consume. The fifth event leaves a mark. Sonic is the first consumer
            // because a leftover sonic mark is not reliably removed by a later element.
            effects
                .DealDamage(value: damage, damageType: DamageTypes.Energy(DamageEnergyType.Fire))
                .DealDamage(value: damage, damageType: DamageTypes.Energy(DamageEnergyType.Sonic))
                .DealDamage(value: damage, damageType: DamageTypes.Energy(DamageEnergyType.Cold))
                .DealDamage(value: damage, damageType: DamageTypes.Energy(DamageEnergyType.Electricity))
                .DealDamage(value: damage, damageType: DamageTypes.Energy(DamageEnergyType.Acid))
                .SavingThrow(
                    SavingThrowType.Fortitude,
                    onResult: ActionsBuilder.New().ConditionalSaved(
                        failed: ActionsBuilder.New().ApplyBuff(
                            BuffRefs.Stunned.Reference.Get(),
                            ContextDuration.Fixed(1),
                            isFromSpell: true)))
                .SavingThrow(
                    SavingThrowType.Reflex,
                    onResult: ActionsBuilder.New().ConditionalSaved(
                        failed: ActionsBuilder.New().ApplyBuff(
                            BuffRefs.BlindnessBuff.Reference.Get(),
                            ContextDuration.FixedDice(DiceType.D4, 1, 0, DurationRate.Rounds),
                            isFromSpell: true)))
                .SavingThrow(
                    SavingThrowType.Will,
                    onResult: ActionsBuilder.New().ConditionalSaved(
                        failed: ActionsBuilder.New().ApplyBuff(
                            BuffRefs.Confusion.Reference.Get(),
                            ContextDuration.Fixed(1),
                            isFromSpell: true)));

            var visualSource = AbilityRefs.RainbowPattern.Reference.Get();
            var configurator = AbilityConfigurator.NewSpell(
                    "NewSpell.ColorOfMagic",
                    "{5A8D2F71-93C4-4B6E-A057-1D9E3C7F6248}",
                    SpellSchool.Evocation,
                    canSpecialize: false,
                    SpellDescriptor.Fire,
                    SpellDescriptor.Cold,
                    SpellDescriptor.Electricity,
                    SpellDescriptor.Acid,
                    SpellDescriptor.Sonic)
                .SetIcon(AbilityRefs.RainbowArrows.Reference.Get().Icon)
                .SetAnimation(visualSource.Animation)
                .SetRange(AbilityRange.Personal)
                .SetActionType(UnitCommand.CommandType.Standard)
                // Personal range resolves against the caster. The area component still limits the effect to enemies.
                .AllowTargeting(point: false, enemies: true, friends: false, self: true)
                .SetAvailableMetamagic(
                    Metamagic.CompletelyNormal,
                    Metamagic.Empower,
                    Metamagic.Maximize,
                    Metamagic.Quicken)
                .AddAbilityTargetsAround(
                    includeDead: false,
                    targetType: TargetType.Enemy,
                    radius: 30.Feet(),
                    spreadSpeed: 20.Feet())
                .AddAbilityEffectRunAction(effects);

            AddCasterBurst(configurator, visualSource, "Rainbow Pattern");
            AddCasterBurst(configurator, AbilityRefs.RainbowDome.Reference.Get(), "Rainbow Dome");
            AddCasterBurst(configurator, AbilityRefs.BurstOfSonicEnergy.Reference.Get(), "Burst of Sonic Energy");

            var ability = configurator.Configure();
            ability.SpellResistance = false;
            ability.LocalizedDuration = AbilityRefs.Fireball.Reference.Get().LocalizedDuration;
            ability.LocalizedSavingThrow = Helpers.CreateString(
                "mmm-color-of-magic-save",
                "Fortitude, Reflex, and Will negate individual effects");
            ability.m_DisplayName = Helpers.CreateString(
                "mmm-color-of-magic-name",
                "The Color of Magic");
            ability.m_Description = Helpers.CreateString(
                "mmm-color-of-magic-desc",
                "The Azata erupts with rainbow light. Every enemy within 30 feet takes {g|Encyclopedia:Dice}5d12{/g} points each of fire, sonic, cold, electricity, and acid {g|Encyclopedia:Energy_Damage}damage{/g}, in that order. This damage allows no {g|Encyclopedia:Saving_Throw}saving throw{/g} and is not affected by {g|Encyclopedia:Spell_Resistance}spell resistance{/g}. Each enemy then attempts three independent saving throws: a Fortitude save negates being {g|ConditionStunned}stunned{/g} for 1 round, a Reflex save negates being {g|ConditionBlind}blinded{/g} for {g|Encyclopedia:Dice}1d4{/g} rounds, and a Will save negates being {g|ConditionConfusion}confused{/g} for 1 round. Immunity to a condition prevents that condition normally.");

            SpellStuff.AddSpellLevel(ability, SpellListRefs.AzataMythicSpellsSpelllist.Reference, 10);
        }

        /// <summary>
        /// Rainbow Pattern's effect component cannot be shared, so each visual is copied onto this spell and anchored to the caster.
        /// Rainbow rays are also fired at every affected enemy.
        /// </summary>
        private static void AddCasterBurst(AbilityConfigurator configurator, BlueprintAbility source, string spellName)
        {
            var fx = source?.GetComponent<AbilitySpawnFx>();
            if (fx == null)
            {
                Main.Logger.Info($"{spellName} has no burst visual.");
                return;
            }

            var clone = CloneComponent(fx);
            var anchor = typeof(AbilitySpawnFx).GetField("Anchor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? typeof(AbilitySpawnFx).GetField("m_Anchor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (anchor != null && Enum.GetNames(anchor.FieldType).Contains("Caster"))
                anchor.SetValue(clone, Enum.Parse(anchor.FieldType, "Caster"));
            configurator.AddComponent(clone);
        }

        private static BlueprintComponent CloneComponent(BlueprintComponent source)
        {
            var clone = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(source.GetType())
                ? (BlueprintComponent)(object)UnityEngine.ScriptableObject.CreateInstance(source.GetType())
                : (BlueprintComponent)Activator.CreateInstance(source.GetType());
            foreach (var field in source.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.IsStatic || field.Name.Contains("Owner")) continue;
                field.SetValue(clone, field.GetValue(source));
            }
            return clone;
        }

        private static void AddRainbowProjectiles(ActionsBuilder effects)
        {
            var projectiles = FindProjectiles(AbilityRefs.RainbowArrows.Reference.Get())
                .Select(projectile => projectile.ToString())
                .Where(projectile => !string.IsNullOrEmpty(projectile))
                .ToList();
            if (projectiles.Count == 0)
            {
                projectiles.Add(ProjectileRefs.ArrowFire00.ToString());
                projectiles.Add(ProjectileRefs.ColdCommonProjectile00.ToString());
                projectiles.Add(ProjectileRefs.ElectroCommonProjectile00.ToString());
                projectiles.Add(ProjectileRefs.AcidArrow00.ToString());
                projectiles.Add(ProjectileRefs.SonicCommonRay00_Projectile.ToString());
            }

            foreach (var projectile in projectiles)
                effects.ProjectileFx(projectile);
        }

        private static List<object> FindProjectiles(BlueprintAbility ability)
        {
            var found = new List<object>();
            var deliver = ability.GetComponent<AbilityDeliverProjectile>();
            if (deliver == null) return found;
            foreach (var field in deliver.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.Name.IndexOf("rojectile", StringComparison.OrdinalIgnoreCase) < 0) continue;
                CollectProjectiles(field.GetValue(deliver), found, new HashSet<object>());
            }
            return found;
        }

        private static void CollectProjectiles(object node, List<object> found, HashSet<object> seen)
        {
            if (node == null || node is string || !seen.Add(node)) return;
            if (node.GetType().Name.IndexOf("Projectile", StringComparison.OrdinalIgnoreCase) >= 0
                && node.GetType().Name.IndexOf("Reference", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                found.Add(node);
                return;
            }

            if (node is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                    CollectProjectiles(item, found, seen);
            }
        }

        /// <summary>
        /// The crit is turned into a miss after the roll and before damage. Blink is applied here because
        /// the original trigger only runs for a confirmed critical hit, which this attack no longer is.
        /// </summary>
        private static void PatchElusiveNature()
        {
            new Harmony("MythicMagicMayhem.ElusiveNature").Patch(
                AccessTools.Method(typeof(RuleAttackRoll), nameof(RuleAttackRoll.OnTrigger)),
                postfix: new HarmonyMethod(typeof(AzataSpellBalance), nameof(NegateElusiveNatureCrit)));

            var ability = AbilityRefs.ElusiveNature.Reference.Get();
            ability.m_Description = Helpers.CreateString(
                "mmm-elusive-nature-desc",
                "The Azata can grant one companion immunity to the {g|ConditionProne}prone{/g} condition and immunity to {g|Encyclopedia:Combat_Maneuvers}combat maneuvers{/g} for 1 {g|Encyclopedia:Combat_Round}round{/g} per {g|Encyclopedia:Caster_Level}caster level{/g}. Whenever the affected companion is critically hit, they are also affected with the effect of the blink {g|Encyclopedia:Spell}spell{/g} for 1 round per four caster levels the Azata possesses. The critical hit that causes blink to begin misses completely. While blink is active, attacks resolve normally.");
        }

        private static void NegateElusiveNatureCrit(RuleAttackRoll __instance)
        {
            try
            {
                var target = __instance.Target;
                if (target == null || !__instance.IsHit || !__instance.IsCriticalConfirmed)
                    return;

                var elusive = target.Buffs.GetBuff(BuffRefs.ElusiveNatureBuff.Reference.Get());
                if (elusive == null)
                    return;
                if (target.Buffs.GetBuff(BuffRefs.BlinkBuff.Reference.Get()) != null)
                    return;

                __instance.SetFake(AttackResult.Miss);
                AccessTools.Property(typeof(RuleAttackRoll), nameof(RuleAttackRoll.IsCriticalConfirmed))
                    .SetValue(__instance, false);

                var trigger = elusive.Blueprint.GetComponent<AddTargetAttackWithWeaponTrigger>();
                if (trigger?.ActionOnSelf == null)
                {
                    Main.Logger.Error("Elusive Nature is missing its blink action.");
                    return;
                }

                ((IFactContextOwner)elusive).RunActionInContext(trigger.ActionOnSelf, target);
            }
            catch (Exception exception)
            {
                Main.Logger.Error("Failed to negate the Elusive Nature critical hit.", exception);
            }
        }

        private static T FindAction<T>(object node, HashSet<object> seen) where T : GameAction
        {
            if (node == null || !seen.Add(node)) return null;
            if (node is T match) return match;

            if (node is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is not GameAction and not ActionList) continue;
                    var found = FindAction<T>(item, seen);
                    if (found != null) return found;
                }
            }

            if (node is not GameAction and not ActionList) return null;
            foreach (var field in node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var value = field.GetValue(node);
                if (value is not GameAction and not ActionList and not IEnumerable) continue;
                var found = FindAction<T>(value, seen);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// Area target type Enemy is 2. That excludes allies. The area controller still treats neutral creatures as enemies.
        /// </summary>
        private static void PatchRepulsiveNature()
        {
            var area = AbilityAreaEffectRefs.RepulsiveNatureArea.Reference.Get();
            var targetType = AccessTools.Field(typeof(BlueprintAbilityAreaEffect), "m_TargetType");
            targetType.SetValue(area, Enum.ToObject(targetType.FieldType, 2));

            var ability = AbilityRefs.RepulsiveNature.Reference.Get();
            ability.m_Description = Helpers.CreateString(
                "mmm-repulsive-nature-desc",
                "The Azata creates nauseating plants in a 40-foot area for 1 {g|Encyclopedia:Combat_Round}round{/g} per {g|Encyclopedia:Caster_Level}caster level{/g}. Any enemy within this area must succeed at a {g|Encyclopedia:Saving_Throw}Fortitude saving throw{/g} or become {g|ConditionNauseated}nauseated{/g} and suffer {g|Encyclopedia:Dice}2d6{/g} {g|Encyclopedia:Energy_Damage}acid damage{/g} every round. The creature attempts to end the nauseated condition by making further Fortitude saving throws every round. A successful save makes the creature immune to this effect until the end of combat.");
        }

        /// <summary>
        /// Ground on the spell makes the area skip flying creatures and creatures immune to ground effects.
        /// The Reflex and Will tracks are separate. Sleep immunity stays for the casting, including after
        /// the creature leaves the flowers, and ends with the area because the buff is linked to it.
        /// </summary>
        private static void PatchFieldOfFlowers()
        {
            var ability = AbilityRefs.FieldOfFlowers.Reference.Get();
            var descriptor = ability.GetComponent<SpellDescriptorComponent>();
            if (descriptor == null)
            {
                Main.Logger.Error("Field of Flowers is missing its spell descriptor.");
            }
            else
            {
                descriptor.Descriptor = new SpellDescriptorWrapper(descriptor.Descriptor.Value | SpellDescriptor.Ground);
            }

            var icon = ability.Icon;
            var entangle = BuffConfigurator.New("FieldOfFlowersEntangleBuff", "{A7C3E1B4-6D20-4F8A-9C15-2B8E4F6A1D03}")
                .SetIcon(icon)
                .AddCondition(UnitCondition.Entangled)
                .Configure();
            entangle.m_DisplayName = Helpers.CreateString("mmm-fof-entangle-name", "Grasping Vines");
            entangle.m_Description = Helpers.CreateString("mmm-fof-entangle-desc", "Entangled by grasping vines.");

            var immobilize = BuffConfigurator.New("FieldOfFlowersImmobilizeBuff", "{B8D4F2C5-7E31-4A9B-8D26-3C9F5A7B2E14}")
                .SetIcon(icon)
                .AddCondition(UnitCondition.CantMove)
                .Configure();
            immobilize.m_DisplayName = Helpers.CreateString("mmm-fof-immobilize-name", "Grasping Vines");
            immobilize.m_Description = Helpers.CreateString("mmm-fof-immobilize-desc", "Held in place by grasping vines.");

            var sickness = new SpellDescriptorWrapper(SpellDescriptor.Nauseated | SpellDescriptor.Sickened);
            var sweetAir = BuffConfigurator.New("FieldOfFlowersSweetAirBuff", "{C9E5A3D6-8F42-4B0C-9E37-4D0A6B8C3F25}")
                .SetIcon(icon)
                .AddConditionImmunity(UnitCondition.Nauseated)
                .AddConditionImmunity(UnitCondition.Sickened)
                .AddBuffDescriptorImmunity(descriptor: sickness)
                .AddSpellImmunityToSpellDescriptor(descriptor: sickness)
                .Configure();
            sweetAir.m_DisplayName = Helpers.CreateString("mmm-fof-sweet-air-name", "Sweet Air");
            sweetAir.m_Description = Helpers.CreateString("mmm-fof-sweet-air-desc", "Immune to the nauseated and sickened conditions.");

            var mindAffecting = new SpellDescriptorWrapper(SpellDescriptor.MindAffecting);
            BuffConfigurator.For(BuffRefs.FieldOfFlowersBuffDistracted.Reference.Get())
                .AddSpellDescriptorComponent(mindAffecting)
                .Configure();
            BuffConfigurator.For(BuffRefs.FieldOfFlowersBuffAsleep.Reference.Get())
                .AddSpellDescriptorComponent(mindAffecting)
                .Configure();

            var area = AbilityAreaEffectRefs.FieldOfFlowersArea.Reference.Get();
            AbilityAreaEffectConfigurator.For(area)
                .AddAbilityAreaEffectRunAction(
                    merge: null,
                    mergeBehavior: ComponentMerge.Replace,
                    round: EnemySaves(entangle, immobilize),
                    unitEnter: EnemySaves(entangle, immobilize),
                    unitExit: ActionsBuilder.New()
                        .RemoveBuff(entangle)
                        .RemoveBuff(immobilize)
                        .RemoveBuff(BuffRefs.FieldOfFlowersBuffDistracted.Reference.Get()),
                    unitMove: ActionsBuilder.New())
                .AddAbilityAreaEffectBuff(
                    sweetAir,
                    checkConditionEveryRound: true,
                    condition: ConditionsBuilder.New().IsAlly())
                .Configure();

            ability.m_Description = Helpers.CreateString(
                "mmm-field-of-flowers-desc",
                "The Azata creates a 30-foot area of blossoming flowers. Flying creatures are unaffected. Each {g|Encyclopedia:Combat_Round}round{/g}, enemies in the area make a {g|Encyclopedia:Saving_Throw}Reflex saving throw{/g}. The first failure {g|ConditionEntangled}entangles{/g} them. A second failure immobilizes them until they succeed at a Reflex saving throw or the spell ends. Enemies also make a Will saving throw each round. This fragrance is mind-affecting. The first failure distracts them, imposing a –2 {g|Encyclopedia:Penalty}penalty{/g} on {g|Encyclopedia:Attack}attack{/g} and {g|Encyclopedia:Damage}damage{/g} rolls. A second failure puts them to sleep for {g|Encyclopedia:Dice}1d4{/g} rounds. A creature that awakens cannot be put to sleep again by the same casting. Allies in the area are immune to the {g|ConditionNauseated}nauseated{/g} and {g|ConditionSickened}sickened{/g} conditions.");
        }

        private static ActionsBuilder EnemySaves(BlueprintBuff entangle, BlueprintBuff immobilize)
        {
            var distracted = BuffRefs.FieldOfFlowersBuffDistracted.Reference.Get();
            var asleep = BuffRefs.FieldOfFlowersBuffAsleep.Reference.Get();
            var immunity = BuffRefs.FieldOfFlowersBuffImmunity.Reference.Get();

            return ActionsBuilder.New()
                .Conditional(
                    ConditionsBuilder.New().IsEnemy(),
                    ActionsBuilder.New()
                        .SavingThrow(SavingThrowType.Reflex, onResult: VineSave(entangle, immobilize))
                        .Conditional(
                            ConditionsBuilder.New().Add(new ImmuneToMindAffectingCondition()),
                            ActionsBuilder.New(),
                            ActionsBuilder.New().SavingThrow(SavingThrowType.Will, onResult: FragranceSave(distracted, asleep, immunity))));
        }

        private static ActionsBuilder VineSave(BlueprintBuff entangle, BlueprintBuff immobilize)
        {
            return ActionsBuilder.New().ConditionalSaved(
                failed: ActionsBuilder.New().Conditional(
                    ConditionsBuilder.New().HasBuff(entangle),
                    ActionsBuilder.New().ApplyBuffPermanent(immobilize, isFromSpell: true),
                    ActionsBuilder.New().ApplyBuffPermanent(entangle, isFromSpell: true)),
                succeed: ActionsBuilder.New().Conditional(
                    ConditionsBuilder.New().HasBuff(immobilize),
                    ActionsBuilder.New().RemoveBuff(immobilize),
                    ActionsBuilder.New()));
        }

        private static ActionsBuilder FragranceSave(BlueprintBuff distracted, BlueprintBuff asleep, BlueprintBuff immunity)
        {
            return ActionsBuilder.New().ConditionalSaved(
                failed: ActionsBuilder.New().Conditional(
                    ConditionsBuilder.New().HasBuff(distracted),
                    ActionsBuilder.New().Conditional(
                        ConditionsBuilder.New().HasBuff(immunity),
                        ActionsBuilder.New(),
                        ActionsBuilder.New()
                            .ApplyBuff(asleep, ContextDuration.FixedDice(DiceType.D4, 1, 0, DurationRate.Rounds), isFromSpell: true)
                            .ApplyBuffPermanent(immunity, isFromSpell: true)),
                    ActionsBuilder.New().ApplyBuffPermanent(distracted, isFromSpell: true)),
                succeed: ActionsBuilder.New());
        }

        private static void PatchUnbreakableBond()
        {
            AbilityAreaEffectConfigurator.For(AbilityAreaEffectRefs.UnbreakableBondArea.Reference.Get())
                .SetSize(30.Feet())
                .Configure();

            var ability = AbilityRefs.UnbreakableBond.Reference.Get();
            ability.m_Description = Helpers.CreateString(
                "mmm-unbreakable-bond-desc",
                "While under the effect of this {g|Encyclopedia:Spell}spell{/g}, all companions become immune to mind-affecting, charm, fear, compulsion, emotion, {g|ConditionShaken}shaken{/g}, and {g|ConditionConfusion}confusion{/g} effects. They also get a +3 luck {g|Encyclopedia:Bonus}bonus{/g} on all {g|Encyclopedia:Saving_Throw}saving throws{/g} and {g|Encyclopedia:Damage_Reduction}DR{/g} 5/— while they remain within 30 feet of the Azata.");
        }

        /// <summary>
        /// The cast itself stays a standard action. Each later star is a separate ability granted while the spell lasts.
        /// </summary>
        private static void PatchRainbowStarfall()
        {
            var star = AbilityRefs.RainbowStarfallStormAbility.Reference.Get();
            AbilityConfigurator.For(star)
                .SetActionType(UnitCommand.CommandType.Swift)
                .Configure();
            AddElementalSpellDescriptors(star, "Rainbow Starfall");
            star.m_Description = Helpers.CreateString(
                "mmm-rainbow-starfall-star-desc",
                "You may call down a rainbow star as a {g|Encyclopedia:Swift_Action}swift action{/g}. It deals {g|Encyclopedia:Dice}1d6{/g} per three {g|Encyclopedia:Caster_Level}caster levels{/g} points of acid, cold, electricity, fire, or {g|Encyclopedia:Energy_Damage}sonic damage{/g}, depending on the greatest potential damage (taking into consideration the target's immunities and energy {g|Encyclopedia:Energy_Resistance}resistances{/g}). The star falls down in a vertical stroke at whatever target point you choose within long range.");

            var ability = AbilityRefs.RainbowStarfall.Reference.Get();
            AddElementalSpellDescriptors(ability, "Rainbow Starfall");
            ability.m_Description = Helpers.CreateString(
                "mmm-rainbow-starfall-desc",
                "This {g|Encyclopedia:Spell}spell{/g} functions like {g|SpellsCallLightningStorm}call lightning storm{/g}, except the Azata invokes a rainbow starfall. Calling another star is a {g|Encyclopedia:Swift_Action}swift action{/g}. Every star deals {g|Encyclopedia:Dice}1d6{/g} per three {g|Encyclopedia:Caster_Level}caster levels{/g} points of acid, cold, electricity, fire, or {g|Encyclopedia:Energy_Damage}sonic damage{/g}, depending on the greatest potential damage (taking into consideration the target's immunities and energy {g|Encyclopedia:Energy_Resistance}resistances{/g}). You may call a maximum of 15 stars.");
        }

        /// <summary>
        /// The base spell picks an energy at random and never rolls the Reflex save shown in its tooltip.
        /// Starfall's best-damage action previews each energy, including immunity and resistance, and applies the highest.
        /// </summary>
        private static void PatchRainbowArrows()
        {
            var ability = AbilityRefs.RainbowArrows.Reference.Get();
            AddElementalSpellDescriptors(ability, "Rainbow Arrows");
            var effect = ability.GetComponent<AbilityEffectRunAction>();
            var actions = effect?.Actions?.Actions;
            var randomIndex = actions == null ? -1 : Array.FindIndex(actions, action => action is ContextActionRandomize);
            if (effect == null || randomIndex < 0)
            {
                Main.Logger.Error("Rainbow Arrows is missing its random energy damage.");
                return;
            }

            var wrappersField = AccessTools.Field(typeof(ContextActionRandomize), "m_Actions");
            var damageListField = AccessTools.Field(typeof(ContextActionDealBestDamage), "m_DamageList");
            var wrappers = wrappersField?.GetValue(actions[randomIndex]) as Array;
            var damages = new List<GameAction>();
            if (wrappers != null)
            {
                foreach (var wrapper in wrappers)
                {
                    var actionList = AccessTools.Field(wrapper.GetType(), "Action")?.GetValue(wrapper) as ActionList;
                    var damage = actionList?.Actions?.OfType<ContextActionDealDamage>().FirstOrDefault();
                    if (damage == null)
                        continue;

                    var value = damage.Value;
                    value.DiceType = DiceType.D8;
                    damage.Value = value;
                    damage.HalfIfSaved = true;
                    damages.Add(damage);
                }
            }

            if (damageListField == null || damages.Count == 0)
            {
                Main.Logger.Error("Rainbow Arrows is missing its energy damage options.");
                return;
            }

            var best = new ContextActionDealBestDamage();
            damageListField.SetValue(best, new ActionList { Actions = damages.ToArray() });
            actions[randomIndex] = best;
            effect.SavingThrowType = SavingThrowType.Reflex;

            var diceRank = ability.GetComponents<ContextRankConfig>().FirstOrDefault(rank => rank.Type == AbilityRankType.Default);
            if (diceRank == null)
            {
                Main.Logger.Error("Rainbow Arrows is missing its caster level damage rank.");
            }
            else
            {
                diceRank.m_Progression = ContextRankProgression.AsIs;
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-rainbow-arrows-desc",
                "The Azata creates a rainbow arrow that jumps from target to target up to one time per four {g|Encyclopedia:Caster_Level}caster levels{/g}, dealing {g|Encyclopedia:Dice}1d8{/g} per {g|Encyclopedia:Caster_Level}caster level{/g} points of acid, cold, electricity, fire, or {g|Encyclopedia:Energy_Damage}sonic damage{/g}, depending on the greatest potential damage (taking into consideration the target's immunities and energy {g|Encyclopedia:Energy_Resistance}resistances{/g}). A successful {g|Encyclopedia:Saving_Throw}Reflex saving throw{/g} halves the damage.");
        }

        private static void AddElementalSpellDescriptors(BlueprintAbility ability, string spellName)
        {
            const SpellDescriptor elements =
                SpellDescriptor.Acid
                | SpellDescriptor.Cold
                | SpellDescriptor.Electricity
                | SpellDescriptor.Fire
                | SpellDescriptor.Sonic;

            var descriptor = ability.GetComponent<SpellDescriptorComponent>();
            if (descriptor == null)
            {
                AbilityConfigurator.For(ability)
                    .AddSpellDescriptorComponent(elements)
                    .Configure();
                if (ability.GetComponent<SpellDescriptorComponent>() == null)
                    Main.Logger.Error($"{spellName} is missing its spell descriptor.");
                return;
            }

            descriptor.Descriptor = new SpellDescriptorWrapper(descriptor.Descriptor.Value | elements);
        }

        /// <summary>
        /// The burst is centered on the caster. Adjacent means within 5 feet, which is the full-damage branch.
        /// Flat-footed here is the pair of conditions that remove Dexterity from AC and forbid attacks of opportunity.
        /// </summary>
        private static void PatchBurstOfSonicEnergy()
        {
            var ability = AbilityRefs.BurstOfSonicEnergy.Reference.Get();
            var buff = BuffConfigurator.New("BurstOfSonicEnergyFlatFootedBuff", "{E7A1C4D2-8B36-4F19-9C50-2D6F8A1B4E73}")
                .SetIcon(ability.Icon)
                .AddCondition(UnitCondition.LoseDexterityToAC)
                .AddCondition(UnitCondition.DisableAttacksOfOpportunity)
                .Configure();
            buff.m_DisplayName = Helpers.CreateString("mmm-sonic-burst-flat-footed-name", "Flat-Footed");
            buff.m_Description = Helpers.CreateString(
                "mmm-sonic-burst-flat-footed-desc",
                "This creature is flat-footed. It loses its Dexterity bonus to AC and cannot make attacks of opportunity.");

            var spells = new[]
            {
                ability,
                AbilityRefs.BurstOfSonicEnergyAdjacent.Reference.Get(),
                AbilityRefs.BurstOfSonicEnergy10Feet.Reference.Get(),
            };
            foreach (var spell in spells)
            {
                var areas = spell.GetComponents<AbilityTargetsAround>().ToArray();
                if (areas.Length == 0)
                {
                    Main.Logger.Error($"{spell.name} is missing its area.");
                    continue;
                }

                foreach (var area in areas)
                    area.m_TargetType = TargetType.Enemy;
            }

            var adjacent = ability.GetComponent<AbilityEffectRunAction>()?.Actions?.Actions?
                .OfType<Conditional>()
                .FirstOrDefault(IsWithinFiveFeet);
            if (adjacent?.IfTrue == null)
            {
                Main.Logger.Error("Burst of Sonic Energy is missing its adjacent damage.");
            }
            else
            {
                AddFlatFooted(adjacent.IfTrue, buff);
            }

            var close = AbilityRefs.BurstOfSonicEnergyAdjacent.Reference.Get().GetComponent<AbilityEffectRunAction>();
            if (close?.Actions != null)
                AddFlatFooted(close.Actions, buff);

            ability.m_Description = Helpers.CreateString(
                "mmm-burst-of-sonic-energy-desc",
                "The Azata creates bolts that deal {g|Encyclopedia:Dice}1d6{/g} {g|Encyclopedia:Energy_Damage}sonic damage{/g} per two {g|Encyclopedia:Caster_Level}caster levels{/g} to adjacent enemies and half the Azata's caster level sonic damage to enemies within 10 feet. Adjacent enemies are {g|Encyclopedia:Flat_Footed}flat-footed{/g} for 1 {g|Encyclopedia:Combat_Round}round{/g} and cannot make {g|Encyclopedia:Attack_Of_Opportunity}attacks of opportunity{/g}.");
        }

        private static bool IsWithinFiveFeet(Conditional conditional)
        {
            return conditional.ConditionsChecker?.Conditions?
                .OfType<ContextConditionDistanceToTarget>()
                .Any(condition => condition.Not && Math.Abs(condition.DistanceGreater.Meters - 5.Feet().Meters) < 0.05f) == true;
        }

        private static void AddFlatFooted(ActionList actions, BlueprintBuff buff)
        {
            var apply = ActionsBuilder.New()
                .ApplyBuff(buff, ContextDuration.Fixed(1), isFromSpell: true)
                .Build()
                .Actions
                .Single();
            actions.Actions = actions.Actions.Concat(new GameAction[] { apply }).ToArray();
        }

        private static void PatchSuddenSquall()
        {
            var ability = AbilityRefs.SuddenSquall.Reference.Get();
            var projectile = ability.GetComponent<AbilityDeliverProjectile>();
            if (projectile == null)
            {
                Main.Logger.Error("Sudden Squall is missing its cone.");
            }
            else
            {
                projectile.m_Length = 30.Feet();
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-sudden-squall-desc",
                "The Azata creates a 30-foot cone of wind that deals ({g|Encyclopedia:Dice}3d6{/g} + the Azata's {g|Encyclopedia:Caster_Level}caster level{/g}) {g|Encyclopedia:Energy_Damage}sonic damage{/g} to all enemies inside it. Every affected enemy must succeed at a {g|Encyclopedia:Saving_Throw}Fortitude saving throw{/g} or become disoriented and unable to cast any {g|Encyclopedia:Spell}spells{/g} for 1d3 {g|Encyclopedia:Combat_Round}rounds{/g}.");
        }

        /// <summary>
        /// The spell is created by the Azata spellbook changes, which run before this patch.
        /// </summary>
        private static void PatchDragonDance()
        {
            var description = Helpers.CreateString(
                "mmm-dragon-dance-desc",
                "The primordial power of Elysium flows through you blasting your enemies and rendering them vulnerable to your elemental power.  Choose an element (fire, cold, lightning, acid or sonic) when you cast the spell, the spell leaps from target to target in the same way as chain lightning inflicting 1d8 damage of the chosen element per caster level. The target also gains vulnerability to the selected element for 1 minute, succeeding in a saving throw halves the damage but does not prevent the vulnerability effect.");
            var guids = new[]
            {
                AzataNewSpell.DragonDanceAbilityGuid,
                AzataNewSpell.DragonDanceAbilityGuid1,
                AzataNewSpell.DragonDanceAbilityGuid2,
                AzataNewSpell.DragonDanceAbilityGuid3,
                AzataNewSpell.DragonDanceAbilityGuid4,
                AzataNewSpell.DragonDanceAbilityGuid5,
            };
            var found = false;
            foreach (var guid in guids)
            {
                var ability = ResourcesLibrary.TryGetBlueprint<BlueprintAbility>(guid);
                if (ability == null)
                    continue;
                found = true;
                ability.m_Description = description;
                var damage = FindAction<ContextActionDealDamage>(
                    ability.GetComponent<AbilityEffectRunAction>()?.Actions,
                    new HashSet<object>());
                if (damage == null)
                    continue;
                var value = damage.Value;
                value.DiceType = DiceType.D8;
                damage.Value = value;
            }

            if (!found)
                Main.Logger.Error("Primordial Dragon Dance was not found, so its damage dice were not changed.");
        }

        /// <summary>
        /// The saving throw's actions used to run on both results. The weakness now does that on purpose,
        /// and prone and slowed happen only when the save fails. The original buff slows and applies the
        /// weakness together, so a successful save gets a weakness-only buff with the same duration.
        /// </summary>
        private static void PatchWaterTorrent()
        {
            var ability = AbilityRefs.WaterTorrent.Reference.Get();
            if (!ability.GetComponents<ContextRankConfig>().Any(rank => rank.Type == AbilityRankType.DamageDice))
            {
                AbilityConfigurator.For(ability)
                    .AddContextRankConfig(ContextRankConfigs.CasterLevel(type: AbilityRankType.DamageDice).WithDiv2Progression(0))
                    .Configure();
            }

            var damage = FindAction<ContextActionDealDamage>(
                ability.GetComponent<AbilityEffectRunAction>()?.Actions,
                new HashSet<object>());
            if (damage == null)
            {
                Main.Logger.Error("Water Torrent is missing its damage.");
            }
            else
            {
                var value = damage.Value;
                value.DiceCountValue = ContextValues.Rank(AbilityRankType.DamageDice);
                value.DiceType = DiceType.D6;
                damage.Value = value;
            }

            var save = FindAction<ContextActionSavingThrow>(
                ability.GetComponent<AbilityEffectRunAction>()?.Actions,
                new HashSet<object>());
            var saveActions = save?.Actions?.Actions;
            var slow = saveActions?.OfType<ContextActionApplyBuff>().FirstOrDefault();
            var knockdown = saveActions?.OfType<ContextActionKnockdownTarget>().FirstOrDefault();
            var vulnerability = slow?.Buff?.GetComponent<AddDamageTypeVulnerability>();
            if (save == null || slow == null || knockdown == null || vulnerability == null)
            {
                Main.Logger.Error("Water Torrent is missing its bludgeoning weakness.");
            }
            else
            {
                var weakness = BuffConfigurator.New("WaterTorrentWeaknessBuff", "{D4E8A1C7-6B25-4F90-8A13-7C2E5B9D4F18}")
                    .SetIcon(slow.Buff.Icon)
                    .AddComponent(new AddDamageTypeVulnerability
                    {
                        PhyscicalForm = vulnerability.PhyscicalForm,
                        FormType = vulnerability.FormType,
                        PhyscicalAlignment = vulnerability.PhyscicalAlignment,
                        DamageAlignmentType = vulnerability.DamageAlignmentType,
                        PhyscicalMaterial = vulnerability.PhyscicalMaterial,
                        MaterialType = vulnerability.MaterialType,
                    })
                    .Configure();
                weakness.m_DisplayName = Helpers.CreateString("mmm-water-torrent-weakness-name", "Bludgeoning Weakness");
                weakness.m_Description = Helpers.CreateString(
                    "mmm-water-torrent-weakness-desc",
                    "This creature is vulnerable to {g|Encyclopedia:Damage_Type}bludgeoning damage{/g}. A successful saving throw does not prevent this weakness.");

                var weaknessAction = (ContextActionApplyBuff)ActionsBuilder.New()
                    .ApplyBuff(
                        weakness,
                        slow.DurationValue,
                        asChild: slow.AsChild,
                        ignoreParentContext: slow.IgnoreParentContext,
                        isFromSpell: slow.IsFromSpell,
                        isNotDispelable: slow.IsNotDispelable,
                        notLinkToAreaEffect: slow.NotLinkToAreaEffect,
                        sameDuration: slow.SameDuration,
                        toCaster: slow.ToCaster)
                    .Build()
                    .Actions
                    .Single();
                weaknessAction.Permanent = slow.Permanent;
                weaknessAction.UseDurationSeconds = slow.UseDurationSeconds;
                weaknessAction.DurationSeconds = slow.DurationSeconds;

                save.Actions = new ActionList
                {
                    Actions = new GameAction[]
                    {
                        new ContextActionConditionalSaved
                        {
                            Failed = new ActionList { Actions = new GameAction[] { knockdown, slow } },
                            Succeed = new ActionList { Actions = new GameAction[] { weaknessAction } },
                        }
                    }
                };
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-water-torrent-desc",
                "The Azata summons a wave of water that deals {g|Encyclopedia:Dice}1d6{/g} {g|Encyclopedia:Damage_Type}bludgeoning damage{/g} per two {g|Encyclopedia:Caster_Level}caster levels{/g} to all enemies in the area. Affected enemies become vulnerable to bludgeoning damage for 1d6 {g|Encyclopedia:Combat_Round}rounds{/g}, even if they succeed at a {g|Encyclopedia:Saving_Throw}Fortitude saving throw{/g}. A failed save also knocks them prone and {g|ConditionSlowed}slows{/g} them for that duration. All allies affected by this wave are cleansed from all of the following conditions: {g|Encyclopedia:AbilityDamage}ability damage{/g}, {g|ConditionBlind}blinded{/g}, {g|ConditionConfusion}confused{/g}, {g|ConditionDazzled}dazzled{/g}, diseased, {g|ConditionExhausted}exhausted{/g}, {g|ConditionFatigued}fatigued{/g}, {g|ConditionNauseated}nauseated{/g}, poisoned, and {g|ConditionSickened}sickened{/g}.");
        }

        /// <summary>
        /// The cone delivers to every creature in it unless this check rejects allies.
        /// The damage rank is already caster level divided by 2.
        /// </summary>
        private static void PatchWindsOfTheFall()
        {
            var ability = AbilityRefs.WindsOfTheFall.Reference.Get();
            var damage = FindAction<ContextActionDealDamage>(
                ability.GetComponent<AbilityEffectRunAction>()?.Actions,
                new HashSet<object>());
            if (damage == null)
            {
                Main.Logger.Error("Winds of the Fall is missing its damage.");
            }
            else
            {
                var value = damage.Value;
                value.DiceType = DiceType.D12;
                damage.Value = value;
            }

            ability.m_Description = Helpers.CreateString(
                "mmm-winds-of-the-fall-desc",
                "The Azata creates a 30-foot cone of leaves that deals {g|Encyclopedia:Dice}1d12{/g} {g|Encyclopedia:Damage_Type}slashing damage{/g} per two caster levels to enemies within the cone. An enemy that suffers damage from this {g|Encyclopedia:Spell}spell{/g} must succeed at a {g|Encyclopedia:Saving_Throw}Will saving throw{/g} or become intoxicated for 1 {g|Encyclopedia:Combat_Round}round{/g} per {g|Encyclopedia:Caster_Level}caster level{/g}. Whenever the creature under this effect makes a roll, it rolls twice and chooses the worse result. A successful saving throw reduces the duration of this effect to 1 round.");

            new Harmony("MythicMagicMayhem.WindsOfTheFall").Patch(
                AccessTools.Method(typeof(AbilityDeliverProjectile), "CanTargetUnit"),
                prefix: new HarmonyMethod(typeof(AzataSpellBalance), nameof(WindsOfTheFallEnemiesOnly)));
        }

        private static bool WindsOfTheFallEnemiesOnly(AbilityExecutionContext context, UnitEntityData unit, ref bool __result)
        {
            if (context?.AbilityBlueprint != AbilityRefs.WindsOfTheFall.Reference.Get())
                return true;

            var caster = context.MaybeCaster;
            __result = caster != null && caster.IsEnemy(unit);
            return false;
        }

        [TypeId("c4a1e7b29d0f4a6e8b3c1d5f7a9e2b64")]
        private class ImmuneToMindAffectingCondition : ContextCondition
        {
            public override string GetConditionCaption()
            {
                return "Immune to mind-affecting";
            }

            public override bool CheckCondition()
            {
                var unit = Target?.Unit;
                if (unit == null)
                    return false;

                var resistance = unit.Get<UnitPartSpellResistance>();
                if (resistance != null && resistance.IgnoreSpellDescriptorImmunities.HasAnyFlag(SpellDescriptor.MindAffecting))
                    return false;

                var caster = Context?.MaybeCaster;
                foreach (var fact in unit.Facts.List)
                {
                    foreach (var immunity in fact.SelectComponents<SpellImmunityToSpellDescriptor>())
                    {
                        if (!immunity.Descriptor.HasAnyFlag(SpellDescriptor.MindAffecting))
                            continue;
                        if (immunity.CasterIgnoreImmunityFact != null && caster != null && caster.Descriptor.HasFact(immunity.CasterIgnoreImmunityFact))
                            continue;
                        return true;
                    }

                    foreach (var immunity in fact.SelectComponents<BuffDescriptorImmunity>())
                    {
                        if (!immunity.Descriptor.HasAnyFlag(SpellDescriptor.MindAffecting))
                            continue;
                        if (caster != null && caster.State.Features.MythicReduceResistances)
                            continue;
                        if (immunity.IgnoreFeature != null && caster != null && caster.Descriptor.HasFact(immunity.IgnoreFeature))
                            continue;
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Travel fatigue is not the buff. UnitPartWeariness keeps stacks and reapplies
        /// the fatigued or exhausted buff every hour until a rest calls Reset.
        /// </summary>
        private static void ResetTravelWeariness(UnitEntityData unit)
        {
            if (unit == null) return;

            bool inParty = Game.Instance.Player.PartyAndPets.Contains(unit);
            var part = unit.Get<UnitPartWeariness>();
            if (part == null && !inParty) return;
            part ??= unit.Ensure<UnitPartWeariness>();

            // Cooking and similar bonuses live in the extra hours. Rest clears them and
            // then adds them back. This spell is not a rest, so keep the hours already stored.
            float extraHours = 0f;
            try
            {
                extraHours = part.FatigueHoursPeriod - part.FatigueHours;
            }
            catch (Exception e)
            {
                Main.Logger.Error("Failed to read travel weariness hours.", e);
            }

            part.Reset();
            if (extraHours != 0f)
                part.AddWearinessHours(extraHours);
        }

        private class RejuvenatingPoemWearinessReset : IApplyAbilityEffectHandler
        {
            public void OnAbilityEffectApplied(AbilityExecutionContext context) { }

            public void OnAbilityEffectAppliedToTarget(AbilityExecutionContext context, TargetWrapper target) { }

            public void OnTryToApplyAbilityEffect(AbilityExecutionContext context, TargetWrapper target)
            {
                if (context?.AbilityBlueprint != AbilityRefs.RejuvenatingPoem.Reference.Get()) return;
                ResetTravelWeariness(target?.Unit);
            }
        }
    }
}
