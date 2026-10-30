using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Blueprints.Configurators.Items.Equipment;
using BlueprintCore.Blueprints.Configurators.Loot;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using HarmonyLib;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Blueprints.Loot;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Commands.Base;
using System.Collections.Generic;
using static Kingmaker.Visual.Animation.Kingmaker.Actions.UnitAnimationActionCastSpell;

namespace MythicMagicMayhem.Relics
{
    internal static class BlastingBracers
    {
        private const string Weapon = "fe2acd72f8e79884f8b693b1e020a20c";
        private const string ArmorBracers = "bf063663cdb3df34c8ffa05ff1d1c877";
        private const string ChiefKhara = "4b7ba40dd891acf4bbb0582d65b6f506";

        private const string LaserAbility = "BlackwaterBlastingBracersLaserAbility";
        private const string LaserAbilityGuid = "{A4C8E2B1-7D35-4F90-8A16-3E5B9C0D2F74}";
        private const string Item = "BlackwaterBlastingBracersItem";
        private const string ItemGuid = "{C6E0A4D3-9F57-4B12-8C38-5A7D1E2F4B96}";
        private const string Loot = "BlackwaterBlastingBracersLoot";
        private const string LootGuid = "{D7F1B5E4-0A68-4C23-9D49-6B8E2F3A5C07}";

        public static void Configure()
        {
            var source = BlueprintTool.Get<BlueprintItemWeapon>(Weapon);
            var icon = source.Icon != null ? source.Icon : BlueprintTool.Get<BlueprintItemEquipmentWrist>(ArmorBracers).Icon;

            AbilityConfigurator.New(LaserAbility, LaserAbilityGuid)
                .SetDisplayName("BlackwaterBlastingBracers.LaserName")
                .SetDescription("BlackwaterBlastingBracers.LaserDescription")
                .SetIcon(AbilityRefs.ScorchingRay.Reference.Get().Icon)
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Close)
                .SetActionType(UnitCommand.CommandType.Standard)
                .SetSpellResistance(false)
                .SetAutoUseIsForbidden(false)
                .SetAnimation(CastAnimationStyle.Directional)
                .SetEffectOnEnemy(AbilityEffectOnUnit.Harmful)
                .AllowTargeting(point: false, enemies: true, friends: false, self: false)
                .AddSpellDescriptorComponent(SpellDescriptor.Fire)
                .AddAbilityDeliverProjectile(
                    type: AbilityProjectileType.Simple,
                    needAttackRoll: true,
                    weapon: ItemWeaponRefs.RayItem.ToString(),
                    projectiles: new List<Blueprint<BlueprintProjectileReference>> { ProjectileRefs.Scorching_Ray00.ToString() })
                .AddAbilityEffectRunAction(ActionsBuilder.New().DealDamage(
                    value: ContextDice.Value(DiceType.D6, diceCount: 4),
                    damageType: DamageTypes.Energy(DamageEnergyType.Fire)))
                .Configure();

            ItemEquipmentWristConfigurator.New(Item, ItemGuid)
                .CopyFrom(ArmorBracers)
                .SetDisplayNameText("BlackwaterBlastingBracers.Name")
                .SetDescriptionText("BlackwaterBlastingBracers.Description")
                .SetFlavorText("BlackwaterBlastingBracers.Flavor")
                .SetNonIdentifiedNameText("BlackwaterBlastingBracers.Name")
                .SetNonIdentifiedDescriptionText("BlackwaterBlastingBracers.Description")
                .SetIcon(icon)
                .SetAbility(LaserAbility)
                .SetCharges(0)
                .SetSpendCharges(false)
                .SetIsNotable(true)
                .Configure();

            var drop = new LootItem();
            AccessTools.Field(typeof(LootItem), "m_Type").SetValue(drop, LootItemType.Item);
            AccessTools.Field(typeof(LootItem), "m_Item").SetValue(drop, BlueprintTool.GetRef<BlueprintItemReference>(Item));
            UnitLootConfigurator.New(Loot, LootGuid)
                .AddLootItemsPackFixed(count: 1, item: drop)
                .Configure();

            UnitConfigurator.For(ChiefKhara)
                .AddLoot(Loot)
                .Configure();
        }
    }
}
