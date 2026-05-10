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

        if (__instance.CanChangeLevels() && RMConfig.Instance.AutoExplore && !States.hasGambled) // Performs auto explore
        {
            // If there are more than 0 days left, perform the same as explore command, else travel to Galetry (Company Building)
            if (TimeOfDay.Instance.daysUntilDeadline > 0)
            {
                SelectableLevel moon = ExploreCommand.ChooseRandomMoon(terminal.moonsCatalogueList);
                __instance.ChangeLevelServerRpc(moon.levelID, terminal.groupCredits);
                States.lastVisitedMoon = moon.PlanetName;
                States.hasGambled = true;
            }
            else
            {
                SelectableLevel moon = ExploreCommand.ChooseRandomMoon(terminal.moonsCatalogueList);

                if (terminal.moonsCatalogueList.Length < States.companyBuildingLevelID)
                {
                    __instance.ChangeLevelServerRpc(3, terminal.groupCredits);
                    States.lastVisitedMoon = moon.PlanetName;
                    States.hasGambled = true;
                }
                else
                {
                    __instance.ChangeLevelServerRpc(States.companyBuildingLevelID, terminal.groupCredits);
                    States.lastVisitedMoon = moon.PlanetName;
                    States.hasGambled = true;
                }
            }
        }
    }
}