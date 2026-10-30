using BlueprintCore.Actions.Builder;
using BlueprintCore.Actions.Builder.BasicEx;
using BlueprintCore.Actions.Builder.ContextEx;
using BlueprintCore.Actions.Builder.KingdomEx;
using BlueprintCore.Actions.Builder.StoryEx;
using BlueprintCore.Blueprints.Configurators;
using BlueprintCore.Blueprints.CustomConfigurators;
using BlueprintCore.Blueprints.CustomConfigurators.Classes;
using BlueprintCore.Conditions.Builder.ContextEx;
using HarmonyLib;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Abilities;
using BlueprintCore.Blueprints.CustomConfigurators.UnitLogic.Buffs;
using BlueprintCore.Blueprints.Configurators.Items;
using BlueprintCore.Blueprints.Configurators.Items.Ecnchantments;
using BlueprintCore.Blueprints.Configurators.Items.Equipment;
using BlueprintCore.Blueprints.Configurators.Items.Weapons;
using BlueprintCore.Blueprints.Configurators.Kingdom;
using BlueprintCore.Blueprints.References;
using BlueprintCore.Conditions.Builder;
using BlueprintCore.Conditions.Builder.BasicEx;
using BlueprintCore.Conditions.Builder.StoryEx;
using BlueprintCore.Utils;
using BlueprintCore.Utils.Types;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Classes.Spells;
using Kingmaker;
using Kingmaker.Blueprints.Items;
using Kingmaker.Blueprints.Items.Equipment;
using Kingmaker.Blueprints.Items.Ecnchantments;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.Enums.Damage;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Designers.EventConditionActionSystem.Actions;
using Kingmaker.Designers.Mechanics.Facts;
using Kingmaker.ElementsSystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Items;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.PubSubSystem;
using Kingmaker.RuleSystem;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Abilities;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Abilities.Components;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.UnitLogic.Alignments;
using Kingmaker.UnitLogic.Buffs.Blueprints;
using Kingmaker.UnitLogic.Buffs.Components;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.UnitLogic.FactLogic;
using Kingmaker.UnitLogic.Mechanics;
using Kingmaker.Utility;
using Kingmaker.Kingdom;
using Kingmaker.Kingdom.Actions;
using Kingmaker.Kingdom.Blueprints;
using System;

namespace MythicMagicMayhem.Relics
{
    /// <summary>
    /// Relic decree. The first choice is who works the relic. The second choice is its form.
    /// </summary>
    internal static class BlackwaterInitiative
    {
        private const string PortalStoneRelic = "14cd10f130de35940bff8f2c79607894";
        private const string Quarterstaff = "ada85dae8d12eda4bbe6747bb8b5883c";
        private const string QuarterstaffPlus5 = "32257fb7aaee82946aad584a02d3211a";
        private const string EmpowerSpellFeat = "a1de1e4f92195b442adb946f0e2b9d4e";
        private const string MaximizeSpellFeat = "7f2b282626862e345935bbea5e66424b";
        private const string HeavyCrossbow = "19a5092244dcf99478dcd73c974828b1";
        private const string Enhancement4 = "783d7d496da6ac44f9511011fc5f1979";
        private const string Shock = "7bda5277d36ad114f9f9fd21d0dab658";
        private const string ShockingBurst = "914d7ee77fb09d846924ca08bccee0ff";
        private const string AdamantineWeaponEnchantment = "ab39e7d59dd12f4429ffef5dca88dc7b";
        private const string RollingThunder = "cfc64eff4b9ddef46b8eb0edd411466f";
        private const string PassionsSweetPoison = "2cdc803d9f3e17f4abcef6916f962c60";
        private const string Circlet = "34a3a092b7a90d5429898baeb78b29ed";
        private const string PerfectTiara = "7c177eb33d2446dbb579e133ba7cdb38";
        private const string PerfectTiaraEntity = "ea0c9bcf38aa48f08360108b1e3df2d6";
        private const string HeartOfTheDevice = "1fabd5886aaa4b619d1730870d684152";
        private const string AwakenedDriftwood = "d4fef62a63b0253409828eca8d7cc1f7";

        private const string MageStaffEnchantment = "BlackwaterMageStaffEnchantment";
        private const string MageStaffEnchantmentGuid = "{E1C74A90-2B58-4D36-9F07-8A3E5C1D6B24}";
        private const string TechnicStaffEnchantment = "BlackwaterTechnicStaffEnchantment";
        private const string TechnicStaffEnchantmentGuid = "{B7E2C4A1-9D58-4F06-8C31-5A0E7B3D6F92}";
        private const string GalvanicResource = "BlackwaterGalvanicChargeResource";
        private const string GalvanicResourceGuid = "{C8F3D5B2-0E69-4A17-9D42-6B1F8C4E7A03}";
        private const string GalvanicEmpowerAbility = "BlackwaterGalvanicEmpowerAbility";
        private const string GalvanicEmpowerAbilityGuid = "{D9A4E6C3-1F70-4B28-8E53-7C2A9D5F8B14}";
        private const string GalvanicEmpowerBuff = "BlackwaterGalvanicEmpowerBuff";
        private const string GalvanicEmpowerBuffGuid = "{E0B5F7D4-2A81-4C39-9F64-8D3B0E6A9C25}";
        private const string GalvanicSurgeAbility = "BlackwaterGalvanicSurgeAbility";
        private const string GalvanicSurgeAbilityGuid = "{F1C6A8E5-3B92-4D40-A075-9E4C1F7B0D36}";
        private const string GalvanicSurgeBuff = "BlackwaterGalvanicSurgeBuff";
        private const string GalvanicSurgeBuffGuid = "{A2D7B9F6-4C03-4E51-B186-0F5D2A8C1E47}";
        private const string MageCircletEnchantment = "BlackwaterMageCircletEnchantment";
        private const string MageCircletEnchantmentGuid = "{F7A2C5E8-0D64-4B91-9E52-6C1A9F4B8D07}";
        private const string MageCircletFeature = "BlackwaterMageCircletFeature";
        private const string MageCircletFeatureGuid = "{B3C8E1A4-6F20-4D57-9A18-2E7C5B0D4F63}";
        private const string MageCircletAuraBuff = "BlackwaterMageCircletAuraBuff";
        private const string MageCircletAuraBuffGuid = "{C4D9F2B5-7A31-4E68-8B29-3F8D6C1E5A74}";
        private const string MageCircletArea = "BlackwaterMageCircletArea";
        private const string MageCircletAreaGuid = "{D5E0A3C6-8B42-4F79-9C30-4A9E7D2F6B85}";
        private const string MageCircletWillBuff = "BlackwaterMageCircletWillBuff";
        private const string MageCircletWillBuffGuid = "{E6F1B4D7-9C53-4A80-8D41-5B0F8E3A7C96}";
        private const string ClemencyOfShadows = "881b1782496145a49a1d7747d2a7c11b";
        private const string ClemencyOfShadowsEntity = "4152032ec9144e7d81b26ed9df06b8fb";
        private const string TechnicCircletEnchantment = "BlackwaterTechnicCircletEnchantment";
        private const string TechnicCircletEnchantmentGuid = "{A8C3D6E1-4F70-4B25-9D18-7E2A5C0B3F64}";
        private const string TechnicCircletFeature = "BlackwaterTechnicCircletFeature";
        private const string TechnicCircletFeatureGuid = "{B9D4E7F2-5A81-4C36-8E29-8F3B6D1C4A75}";
        private const string SovereignResource = "BlackwaterSovereignResource";
        private const string SovereignResourceGuid = "{C0E5F8A3-6B92-4D47-9F30-9A4C7E2D5B86}";
        private const string SovereignAbility = "BlackwaterSovereignAbility";
        private const string SovereignAbilityGuid = "{D1F6A9B4-7C03-4E58-A041-0B5D8F3E6C97}";
        private const string SovereignMarkBuff = "BlackwaterSovereignMarkBuff";
        private const string SovereignMarkBuffGuid = "{E2A7B0C5-8D14-4F69-B152-1C6E9A4F7D08}";
        private const string SovereignDominateBuff = "BlackwaterSovereignDominateBuff";
        private const string SovereignDominateBuffGuid = "{F3B8C1D6-9E25-4A70-C263-2D7F0B5A8E19}";

