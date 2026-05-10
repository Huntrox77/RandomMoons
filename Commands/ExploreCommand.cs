using BepInEx.Logging;
using CSync;
using LethalAPI.LibTerminal;
using LethalAPI.LibTerminal.Attributes;
using LethalAPI.LibTerminal.Interactions;
using LethalAPI.LibTerminal.Interfaces;
using LethalAPI.LibTerminal.Models;
using RandomMoons.ConfigUtils;
using RandomMoons.Utils;
using System;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

namespace RandomMoons.Commands;

/// <summary>
/// The main command, explore
/// </summary>
/// 

public class ExploreCommand
{
    // When explore is typed in the terminal
    [TerminalCommand("explore"), CommandInfo("Let you travel to a random moon for free !")]

    public ITerminalInteraction exec(Terminal terminal)
    {
        States.isInteracting = true; // The player will need to confirm / deny
        return new TerminalInteraction() // Starts terminal interaction (confirm / deny)
            .WithPrompt($"You're going to route to a randomly chosen moon, for free.\n\nPlease CONFIRM or DENY")
            .WithHandler(onInteraction);
    }

    private string onInteraction(Terminal terminal, string s)
    {
        if (States.closedUponConfirmation) // If the terminal closed on interaction, execute what the player typed (patches LethalAPI.Terminal issue with the interaction not being cancelled when quitting terminal)
        {
            States.closedUponConfirmation = false;
            States.isInteracting = false; // End of interaction

            terminal.currentNode = new TerminalNode() { name = s };
            terminal.OnSubmit();

            return null;
        }

        if (s.ToLower() == "c" || s.ToLower() == "confirm") // If the player confirms the interaction
        {
            if (StartOfRound.Instance.shipHasLanded || !StartOfRound.Instance.CanChangeLevels()) // If the ship cannot travel
            {
                return "Please wait before travelling to a new moon !";
            }

            if (States.hasGambled && RMConfig.Instance.RestrictedCommandUsage) // If the ship cannot travel
            {
                return "You cannot go to another random moon yet!";
            }

            // Choose a random moon from moons shown in the terminal and travel to it at no cost.
            SelectableLevel moon = ChooseRandomMoon(terminal.moonsCatalogueList); 
            StartOfRound.Instance.ChangeLevelServerRpc(moon.levelID, terminal.groupCredits);

            States.lastVisitedMoon = moon.PlanetName;
            States.isInteracting = false; // End of interaction
            States.hasGambled = true;

            return "A moon has been picked : " + moon.PlanetName + " (" + moon.currentWeather.ToString() + "). Enjoy the trip !";
        }
        else if (s.ToLower() == "d" || s.ToLower() == "deny") // If the player denies the interaction
        {
            States.isInteracting = false; // End of interaction
            return "Route cancelled.";
        }
        else // If the player typed a wrong interaction keyword, execute what the player typed (again patching LethalAPI.Terminal
        {
            States.isInteracting = false; // End of interaction
            terminal.currentNode = new TerminalNode() { name = s };
            terminal.OnSubmit();

            return null;
        }
    }

    // Choose a random moon in a moon list
    public static SelectableLevel ChooseRandomMoon(SelectableLevel[] moons)
    {
        Random random = new();
        int moonIndex = random.Next(0, moons.Length);

        // TODO: redo the way the config works

        MoonSelection type = RMConfig.Instance.MoonSelectionType.Value;

        // Checks moon selection config entry
        // if the config wants vanilla and random is not then recursiv           if the config wants modded and random is vanilla then recursiv
        if (type == MoonSelection.VANILLA && !IsMoonVanilla(moons[moonIndex]) || type == MoonSelection.MODDED && IsMoonVanilla(moons[moonIndex]))
        {
            return ChooseRandomMoon(moons);
        }

        // Checks the register travels config entry
        if (RMConfig.Instance.CheckIfVisitedDuringQuota.Value && States.visitedMoons.Contains(moons[moonIndex].PlanetName))
        {
            return ChooseRandomMoon(moons);
        }

        // Reset visitedMoons list if all the moons have been visited
        if (States.visitedMoons.Count == moons.Length)
        {
            States.visitedMoons = [];
        }

        return moons[moonIndex];
    }

    // Checks if a moon is in the vanillaMoon list
    public static bool IsMoonVanilla(SelectableLevel moon) => States.vanillaMoons.Contains(moon.sceneName);
}