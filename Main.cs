using MythicMagicMayhem.Feats;
using BlueprintCore.Blueprints.Configurators.Root;
using BlueprintCore.Utils;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;
using UnityModManagerNet;
using MythicMagicMayhem.Mechanics;
using MythicMagicMayhem.Angel;
using MythicMagicMayhem.Lich;
using MythicMagicMayhem.Azata;
using MythicMagicMayhem.Aeon;
using MythicMagicMayhem.Demon;
using MythicMagicMayhem.Trickster;

namespace MythicMagicMayhem
{
  public static class Main
  {
    public static bool Enabled;
    public static Settings Settings;
    public static readonly LogWrapper Logger = LogWrapper.Get("MythicMagicMayhem");

    public static bool AzataSpellBalanceChanges => Settings.AzataSpellBalanceChanges;

    public static bool EstimatedHitChance => Settings.EstimatedHitChance;

    public static bool Load(UnityModManager.ModEntry modEntry)
    {
      try
      {
        Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
        ImportModMenuSettings(modEntry);
        modEntry.OnToggle = OnToggle;
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;
        var harmony = new Harmony(modEntry.Info.Id);
        harmony.PatchAll();
        Logger.Info("Finished patching.");
      }
      catch (Exception e)
      {
        Logger.Error("Failed to patch", e);
      }
      return true;
    }

    public static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
      Enabled = value;
      return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
      GUILayout.Label("Restart the game before a blueprint change applies. Do not turn off a feature this save has already used.");
      GUILayout.Label("Estimated Hit Chance applies as soon as you toggle it.");
      if (SpellbookMergeIsEnabled())
      {
        GUILayout.Label("Spellbook Merge is still enabled. Turn it off and restart. Both mods change merging.");
      }

      Settings.LichSpell = GUILayout.Toggle(Settings.LichSpell, "Lich Spell");
      Settings.LichMount = GUILayout.Toggle(Settings.LichMount, "Lich Mount");
      Settings.LichCompanion = GUILayout.Toggle(Settings.LichCompanion, "Lich Companion Feat");
      Settings.LichFortitude = GUILayout.Toggle(Settings.LichFortitude, "Lich Fortitude");
      Settings.AngelSpell = GUILayout.Toggle(Settings.AngelSpell, "Angel Spell");
      Settings.AngelHalo = GUILayout.Toggle(Settings.AngelHalo, "Angel Halo");
      Settings.AzataSpell = GUILayout.Toggle(Settings.AzataSpell, "Azata Spell");
      Settings.AzataSuperpower = GUILayout.Toggle(Settings.AzataSuperpower, "Azata Superpower");
      Settings.AzataSpellBalanceChanges = GUILayout.Toggle(Settings.AzataSpellBalanceChanges, "Azata Spell Balance Changes");
      Settings.AeonSpell = GUILayout.Toggle(Settings.AeonSpell, "Aeon Spell");
      Settings.DemonSpell = GUILayout.Toggle(Settings.DemonSpell, "Demon Spell");
      Settings.TricksterSpell = GUILayout.Toggle(Settings.TricksterSpell, "Trickster Spell");
      Settings.MergableSpellbooks = GUILayout.Toggle(Settings.MergableSpellbooks, "Mergable Spellbooks");
      Settings.EstimatedHitChance = GUILayout.Toggle(Settings.EstimatedHitChance, "Estimated Hit Chance");
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
      Settings.Save(modEntry);
    }