        private const string MageWorkingItem = "BlackwaterMageWorkingItem";
        private const string MageWorkingItemGuid = "{8B2D6F14-9C30-4A57-B8E1-3D5A7C0E2F96}";
        private const string TechnicWorkingItem = "BlackwaterTechnicWorkingItem";
        private const string TechnicWorkingItemGuid = "{1C7E4A90-6D23-4B85-9F18-2A6C8E0B4D37}";

        private const string MageStaffItem = "BlackwaterMageStaffItem";
        private const string MageStaffItemGuid = "{6F3B9D21-8A45-4C07-B2E6-1D9A5C7F0E84}";
        private const string TechnicStaffItem = "BlackwaterTechnicStaffItem";
        private const string TechnicStaffItemGuid = "{A4C1E8B6-3F09-4D72-8A15-7E2B6C0D9F43}";
        private const string MageCircletItem = "BlackwaterMageCircletItem";
        private const string MageCircletItemGuid = "{2D8F5A17-4C6B-4E90-9B31-0A7E3C5D8F62}";
        private const string TechnicCircletItem = "BlackwaterTechnicCircletItem";
        private const string TechnicCircletItemGuid = "{9E0B3C48-7A15-4F26-8D94-6C1E2A7B5D30}";
        private const string AcceleratorEnchantment = "BlackwaterAcceleratorEnchantment";
        private const string AcceleratorEnchantmentGuid = "{D4E7B1A8-6C20-4F53-9A18-2B5C8E0D3F71}";
        private const string AcceleratorResource = "BlackwaterAcceleratorResource";
        private const string AcceleratorResourceGuid = "{E5F8C2B9-7D31-4A64-8B29-3C6D9F1E4A82}";
        private const string AcceleratorAbility = "BlackwaterAcceleratorAbility";
        private const string AcceleratorAbilityGuid = "{F6A9D3C0-8E42-4B75-9C30-4D7E0A2F5B93}";
        private const string AcceleratorBuff = "BlackwaterAcceleratorBuff";
        private const string AcceleratorBuffGuid = "{A7B0E4D1-9F53-4C86-8D41-5E8F1B3A6C04}";
        private const string ThunderboltSonicEnchantment = "BlackwaterThunderboltSonicEnchantment";
        private const string ThunderboltSonicEnchantmentGuid = "{8C1F4A62-0D73-4E95-B2A6-5F7C3D1E9B40}";
        private const string MageCrossbowItem = "BlackwaterMageCrossbowItem";
        private const string MageCrossbowItemGuid = "{5A6C2E90-1D84-4B37-A0F6-8E3C7B1D4A29}";
        private const string TechnicCrossbowItem = "BlackwaterTechnicCrossbowItem";
        private const string TechnicCrossbowItemGuid = "{C7D4A1E2-0B68-4F93-9C25-3A8E6D1F0B74}";

        private const string MageFlag = "BlackwaterMageFlag";
        private const string MageFlagGuid = "{3B9E7C15-6A42-4D08-8F61-2C5A0E8B4D97}";
        private const string TechnicFlag = "BlackwaterTechnicFlag";
        private const string TechnicFlagGuid = "{7F1A4D86-2C90-4B53-A6E8-9D0C3B5F1A24}";

        private const string ReforgeProject = "BlackwaterReforgeProject";
        private const string ReforgeProjectGuid = "{0E6B8A32-5D14-4C79-B3F0-1A8E7C2D6B45}";
        private const string MageProject = "BlackwaterMageProject";
        private const string MageProjectGuid = "{B5C8E0A1-4F37-4D26-9A82-6E1B3C9D0F78}";
        private const string TechnicProject = "BlackwaterTechnicProject";
        private const string TechnicProjectGuid = "{2A4F7C96-8E10-4B53-A1D8-5C6E9B0A3F21}";

        private const string ReforgeEvent = "BlackwaterReforgeEvent";
        private const string ReforgeEventGuid = "{8D3C1F65-0A49-4E72-B6C8-7F2A5E1D4B90}";
        private const string EnchantEvent = "BlackwaterEnchantEvent";
        private const string EnchantEventGuid = "{6C2E9A04-3B71-4D58-8F15-1A7C4E0B6D32}";

