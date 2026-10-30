using UnityModManagerNet;

namespace MythicMagicMayhem
{
    public class Settings : UnityModManager.ModSettings
    {
        public bool LichSpell = true;
        public bool LichMount = true;
        public bool LichCompanion = true;
        public bool LichFortitude = true;
        public bool AngelSpell = true;
        public bool AngelHalo = true;
        public bool AzataSpell = true;
        public bool AzataSuperpower = true;
        public bool AzataSpellBalanceChanges = false;
        public bool AeonSpell = true;
        public bool DemonSpell = true;
        public bool TricksterSpell = true;
        public bool MergableSpellbooks = true;
        public bool EstimatedHitChance = false;
        public bool ImportedModMenuSettings = false;

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }
}