    private static void ImportModMenuSettings(UnityModManager.ModEntry modEntry)
    {
      if (Settings.ImportedModMenuSettings || string.IsNullOrEmpty(Application.persistentDataPath))
        return;

      var generalSettings = Path.Combine(Application.persistentDataPath, "general_settings.json");
      if (!File.Exists(generalSettings))
      {
        Settings.ImportedModMenuSettings = true;
        Settings.Save(modEntry);
        return;
      }

      Settings.ImportedModMenuSettings = true;

      try
      {
        var json = JObject.Parse(File.ReadAllText(generalSettings));
        bool Read(string key, bool fallback)
        {
          var token = json["mod-menu.mmm-settings." + key];
          return token != null && token.Type == JTokenType.Boolean ? token.Value<bool>() : fallback;
        }

        Settings.LichSpell = Read("tg1", Settings.LichSpell);
        Settings.LichMount = Read("tg2", Settings.LichMount);
        Settings.LichCompanion = Read("tg3", Settings.LichCompanion);
        Settings.LichFortitude = Read("tg4", Settings.LichFortitude);
        Settings.AngelSpell = Read("tg5", Settings.AngelSpell);
        Settings.AngelHalo = Read("tg6", Settings.AngelHalo);
        Settings.MergableSpellbooks = Read("tg7", Settings.MergableSpellbooks);
        Settings.AzataSpell = Read("tg9", Settings.AzataSpell);
        Settings.AzataSuperpower = Read("tg10", Settings.AzataSuperpower);
        Settings.AeonSpell = Read("tg11", Settings.AeonSpell);
        Settings.DemonSpell = Read("tg12", Settings.DemonSpell);
        Settings.TricksterSpell = Read("tg13", Settings.TricksterSpell);
        Settings.AzataSpellBalanceChanges = Read("tg14", Settings.AzataSpellBalanceChanges);
        Settings.EstimatedHitChance = Read("tg15", Settings.EstimatedHitChance);
        Logger.Info("Copied toggles from the old ModMenu settings.");
      }
      catch (Exception e)
      {
        Logger.Error("Failed to copy ModMenu settings.", e);
      }

      Settings.Save(modEntry);
    }

    private static bool SpellbookMergeIsEnabled()
    {
      foreach (var entry in UnityModManager.modEntries)
      {
        if (entry.Info.Id == "SpellbookMerge" && entry.Enabled)
          return true;
      }
      return false;
    }

    [HarmonyPatch(typeof(BlueprintsCache))]
    static class BlueprintsCaches_Patch
    {
      private static bool Initialized = false;

      [HarmonyPriority(Priority.First)]
      [HarmonyPatch(nameof(BlueprintsCache.Init)), HarmonyPostfix]
            static void Init()
            {
                try
        {
          if (Initialized)
          {
            Logger.Info("Already configured blueprints.");
            return;
          }
          Initialized = true;

          Logger.Info("Configuring blueprints.");

          //MyFeat.Configure();
                    if (Settings.LichMount) { LichFeature.UndeadMount1Configure(); }
                    if (Settings.LichCompanion) { LichFeature.UndeadMount2Configure(); }
                    if (Settings.LichFortitude) { LichFeature.UnholyFortitudeConfigure(); }
                    if (Settings.AngelHalo) { AngelFeature.NewHalo1Configure(); AngelFeature.NewHalo2Configure(); }
                    if (Settings.AzataSuperpower) { AzataFeature.NewSuperp1Configure(); }
                }
        catch (Exception e)
        {
          Logger.Error("Failed to configure blueprints.", e);
        }
      }
    }

    [HarmonyPatch(typeof(StartGameLoader))]
    static class StartGameLoader_Patch
    {
      private static bool Initialized = false;

      [HarmonyPatch(nameof(StartGameLoader.LoadPackTOC)), HarmonyPostfix]
      static void LoadPackTOC()
      {
        try
        {
          if (Initialized)
          {
            Logger.Info("Already configured delayed blueprints.");
            return;
          }
          Initialized = true;

                    if (Settings.MergableSpellbooks) { MergableSpellbooks.Patch(); }
                    if (Settings.LichSpell) { LichSpell.Patch(); }
                    if (Settings.AngelSpell) { AngelSpell.Patch(); }
                    if (Settings.AzataSpell) { AzataSpellTweak.Patch(); }
                    if (AzataSpellBalanceChanges) { AzataSpellBalance.Patch(); }
                    if (Settings.AeonSpell) { AeonSpellTweak.Patch(); }
                    if (Settings.DemonSpell) { DemonSpellTweak.Patch(); }
                    if (Settings.TricksterSpell) { TricksterSpellTweak.Patch(); }
                    RootConfigurator.ConfigureDelayedBlueprints();
        }
        catch (Exception e)
        {
          Logger.Error("Failed to configure delayed blueprints.", e);
        }
      }
    }
  }
}