        public static void Configure()
        {
            UnlockableFlagConfigurator.New(MageFlag, MageFlagGuid).Configure();
            UnlockableFlagConfigurator.New(TechnicFlag, TechnicFlagGuid).Configure();

            CreateKeyItem(MageWorkingItem, MageWorkingItemGuid, "BlackwaterMageWorking");
            CreateKeyItem(TechnicWorkingItem, TechnicWorkingItemGuid, "BlackwaterTechnicWorking");

            CreateMageStaff();
            CreateTechnicStaff();
            CreateMageCirclet();
            CreateTechnicCirclet();
            CreateMageCrossbow();
            CreateTechnicCrossbow();

            var reforgeEvent = CreateChoiceEvent(
                ReforgeEvent,
                ReforgeEventGuid,
                "BlackwaterReforgeEvent",
                Choice("BlackwaterChoice.Mages", "BlackwaterChoice.MagesResult", ConditionsBuilder.New(), WorkingActions(MageWorkingItem, MageFlag)),
                Choice("BlackwaterChoice.Technic", "BlackwaterChoice.TechnicResult", ConditionsBuilder.New(), WorkingActions(TechnicWorkingItem, TechnicFlag)));

            var enchantEvent = CreateChoiceEvent(
                EnchantEvent,
                EnchantEventGuid,
                "BlackwaterEnchantEvent",
                Choice("BlackwaterChoice.Staff", "BlackwaterChoice.MageStaffResult", HasFlag(MageFlag), HandItem(MageStaffItem)),
                Choice("BlackwaterChoice.Circlet", "BlackwaterChoice.MageCircletResult", HasFlag(MageFlag), WornItem(MageCircletItem)),
                Choice("BlackwaterChoice.Crossbow", "BlackwaterChoice.MageCrossbowResult", HasFlag(MageFlag), HandItem(MageCrossbowItem)),
                Choice("BlackwaterChoice.Staff", "BlackwaterChoice.TechnicStaffResult", HasFlag(TechnicFlag), HandItem(TechnicStaffItem)),
                Choice("BlackwaterChoice.Circlet", "BlackwaterChoice.TechnicCircletResult", HasFlag(TechnicFlag), WornItem(TechnicCircletItem)),
                Choice("BlackwaterChoice.Crossbow", "BlackwaterChoice.TechnicCrossbowResult", HasFlag(TechnicFlag), HandItem(TechnicCrossbowItem)));

            CreateProject(
                ReforgeProject,
                ReforgeProjectGuid,
                "BlackwaterProject",
                new KingdomResourcesAmount { m_Finances = 1000, m_Materials = 100, m_Favors = 0 },
                ConditionsBuilder.New().ItemsEnough(itemToCheck: HeartOfTheDevice, quantity: 1),
                HeartOfTheDevice,
                reforgeEvent);

            CreateProject(
                MageProject,
                MageProjectGuid,
                "BlackwaterEnchantProject",
                new KingdomResourcesAmount { m_Finances = 500, m_Materials = 50, m_Favors = 50 },
                ConditionsBuilder.New().FlagUnlocked(conditionFlag: MageFlag),
                MageWorkingItem,
                enchantEvent);

            CreateProject(
                TechnicProject,
                TechnicProjectGuid,
                "BlackwaterEnchantProject",
                new KingdomResourcesAmount { m_Finances = 500, m_Materials = 50, m_Favors = 50 },
                ConditionsBuilder.New().FlagUnlocked(conditionFlag: TechnicFlag),
                TechnicWorkingItem,
                enchantEvent);

            KingdomRootConfigurator.For(KingdomRootRefs.KingdomRoot)
                .AddToKingdomProjectEvents(ReforgeProject, MageProject, TechnicProject)
                .Configure();

            BlastingBracers.Configure();
        }

