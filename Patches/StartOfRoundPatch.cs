using System.Threading;
using HarmonyLib;
using RandomMoons.Commands;
using RandomMoons.ConfigUtils;
using RandomMoons.Utils;
using UnityEngine;

namespace RandomMoons.Patches;

/// <summary>
/// Patches StartOfRound. Learn to read
/// </summary>
[HarmonyPatch(typeof(StartOfRound))]
internal class StartOfRoundPatch
{
    private static Terminal terminal = Object.FindObjectOfType<Terminal>(); // Find script Terminal.cs

    // Uses basically all the states
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static void UpdatePatch(StartOfRound __instance)
    {
        // Add moon to visitedMoons
        if (__instance.shipHasLanded && States.hasGambled)
        {
            States.hasGambled = false;
            States.visitedMoons.Add(States.lastVisitedMoon);
        }

        // Reset visitedMoons when game over
        if (__instance.suckingPlayersOutOfShip)
        {
            States.visitedMoons = [];
        }

        if (__instance.CanChangeLevels() && States.exploreASAP) // Performs auto explore
        {
            // If there are more than 0 days left, perform the same as explore command, else travel to Gordion (Company Building)
            if (TimeOfDay.Instance.daysUntilDeadline > 0 || __instance.currentLevelID == States.companyBuildingLevelID)
            {
                SelectableLevel moon = ExploreCommand.ChooseRandomMoon(terminal.moonsCatalogueList);
                __instance.ChangeLevelServerRpc(moon.levelID, terminal.groupCredits);
                States.lastVisitedMoon = moon.PlanetName;
                States.hasGambled = true;
            }
            else {
                SelectableLevel moon = ExploreCommand.ChooseRandomMoon(terminal.moonsCatalogueList);

                if (terminal.moonsCatalogueList.Length < States.companyBuildingLevelID)
                {
                    __instance.ChangeLevelServerRpc(3, terminal.groupCredits);
                }
                else
                {
                    __instance.ChangeLevelServerRpc(States.companyBuildingLevelID, terminal.groupCredits);
                }
            }
        }
    }

    [HarmonyPatch("ArriveAtLevel")]
    [HarmonyPostfix]
    public static void ArriveAtLevelPatch()
    {
        Thread.Sleep(1000);

        GameObject startLever = GameObject.Find("StartGameLever"); // Find ship's level game object
        if (startLever == null) return;

        StartMatchLever startMatchLever = startLever.GetComponent<StartMatchLever>(); // Find script component for the game object
        if (startMatchLever == null) return;

        startMatchLever.PullLever(); // Pulls the lever
        startMatchLever.LeverAnimation(); // Plays the animation
        startMatchLever.StartGame(); // Starts the level
    }

    [HarmonyPatch("ChangeLevel")]
    [HarmonyPrefix]
    public static void ChangeLevelPatch()
    {
        if (States.exploreASAP)
            States.exploreASAP = false;
    }

    [HarmonyPatch("EndOfGame")]
    [HarmonyPostfix]
    public static void EndOfGamePatch()
    {
        States.exploreASAP = true;
    }
}