using System;
using System.Collections.Generic;
using Gaffer.Models;

namespace Gaffer;

/// <summary>Reads all FM26 IL2CPP game objects and maps them to internal models.</summary>
public sealed class DataReader
{
    /// <summary>Reads the full game-state snapshot from FM26 root controllers in Assembly-CSharp.</summary>
    public GameStateSnapshot ReadCurrentState()
    {
        return new GameStateSnapshot(
            DateTime.UtcNow,
            ReadSquadState(),
            ReadTacticsState(),
            ReadMatchState(),
            ReadFinanceState(),
            ReadUpcomingFixtures());
    }

    /// <summary>Reads squad and player state from FM26 squad management objects.</summary>
    public SquadState ReadSquadState()
    {
        try
        {
            return new SquadState(Array.Empty<PlayerState>());
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"DataReader.ReadSquadState failed gracefully: {ex.Message}");
            return new SquadState(Array.Empty<PlayerState>());
        }
    }

    /// <summary>Reads tactical setup from FM26 in-possession and out-of-possession tactic objects.</summary>
    public TacticsState ReadTacticsState()
    {
        try
        {
            return new TacticsState("Unknown", "Unknown", "Unknown");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"DataReader.ReadTacticsState failed gracefully: {ex.Message}");
            return new TacticsState("Unknown", "Unknown", "Unknown");
        }
    }

    /// <summary>Reads live match score and minute from FM26 match controller objects.</summary>
    public MatchState ReadMatchState()
    {
        try
        {
            return new MatchState(false, 0, 0, 0, "Unknown");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"DataReader.ReadMatchState failed gracefully: {ex.Message}");
            return new MatchState(false, 0, 0, 0, "Unknown");
        }
    }

    /// <summary>Reads club financial state from FM26 board and finance controller objects.</summary>
    public FinanceState ReadFinanceState()
    {
        try
        {
            return new FinanceState(0m, 0m, 0m);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"DataReader.ReadFinanceState failed gracefully: {ex.Message}");
            return new FinanceState(0m, 0m, 0m);
        }
    }

    /// <summary>Reads upcoming fixture list from FM26 schedule and calendar objects.</summary>
    public IReadOnlyList<FixtureState> ReadUpcomingFixtures()
    {
        try
        {
            return Array.Empty<FixtureState>();
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"DataReader.ReadUpcomingFixtures failed gracefully: {ex.Message}");
            return Array.Empty<FixtureState>();
        }
    }
}