        private static void CreateKeyItem(string name, string guid, string textKey)
        {
            ItemConfigurator.New(name, guid)
                .CopyFrom(PortalStoneRelic)
                .SetDisplayNameText(textKey + ".Name")
                .SetDescriptionText(textKey + ".Description")
                .SetFlavorText(textKey + ".Flavor")
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateMageStaff()
        {
            WeaponEnchantmentConfigurator.New(MageStaffEnchantment, MageStaffEnchantmentGuid)
                .SetEnchantName("BlackwaterMageStaff.Name")
                .SetDescription("BlackwaterMageStaff.Enchantment")
                .AddComponent<IncreaseSpellDescriptorCasterLevel>(component =>
                {
                    component.Descriptor = SpellDescriptor.Electricity;
                    component.BonusCasterLevel = 3;
                    component.ModifierDescriptor = ModifierDescriptor.UntypedStackable;
                    component.SpellsOnly = true;
                })
                .AddComponent<DraconicBloodlineArcana>(component =>
                {
                    component.SpellDescriptor = SpellDescriptor.Electricity;
                    component.SpellsOnly = true;
                    component.UseContextBonus = true;
                    component.Value = ContextValues.Constant(2);
                })
                .Configure();

            var appearance = BlueprintTool.Get<BlueprintItemWeapon>(AwakenedDriftwood);
            ItemWeaponConfigurator.New(MageStaffItem, MageStaffItemGuid)
                .CopyFrom(Quarterstaff)
                .SetDisplayNameText("BlackwaterMageStaff.Name")
                .SetDescriptionText("BlackwaterMageStaff.Description")
                .SetFlavorText("BlackwaterMageStaff.Flavor")
                .SetIcon(appearance.Icon)
                .ModifyVisualParameters(visual => visual.m_WeaponModel = appearance.m_VisualParameters.m_WeaponModel)
                .ClearEnchantments()
                .AddToEnchantments(MageStaffEnchantment)
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateTechnicStaff()
        {
            AbilityResourceConfigurator.New(GalvanicResource, GalvanicResourceGuid)
                .SetLocalizedName("BlackwaterGalvanicCharge.Name")
                .SetLocalizedDescription("BlackwaterGalvanicCharge.Description")
                .SetMaxAmount(ResourceAmountBuilder.New(9))
                .Configure();

            var empowerIcon = BlueprintTool.Get<BlueprintFeature>(EmpowerSpellFeat).Icon;
            var maximizeIcon = BlueprintTool.Get<BlueprintFeature>(MaximizeSpellFeat).Icon;
            CreateGalvanicBuff(GalvanicEmpowerBuff, GalvanicEmpowerBuffGuid, "BlackwaterGalvanicEmpower", empowerIcon, Metamagic.Empower);
            CreateGalvanicBuff(
                GalvanicSurgeBuff,
                GalvanicSurgeBuffGuid,
                "BlackwaterGalvanicSurge",
                maximizeIcon,
                Metamagic.Empower | Metamagic.Maximize);
            CreateGalvanicAbility(GalvanicEmpowerAbility, GalvanicEmpowerAbilityGuid, "BlackwaterGalvanicEmpower", empowerIcon, GalvanicEmpowerBuff, 3);
            CreateGalvanicAbility(GalvanicSurgeAbility, GalvanicSurgeAbilityGuid, "BlackwaterGalvanicSurge", maximizeIcon, GalvanicSurgeBuff, 6);

            WeaponEnchantmentConfigurator.New(TechnicStaffEnchantment, TechnicStaffEnchantmentGuid)
                .SetEnchantName("BlackwaterTechnicStaff.Name")
                .SetDescription("BlackwaterTechnicStaff.Enchantment")
                .AddComponent<AddAbilityResources>(component =>
                {
                    component.m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(GalvanicResource);
                    component.Amount = 0;
                    component.RestoreAmount = false;
                })
                .AddComponent<GalvanicChargeOnSpellCast>(component =>
                {
                    component.Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(GalvanicResource);
                })
                .AddComponent<AddFacts>(component =>
                {
                    component.m_Facts = new BlueprintUnitFactReference[]
                    {
                        BlueprintTool.GetRef<BlueprintUnitFactReference>(GalvanicEmpowerAbility),
                        BlueprintTool.GetRef<BlueprintUnitFactReference>(GalvanicSurgeAbility)
                    };
                })
                .Configure();

            var appearance = BlueprintTool.Get<BlueprintItemWeapon>(QuarterstaffPlus5);
            ItemWeaponConfigurator.New(TechnicStaffItem, TechnicStaffItemGuid)
                .CopyFrom(Quarterstaff)
                .SetDisplayNameText("BlackwaterTechnicStaff.Name")
                .SetDescriptionText("BlackwaterTechnicStaff.Description")
                .SetFlavorText("BlackwaterTechnicStaff.Flavor")
                .SetIcon(appearance.Icon)
                .ModifyVisualParameters(visual => visual.m_WeaponModel = appearance.m_VisualParameters.m_WeaponModel)
                .ClearEnchantments()
                .AddToEnchantments(TechnicStaffEnchantment)
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateGalvanicBuff(string name, string guid, string textKey, UnityEngine.Sprite icon, Metamagic metamagic)
        {
            BuffConfigurator.New(name, guid)
                .SetDisplayName(textKey + ".Name")
                .SetDescription(textKey + ".Description")
                .SetIcon(icon)
                .SetStacking(StackingType.Replace)
                .AddComponent<GalvanicMetamagicNextSpell>(component => component.Metamagic = metamagic)
                .Configure();
        }

        private static void CreateGalvanicAbility(string name, string guid, string textKey, UnityEngine.Sprite icon, string buff, int cost)
        {
            AbilityConfigurator.New(name, guid)
                .SetDisplayName(textKey + ".Name")
                .SetDescription(textKey + ".Description")
                .SetIcon(icon)
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Swift)
                .AddAbilityResourceLogic(amount: cost, isSpendResource: true, requiredResource: GalvanicResource)
                .AddAbilityEffectRunAction(ActionsBuilder.New().ApplyBuff(buff, ContextDuration.Fixed(1, DurationRate.Rounds)))
                .Configure();
        }

        private static void CreateMageCrossbow()
        {
            WeaponEnchantmentConfigurator.New(ThunderboltSonicEnchantment, ThunderboltSonicEnchantmentGuid)
                .SetEnchantName("BlackwaterThunderbolt.SonicName")
                .SetDescription("BlackwaterThunderbolt.SonicDescription")
                .AddComponent<ThunderboltSonicBurst>()
                .Configure();

            var appearance = BlueprintTool.Get<BlueprintItemWeapon>(RollingThunder);
            ItemWeaponConfigurator.New(MageCrossbowItem, MageCrossbowItemGuid)
                .CopyFrom(HeavyCrossbow)
                .SetDisplayNameText("BlackwaterMageCrossbow.Name")
                .SetDescriptionText("BlackwaterMageCrossbow.Description")
                .SetFlavorText("BlackwaterMageCrossbow.Flavor")
                .SetIcon(appearance.Icon)
                .ModifyVisualParameters(visual =>
                {
                    visual.m_WeaponModel = appearance.m_VisualParameters.m_WeaponModel;
                    visual.m_WeaponSheathModelOverride = appearance.m_VisualParameters.m_WeaponSheathModelOverride;
                })
                .ClearEnchantments()
                .AddToEnchantments(Enhancement4, ShockingBurst, Shock, AdamantineWeaponEnchantment, ThunderboltSonicEnchantment)
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateTechnicCrossbow()
        {
            var appearance = BlueprintTool.Get<BlueprintItemWeapon>(PassionsSweetPoison);
            AbilityResourceConfigurator.New(AcceleratorResource, AcceleratorResourceGuid)
                .SetLocalizedName("BlackwaterAccelerator.Resource")
                .SetLocalizedDescription("BlackwaterAccelerator.ResourceDescription")
                .SetMaxAmount(ResourceAmountBuilder.New(3))
                .Configure();

            BuffConfigurator.New(AcceleratorBuff, AcceleratorBuffGuid)
                .SetDisplayName("BlackwaterAccelerator.AbilityName")
                .SetDescription("BlackwaterAccelerator.BuffDescription")
                .SetIcon(appearance.Icon)
                .SetStacking(StackingType.Replace)
                .AddAttackTypeChange(
                    changeAllTypes: false,
                    needsWeapon: true,
                    newType: AttackType.RangedTouch,
                    originalType: AttackType.Ranged)
                .AddComponent<HypervelocityShot>()
                .Configure();

            AbilityConfigurator.New(AcceleratorAbility, AcceleratorAbilityGuid)
                .SetDisplayName("BlackwaterAccelerator.AbilityName")
                .SetDescription("BlackwaterAccelerator.AbilityDescription")
                .SetIcon(appearance.Icon)
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Personal)
                .SetCanTargetSelf(true)
                .SetActionType(UnitCommand.CommandType.Swift)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: true, requiredResource: AcceleratorResource)
                .AddAbilityEffectRunAction(ActionsBuilder.New().ApplyBuff(AcceleratorBuff, ContextDuration.Fixed(1, DurationRate.Rounds)))
                .Configure();

            WeaponEnchantmentConfigurator.New(AcceleratorEnchantment, AcceleratorEnchantmentGuid)
                .SetEnchantName("BlackwaterTechnicCrossbow.Name")
                .SetDescription("BlackwaterTechnicCrossbow.Enchantment")
                .AddComponent<AddAbilityResources>(component =>
                {
                    component.m_Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(AcceleratorResource);
                    component.Amount = 3;
                    component.RestoreAmount = true;
                })
                .AddComponent<AddFacts>(component =>
                {
                    component.m_Facts = new BlueprintUnitFactReference[]
                    {
                        BlueprintTool.GetRef<BlueprintUnitFactReference>(AcceleratorAbility)
                    };
                })
                .Configure();

            ItemWeaponConfigurator.New(TechnicCrossbowItem, TechnicCrossbowItemGuid)
                .CopyFrom(HeavyCrossbow)
                .SetDisplayNameText("BlackwaterTechnicCrossbow.Name")
                .SetDescriptionText("BlackwaterTechnicCrossbow.Description")
                .SetFlavorText("BlackwaterTechnicCrossbow.Flavor")
                .SetIcon(appearance.Icon)
                .ModifyVisualParameters(visual => visual.m_WeaponModel = appearance.m_VisualParameters.m_WeaponModel)
                .ClearEnchantments()
                .AddToEnchantments(Enhancement4, AdamantineWeaponEnchantment, AcceleratorEnchantment)
                .SetOverrideDamageDice(true)
                .SetDamageDice(new DiceFormula(2, DiceType.D8))
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateMageCirclet()
        {
            var appearance = BlueprintTool.Get<BlueprintItemEquipmentHead>(PerfectTiara);
            BuffConfigurator.New(MageCircletWillBuff, MageCircletWillBuffGuid)
                .SetDisplayName("BlackwaterMageCircletAura.Name")
                .SetDescription("BlackwaterMageCircletAura.Description")
                .SetIcon(appearance.Icon)
                .SetStacking(StackingType.Replace)
                .AddStatBonus(ModifierDescriptor.UntypedStackable, stat: StatType.SaveWill, value: 2)
                .Configure();

            AbilityAreaEffectConfigurator.New(MageCircletArea, MageCircletAreaGuid)
                .SetShape(AreaEffectShape.Cylinder)
                .SetSize(30.Feet())
                .SetTargetType(BlueprintAbilityAreaEffect.TargetType.Ally)
                .SetAffectEnemies(false)
                .SetAggroEnemies(false)
                .AddAbilityAreaEffectBuff(MageCircletWillBuff, condition: ConditionsBuilder.New().IsAlly())
                .Configure();

            BuffConfigurator.New(MageCircletAuraBuff, MageCircletAuraBuffGuid)
                .SetFlags(BlueprintBuff.Flags.HiddenInUi)
                .AddComponent<AddAreaEffect>(component =>
                {
                    component.m_AreaEffect = BlueprintTool.GetRef<BlueprintAbilityAreaEffectReference>(MageCircletArea);
                })
                .Configure();

            FeatureConfigurator.New(MageCircletFeature, MageCircletFeatureGuid)
                .SetDisplayName("BlackwaterMageCirclet.Name")
                .SetDescription("BlackwaterMageCirclet.Description")
                .SetIcon(appearance.Icon)
                .AddStatBonus(ModifierDescriptor.Enhancement, stat: StatType.Intelligence, value: 4)
                .AddStatBonus(ModifierDescriptor.Enhancement, stat: StatType.Wisdom, value: 4)
                .AddStatBonus(ModifierDescriptor.Enhancement, stat: StatType.Charisma, value: 4)
                .AddSavingThrowBonusAgainstDescriptor(
                    spellDescriptor: new SpellDescriptorWrapper(SpellDescriptor.MindAffecting),
                    modifierDescriptor: ModifierDescriptor.UntypedStackable,
                    value: 2)
                .AddComponent<AuraFeatureComponent>(component =>
                {
                    component.m_Buff = BlueprintTool.GetRef<BlueprintBuffReference>(MageCircletAuraBuff);
                })
                .Configure();

            EquipmentEnchantmentConfigurator.New(MageCircletEnchantment, MageCircletEnchantmentGuid)
                .SetEnchantName("BlackwaterMageCirclet.Name")
                .SetDescription("BlackwaterMageCirclet.Enchantment")
                .AddComponent<AddFacts>(component =>
                {
                    component.m_Facts = new BlueprintUnitFactReference[]
                    {
                        BlueprintTool.GetRef<BlueprintUnitFactReference>(MageCircletFeature)
                    };
                })
                .Configure();

            ItemEquipmentHeadConfigurator.New(MageCircletItem, MageCircletItemGuid)
                .CopyFrom(Circlet)
                .SetDisplayNameText("BlackwaterMageCirclet.Name")
                .SetDescriptionText("BlackwaterMageCirclet.Description")
                .SetFlavorText("BlackwaterMageCirclet.Flavor")
                .SetIcon(appearance.Icon)
                .SetEquipmentEntity(PerfectTiaraEntity)
                .ClearEnchantments()
                .AddToEnchantments(MageCircletEnchantment)
                .SetCharges(0)
                .SetSpendCharges(false)
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateTechnicCirclet()
        {
            if (!NeuralOverrideListener.Patched)
            {
                NeuralOverrideListener.Patched = true;
                new Harmony("MythicMagicMayhem.NeuralOverride").Patch(
                    AccessTools.Method(typeof(RuleSavingThrow), "OnTrigger"),
                    postfix: new HarmonyMethod(typeof(NeuralOverrideListener), nameof(NeuralOverrideListener.AfterSave)));
            }

            var appearance = BlueprintTool.Get<BlueprintItemEquipmentHead>(ClemencyOfShadows);
            var icon = appearance.Icon;
            AbilityResourceConfigurator.New(SovereignResource, SovereignResourceGuid)
                .SetLocalizedName("BlackwaterSovereign.Resource")
                .SetLocalizedDescription("BlackwaterSovereign.ResourceDescription")
                .SetMaxAmount(ResourceAmountBuilder.New(1))
                .Configure();

            BuffConfigurator.New(SovereignMarkBuff, SovereignMarkBuffGuid)
                .SetDisplayName("BlackwaterSovereign.MarkName")
                .SetDescription("BlackwaterSovereign.MarkDescription")
                .SetIcon(icon)
                .SetStacking(StackingType.Replace)
                .SetFlags(BlueprintBuff.Flags.RemoveOnRest)
                .AddComponent<NeuralOverrideMark>()
                .Configure();

            BuffConfigurator.New(SovereignDominateBuff, SovereignDominateBuffGuid)
                .SetDisplayName("BlackwaterSovereign.DominateName")
                .SetDescription("BlackwaterSovereign.DominateDescription")
                .SetIcon(icon)
                .SetStacking(StackingType.Replace)
                .AddComponent<SpellDescriptorComponent>(component => component.Descriptor = SpellDescriptor.MindAffecting | SpellDescriptor.Compulsion)
                .AddComponent<ChangeFaction>(component =>
                {
                    component.m_Type = ChangeFaction.ChangeType.ToCaster;
                    component.m_AllowDirectControl = false;
                })
                .Configure();

            AbilityConfigurator.New(SovereignAbility, SovereignAbilityGuid)
                .SetDisplayName("BlackwaterSovereign.AbilityName")
                .SetDescription("BlackwaterSovereign.AbilityDescription")
                .SetIcon(icon)
                .SetType(AbilityType.Supernatural)
                .SetRange(AbilityRange.Medium)
                .SetCanTargetEnemies(true)
                .SetActionType(UnitCommand.CommandType.Free)
                .AddAbilityResourceLogic(amount: 1, isSpendResource: false, requiredResource: SovereignResource)
                .AddAbilityEffectRunAction(ActionsBuilder.New().ApplyBuff(SovereignMarkBuff, ContextDuration.Fixed(1, DurationRate.Days)))
                .Configure();

            FeatureConfigurator.New(TechnicCircletFeature, TechnicCircletFeatureGuid)
                .SetDisplayName("BlackwaterTechnicCirclet.Name")
                .SetDescription("BlackwaterTechnicCirclet.Description")
                .SetIcon(icon)
                .AddStatBonus(ModifierDescriptor.Enhancement, stat: StatType.Charisma, value: 6)
                .AddIncreaseSpellDescriptorDC(
                    bonusDC: 2,
                    descriptor: new SpellDescriptorWrapper(SpellDescriptor.MindAffecting),
                    modifierDescriptor: ModifierDescriptor.UntypedStackable,
                    spellsOnly: true)
                .AddAbilityResources(amount: 1, resource: SovereignResource, restoreAmount: true)
                .AddFacts(new System.Collections.Generic.List<BlueprintCore.Utils.Blueprint<BlueprintUnitFactReference>> { SovereignAbility })
                .AddComponent<NeuralOverrideListener>(component =>
                {
                    component.Resource = BlueprintTool.GetRef<BlueprintAbilityResourceReference>(SovereignResource);
                    component.Mark = BlueprintTool.GetRef<BlueprintBuffReference>(SovereignMarkBuff);
                    component.Dominate = BlueprintTool.GetRef<BlueprintBuffReference>(SovereignDominateBuff);
                })
                .Configure();

            EquipmentEnchantmentConfigurator.New(TechnicCircletEnchantment, TechnicCircletEnchantmentGuid)
                .SetEnchantName("BlackwaterTechnicCirclet.Name")
                .SetDescription("BlackwaterTechnicCirclet.Enchantment")
                .AddComponent<AddFacts>(component =>
                {
                    component.m_Facts = new BlueprintUnitFactReference[]
                    {
                        BlueprintTool.GetRef<BlueprintUnitFactReference>(TechnicCircletFeature)
                    };
                })
                .Configure();

            ItemEquipmentHeadConfigurator.New(TechnicCircletItem, TechnicCircletItemGuid)
                .CopyFrom(Circlet)
                .SetDisplayNameText("BlackwaterTechnicCirclet.Name")
                .SetDescriptionText("BlackwaterTechnicCirclet.Description")
                .SetFlavorText("BlackwaterTechnicCirclet.Flavor")
                .SetIcon(icon)
                .SetEquipmentEntity(ClemencyOfShadowsEntity)
                .ClearEnchantments()
                .AddToEnchantments(TechnicCircletEnchantment)
                .SetCharges(0)
                .SetSpendCharges(false)
                .SetIsNotable(true)
                .Configure();
        }

        private static void CreateCirclet(string name, string guid, string textKey)
        {
            ItemEquipmentHeadConfigurator.New(name, guid)
                .CopyFrom(Circlet)
                .SetDisplayNameText(textKey + ".Name")
                .SetDescriptionText(textKey + ".Description")
                .SetFlavorText(textKey + ".Flavor")
                .ClearEnchantments()
                .SetCharges(0)
                .SetSpendCharges(false)
                .SetIsNotable(true)
                .Configure();
        }

        private static BlueprintCrusadeEvent CreateChoiceEvent(string name, string guid, string textKey, params EventSolution[] choices)
        {
            return CrusadeEventConfigurator.New(name, guid)
                .SetLocalizedName(textKey + ".Name")
                .SetLocalizedDescription(textKey + ".Description")
                .SetResolutionTime(14)
                .SetSkipRoll(true)
                .SetResolutionDC(0)
                .SetAutoResolveResult(EventResult.MarginType.Success)
                .SetDefaultResolutionType(LeaderType.Counselor)
                .SetNeedToVisitTheThroneRoom(false)
                .SetResolveAutomatically(false)
                .SetTriggerCondition(ConditionsBuilder.New())
                .SetSolutions(NoLeader())
                .AddToEventSolutions(choices)
                .Configure();
        }

        private static void CreateProject(
            string name,
            string guid,
            string textKey,
            KingdomResourcesAmount cost,
            ConditionsBuilder trigger,
            string spentItem,
            BlueprintCrusadeEvent next)
        {
            KingdomProjectConfigurator.New(name, guid)
                .SetLocalizedName(textKey + ".Name")
                .SetLocalizedDescription(textKey + ".Description")
                .SetMechanicalDescription(textKey + ".Mechanical")
                .SetDefaultResolutionDescription(textKey + ".Success")
                .SetProjectType(KingdomProjectType.Relics)
                .SetProjectStartCost(cost)
                .SetResolutionTime(5)
                .SetSkipRoll(true)
                .SetResolutionDC(1)
                .SetAutoResolveResult(EventResult.MarginType.Success)
                .SetDefaultResolutionType(LeaderType.Diplomat)
                .SetRepeatable(false)
                .SetNeedToVisitTheThroneRoom(false)
                .SetResolveAutomatically(false)
                .SetTriggerCondition(trigger)
                .AddEventItemCost(amount: 1, items: new() { spentItem })
                .SetSolutions(DiplomatSuccess(ProjectOutcome(spentItem, next), textKey + ".Success"))
                .Configure();
        }

        private static ActionList WorkingActions(string item, string flag)
        {
            return ActionsBuilder.New()
                .GiveItemToPlayer(item, identify: true, quantity: 1)
                .UnlockFlag(flag, flagValue: 1)
                .Build();
        }

        private static ActionList HandItem(string item)
        {
            return ActionsBuilder.New()
                .GiveHandSlotItemToPlayer(item, identify: true, quantity: 1)
                .Build();
        }

        private static ActionList WornItem(string item)
        {
            return ActionsBuilder.New()
                .GiveEquipmentToPlayer(item, identify: true, quantity: 1)
                .Build();
        }

        private static ConditionsBuilder HasFlag(string flag)
        {
            return ConditionsBuilder.New().FlagUnlocked(conditionFlag: flag);
        }

        private static EventSolution Choice(string buttonKey, string resultKey, ConditionsBuilder available, ActionList effects)
        {
            return new EventSolution
            {
                m_SolutionText = Text(buttonKey),
                m_ResultText = Text(resultKey),
                m_AvailConditions = available.Build(),
                m_UnavailingBehaviour = UnavailingBehaviour.HideSolution,
                m_UnavailingPlaceholder = Text("Blackwater.Unavailable"),
                m_SuccessEffects = effects
            };
        }

        private static ActionList ProjectOutcome(string spentItem, BlueprintCrusadeEvent next)
        {
            var followUp = ActionsBuilder.New()
                .KingdomActionStartEvent(eventValue: next.AssetGuid.ToString())
                .Build();
            var actions = new GameAction[followUp.Actions.Length + 1];
            actions[0] = new RemoveItemFromPlayer
            {
                m_ItemToRemove = BlueprintTool.GetRef<BlueprintItemReference>(spentItem),
                Quantity = 1
            };
            Array.Copy(followUp.Actions, 0, actions, 1, followUp.Actions.Length);
            return new ActionList { Actions = actions };
        }

        private static PossibleEventSolutions DiplomatSuccess(ActionList success, string successKey)
        {
            EventResult[] Results(bool solved)
            {
                return new[]
                {
                    Result(EventResult.MarginType.GreatFail, EmptyActions(), ""),
                    Result(EventResult.MarginType.Fail, EmptyActions(), ""),
                    Result(EventResult.MarginType.Success, solved ? success : EmptyActions(), solved ? successKey : ""),
                    Result(EventResult.MarginType.GreatSuccess, EmptyActions(), "")
                };
            }

            return new PossibleEventSolutions
            {
                Entries = new[]
                {
                    Entry(LeaderType.Counselor, false, Results(false)),
                    Entry(LeaderType.Strategist, false, Results(false)),
                    Entry(LeaderType.Diplomat, true, Results(true)),
                    Entry(LeaderType.General, false, Results(false))
                }
            };
        }

        private static PossibleEventSolutions NoLeader()
        {
            PossibleEventSolution Blank(LeaderType leader)
            {
                return new PossibleEventSolution
                {
                    Leader = leader,
                    CanBeSolved = false,
                    Resolutions = Array.Empty<EventResult>()
                };
            }

            return new PossibleEventSolutions
            {
                Entries = new[]
                {
                    Blank(LeaderType.Counselor),
                    Blank(LeaderType.Strategist),
                    Blank(LeaderType.Diplomat),
                    Blank(LeaderType.General)
                }
            };
        }

        private static PossibleEventSolution Entry(LeaderType leader, bool solved, EventResult[] resolutions)
        {
            return new PossibleEventSolution
            {
                Leader = leader,
                CanBeSolved = solved,
                Resolutions = resolutions
            };
        }

        private static EventResult Result(EventResult.MarginType margin, ActionList actions, string key)
        {
            return new EventResult
            {
                Margin = margin,
                LeaderAlignment = AlignmentMaskType.Any,
                Condition = ConditionsBuilder.New().Build(),
                Actions = actions,
                StatChanges = new KingdomStats.Changes
                {
                    m_Changes = new int[4],
                    ResourcesPerTurn = new KingdomResourcesAmount(),
                    ResourcesOneTime = new KingdomResourcesAmount()
                },
                LocalizedDescription = Text(key)
            };
        }

        private static ActionList EmptyActions()
        {
            return new ActionList { Actions = Array.Empty<GameAction>() };
        }

        private static LocalizedString Text(string key)
        {
            return new LocalizedString { m_Key = key };
        }
    }

    internal class HypervelocityShot : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleAttackRoll>, IRulebookHandler<RuleAttackRoll>, IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>, ISubscriber, IInitiatorRulebookSubscriber
    {
        private static readonly BlueprintGuid Accelerator = BlueprintGuid.Parse("{C7D4A1E2-0B68-4F93-9C25-3A8E6D1F0B74}");

        public void OnEventAboutToTrigger(RuleAttackRoll evt)
        {
        }

        public void OnEventDidTrigger(RuleAttackRoll evt)
        {
            if (evt != null && !evt.IsHit && IsAccelerator(evt.Weapon))
                Buff.Remove();
        }

        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (!IsAccelerator(evt.DamageBundle.Weapon))
                return;

            var damage = new PhysicalDamage(new ModifiableDiceFormula(new DiceFormula(2, DiceType.D8)), 0, PhysicalDamageForm.Piercing);
            damage.AddMaterial(PhysicalDamageMaterial.Adamantite);
            evt.Add(damage);
        }

        public void OnEventDidTrigger(RulePrepareDamage evt)
        {
            if (IsAccelerator(evt.DamageBundle.Weapon))
                Buff.Remove();
        }

        private static bool IsAccelerator(ItemEntityWeapon weapon)
        {
            return weapon?.Blueprint != null && weapon.Blueprint.AssetGuid == Accelerator;
        }
    }

    internal class ThunderboltSonicBurst : WeaponEnchantmentLogic, IInitiatorRulebookHandler<RulePrepareDamage>, IRulebookHandler<RulePrepareDamage>, ISubscriber, IInitiatorRulebookSubscriber
    {
        public void OnEventAboutToTrigger(RulePrepareDamage evt)
        {
            if (evt.DamageBundle.Weapon != Owner || Owner == null)
                return;

            var attack = (evt.ParentRule as RuleDealDamage)?.AttackRoll;
            if (attack == null || !attack.IsCriticalConfirmed || attack.FortificationNegatesCriticalHit)
                return;

            evt.Add(new EnergyDamage(new DiceFormula(2, DiceType.D6), DamageEnergyType.Sonic));
        }

        public void OnEventDidTrigger(RulePrepareDamage evt)
        {
        }
    }

    public class GalvanicChargeOnSpellCast : UnitFactComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, ISubscriber, IInitiatorRulebookSubscriber
    {
        public BlueprintAbilityResourceReference Resource;

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (!GalvanicSpell.IsElectricitySpell(evt))
                return;

            Owner.Resources.Restore(Resource, 1);
        }
    }

