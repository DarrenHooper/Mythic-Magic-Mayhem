using BlueprintCore.Blueprints.References;
using MythicMagicMayhem.Mechanics;

namespace MythicMagicMayhem.Azata
{
    internal class AzataSpellTweak
    {
        public static void Patch()
        {
            var book = SpellbookRefs.AzataSpellbook.Reference.Get();
            book.m_SpellsPerDay = SpellbookRefs.AngelSpellbook.Reference.Get().m_SpellsPerDay;
            book.m_SpellsKnown = SpellbookRefs.OracleSpellbook.Reference.Get().m_SpellsKnown;
            SpellStuff.AddSpellLevel(AbilityRefs.Snowball.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 1);
            SpellStuff.AddSpellLevel(AbilityRefs.RemoveFear.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 1);
            SpellStuff.ChangeSpellLevel(AbilityRefs.Rage.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 11, 3);
            SpellStuff.AddSpellLevel(AbilityRefs.Haste.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 3);
            SpellStuff.ChangeSpellLevel(AbilityRefs.FriendlyHug.Reference, SpellListRefs.AzataMythicSpellsSpelllist.Reference, 6, 5);
            SpellStuff.ChangeSpellLevel(AbilityRefs.EuphoricTranquilityCast.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 8, 6);
            SpellStuff.ChangeSpellLevel(AbilityRefs.OverwhelmingPresence.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 9, 6);
            SpellStuff.AddSpellLevel(AbilityRefs.PrismaticSpray.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 7);
            SpellStuff.ChangeSpellLevel(AbilityRefs.HeroNeverSurrender.Reference, SpellListRefs.AzataMythicSpellsSpelllist.Reference, 8, 7);
            SpellStuff.AddSpellLevel(AbilityRefs.Seamantle.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 8);
            SpellStuff.AddSpellLevel(AbilityRefs.PolarRay.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 8);
            SpellStuff.AddSpellLevel(AbilityRefs.SummonElementalElderBase.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 8);
            SpellStuff.AddSpellLevel(AbilityRefs.Stormbolts.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 8);
            SpellStuff.AddSpellLevel(AzataNewSpell.GroupHugConfigure(), SpellListRefs.AzataMythicSpellsSpelllist.Reference, 8);
            //SpellStuff.AddSpellLevel(AbilityRefs.DominateMonster.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 9);
            SpellStuff.AddSpellLevel(AbilityRefs.Tsunami.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 9);
            SpellStuff.AddSpellLevel(AbilityRefs.IcyPrisonMass.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 9);
            //SpellStuff.AddSpellLevel(AbilityRefs.HoldMonsterMass.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 9);
            SpellStuff.AddSpellLevel(AzataNewSpell.ElysiumChoirConfigure(), SpellListRefs.AzataMythicSpellsSpelllist.Reference, 9);
            SpellStuff.ChangeSpellLevel(AbilityRefs.PowerWordStun.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 11, 8);
            SpellStuff.ChangeSpellLevel(AbilityRefs.HeroicInvocation.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 11, 9);
            SpellStuff.ChangeSpellLevel(AbilityRefs.PowerWordKill.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 11, 9);
            SpellStuff.ChangeSpellLevel(AbilityRefs.EvilSuccubusDominate.Reference, SpellListRefs.AzataMythicSpelllist.Reference, 11, 9);
            SpellStuff.AddSpellLevel(AzataNewSpell.DragonDanceConfigure(), SpellListRefs.AzataMythicSpellsSpelllist.Reference, 9);
        }
    }
}
