using System;
using BepInEx;
using BepInEx.Logging;
using CSync;
using HarmonyLib;
using LethalAPI.LibTerminal;
using LethalAPI.LibTerminal.Models;
using LethalConfig;
using RandomMoons.Commands;
using RandomMoons.ConfigUtils;
using RandomMoons.Patches;
using RandomMoons.Utils;

namespace RandomMoons;

/// <summary>
/// Main plugin class
/// </summary>
[BepInPlugin(modGUID, modName, modVersion)]
[BepInDependency("LethalAPI.Terminal")] // TODO: update version Thunderstore mod id : LethalAPI-LethalAPI_Terminal-1.0.1
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)] // TODO: update version Thunderstore mod id : AinaVT-LethalConfig-1.3.4
[BepInDependency("com.willis.lc.lethalsettings", BepInDependency.DependencyFlags.SoftDependency)] // TODO: update version Thunderstore mod id : willis81808-LethalSettings-1.4.0
[BepInDependency("io.github.CSync")]
public class RandomMoons : BaseUnityPlugin
{
    // Basic mod infos
    internal const string modGUID = "Huntress.RandomMoons";
    internal const string modName = "Huntress's RandomMoons Fork";
    internal const string modVersion = "1.0.5";

    // Harmony instance
    readonly Harmony harmony = new(modGUID);

    // Terminal API Registry Instance
    TerminalModRegistry Commands;

    internal static new ManualLogSource Logger;
    public static new RMConfig Config { get; private set; }

    // Executed at start
    void Awake()
    {
        Logger = base.Logger;

        Config = new(base.Config);
        Config.SyncComplete += CheckSynced;

        // Create Terminal and register our commands.
        Commands = TerminalRegistry.CreateTerminalRegistry();
        Commands.RegisterFrom(new ExploreCommand());
        //Commands.RegisterFrom(new DisplayConfigCommand());

        try {
            Logger.LogInfo("Applying patches...");
            ApplyPluginPatches();

            Logger.LogInfo("Patches applied!");
        } catch(Exception e) {
            Logger.LogError(e);
        }

        // Plugin loaded!
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded and operational. Have fun!");
    }

    // Apply harmony patches
    void ApplyPluginPatches()
    {
        harmony.PatchAll(typeof(RandomMoons));
        Logger.LogInfo("Patched RandomMoons");

        harmony.PatchAll(typeof(TerminalPatch));
        Logger.LogInfo("Patched Terminal");

        harmony.PatchAll(typeof(StartOfRoundPatch));
        Logger.LogInfo("Patched StartOfRound");
    }

    void CheckSynced(bool success) {
        if (!success) {
            Logger.LogWarning("SYNC FAILED");
            States.ConfigStatus = false;
            return;

        }
        if (success)
        {
            Logger.LogInfo("Sync Succesfull ! Have fun !");
            States.ConfigStatus = true;
            return;
        }

        Logger.LogDebug($"Commands registered! SyncedVar: {RMConfig.Instance.SyncedVar}");
    }
}