    public class GalvanicMetamagicNextSpell : UnitBuffComponentDelegate, IInitiatorRulebookHandler<RuleCastSpell>, IRulebookHandler<RuleCastSpell>, ISubscriber, IInitiatorRulebookSubscriber
    {
        public Metamagic Metamagic;

        public void OnEventAboutToTrigger(RuleCastSpell evt)
        {
            if (!GalvanicSpell.IsElectricitySpell(evt))
                return;

            var data = evt.Spell.MetamagicData ?? new MetamagicData();
            var cost = data.SpellLevelCost;
            data.Add(Metamagic);
            data.SpellLevelCost = cost;
            evt.Spell.MetamagicData = data;
            if (evt.Context != null)
                evt.Context.Params.Metamagic |= Metamagic;
        }

        public void OnEventDidTrigger(RuleCastSpell evt)
        {
            if (GalvanicSpell.IsElectricitySpell(evt))
                Buff.Remove();
        }
    }

    internal static class GalvanicSpell
    {
        public static bool IsElectricitySpell(RuleCastSpell evt)
        {
            var spell = evt?.Spell;
            if (spell?.Blueprint == null || spell.Spellbook == null || evt.IsDuplicateSpellApplied)
                return false;

            return (spell.SpellDescriptor & SpellDescriptor.Electricity) != 0;
        }
    }

    public class NeuralOverrideMark : UnitBuffComponentDelegate
    {
        public override void OnTurnOn()
        {
            var caster = Buff.Context?.MaybeCaster;
            if (caster == null)
                return;

            foreach (var unit in Game.Instance.State.Units)
            {
                if (unit == Owner)
                    continue;

                var others = new System.Collections.Generic.List<Buff>();
                foreach (var other in unit.Buffs)
                    others.Add(other);
                foreach (var other in others)
                {
                    if (other.Blueprint == Buff.Blueprint && other.Context?.MaybeCaster == caster)
                        other.Remove();
                }
            }
        }
    }

    public class NeuralOverrideListener : UnitFactComponentDelegate
    {
        public static bool Patched;
        private static bool Resolving;

        public BlueprintAbilityResourceReference Resource;
        public BlueprintBuffReference Mark;
        public BlueprintBuffReference Dominate;

        public static void AfterSave(RuleSavingThrow save)
        {
            if (Resolving || save == null || save.IsPassed)
                return;

            try
            {
                var context = save.Reason?.Context as AbilityExecutionContext;
                var caster = context?.Caster;
                var failed = save.Initiator;
                if (caster == null || failed == null || context.IsDuplicateSpellApplied)
                    return;
                if (!IsMindAffectingSpell(context.AbilityBlueprint) || !caster.IsEnemy(failed))
                    return;

                foreach (var fact in caster.Facts.List)
                {
                    foreach (var component in fact.Components)
                    {
                        if (component.SourceBlueprintComponent is NeuralOverrideListener listener)
                        {
                            listener.Seize(save, caster, failed);
                            return;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Main.Logger.Error("Neural Override failed.", ex);
            }
        }

        private void Seize(RuleSavingThrow save, UnitEntityData caster, UnitEntityData failed)
        {
            if (caster.Resources.GetResourceAmount(Resource) < 1)
                return;

            var marked = FindMark(caster);
            if (marked == null || marked == failed || marked.Descriptor.State.IsDead || !caster.IsEnemy(marked))
                return;
            if (IsImmune(marked, caster))
                return;

            Resolving = true;
            try
            {
                var will = new RuleSavingThrow(marked, SavingThrowType.Will, save.DifficultyClass) { Reason = save.Reason };
                Rulebook.Trigger(will);
                caster.Resources.Spend(Resource, 1);
                RemoveMark(caster);
                if (!will.IsPassed)
                    marked.Descriptor.AddBuff(Dominate, caster, 1.Rounds().Seconds, null);
            }
            finally
            {
                Resolving = false;
            }
        }

        private UnitEntityData FindMark(UnitEntityData caster)
        {
            var mark = Mark.Get();
            foreach (var unit in Game.Instance.State.Units)
            {
                foreach (var buff in unit.Buffs)
                {
                    if (buff.Blueprint == mark && buff.Context?.MaybeCaster == caster)
                        return unit;
                }
            }

            return null;
        }

        private void RemoveMark(UnitEntityData caster)
        {
            var mark = Mark.Get();
            foreach (var unit in Game.Instance.State.Units)
            {
                var found = new System.Collections.Generic.List<Buff>();
                foreach (var buff in unit.Buffs)
                {
                    if (buff.Blueprint == mark && buff.Context?.MaybeCaster == caster)
                        found.Add(buff);
                }
                foreach (var buff in found)
                    buff.Remove();
            }
        }

        private static bool IsMindAffectingSpell(BlueprintAbility ability)
        {
            return ability != null
                && ability.Type == AbilityType.Spell
                && (ability.SpellDescriptor & SpellDescriptor.MindAffecting) != 0;
        }

        private static bool IsImmune(UnitEntityData unit, UnitEntityData caster)
        {
            var resistance = unit.Get<UnitPartSpellResistance>();
            if (resistance != null && (resistance.IgnoreSpellDescriptorImmunities & SpellDescriptor.MindAffecting) != 0)
                return false;

            foreach (var fact in unit.Facts.List)
            {
                foreach (var component in fact.Components)
                {
                    if (component.SourceBlueprintComponent is SpellImmunityToSpellDescriptor spellImmunity
                        && (spellImmunity.Descriptor.m_IntValue & (int)SpellDescriptor.MindAffecting) != 0)
                    {
                        if (spellImmunity.CasterIgnoreImmunityFact != null && caster.Descriptor.HasFact(spellImmunity.CasterIgnoreImmunityFact))
                            continue;
                        return true;
                    }

                    if (component.SourceBlueprintComponent is BuffDescriptorImmunity buffImmunity
                        && (buffImmunity.Descriptor.m_IntValue & (int)SpellDescriptor.MindAffecting) != 0
                        && !caster.State.Features.MythicReduceResistances)
                        return true;
                }
            }

            return false;
        }
    }
}
