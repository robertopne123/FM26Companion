using System.Collections.Generic;

namespace FM26Companion;

/// <summary>
/// Single point of truth for all FM26 game-state reads.
///
/// NOTHING else in the plugin reads FM objects directly — all data flows
/// through this class. This keeps IL2CPP-specific interop contained and
/// makes it easy to mock data during development before game classes are
/// identified.
///
/// HOW TO POPULATE THIS FILE
/// ──────────────────────────
/// 1. Run Il2CppDumper against FM26's GameAssembly.dylib (see README.md).
/// 2. Inspect dump.cs / il2cpp.h to identify class and field names.
/// 3. Add the interop DLL for Assembly-CSharp to the .csproj.
/// 4. Replace each TODO stub below with real IL2CPP field/property reads.
///
/// IL2CPP field-read note
/// ───────────────────────
/// In the generated interop DLLs, fields that are value types (int, float,
/// structs) are accessed as normal C# properties. Reference-type fields return
/// Il2CppObjectBase subclasses. String fields come back as Il2CppSystem.String
/// and can be cast to System.String with an explicit cast or .ToString().
///
/// Example pattern once Assembly-CSharp interop is available:
///
///   using SomeFMNamespace;   // from the generated interop DLL
///
///   var manager = GameWorld.s_instance?.currentManager;   // IL2CPP singleton
///   if (manager == null) return MatchData.Empty;
///   return new MatchData
///   {
///       HomeScore = manager.matchScore.homeGoals,   // int field — direct access
///       AwayScore = manager.matchScore.awayGoals,
///       Minute    = (string)manager.matchTime.display,  // Il2CppString → string
///   };
/// </summary>
public sealed class DataReader
{
    public static readonly DataReader Instance = new();
    private DataReader() { }

    // ─────────────────────────────────────────────────────────────────────────
    // Match state
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current live match state, or <see cref="MatchData.Empty"/>
    /// when no match is in progress.
    /// </summary>
    public MatchData GetCurrentMatchData()
    {
        // TODO: locate the FM match controller via IL2CPP interop.
        // Until Assembly-CSharp interop DLLs are generated this returns
        // placeholder data so the rest of the pipeline can be tested.
        return MatchData.Empty with
        {
            HomeTeam  = "Your Club",
            AwayTeam  = "Opponent",
            HomeScore = 0,
            AwayScore = 0,
            Minute    = "0",
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Squad
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a lightweight summary of the current squad.
    /// </summary>
    public IReadOnlyList<PlayerData> GetSquadData()
    {
        // TODO: enumerate the player collection from FM's game world object.
        return System.Array.Empty<PlayerData>();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Individual player
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns detailed attributes for a single player by their FM player ID.
    /// Returns <see langword="null"/> if the player is not found.
    /// </summary>
    public PlayerData? GetPlayerStats(int playerId)
    {
        // TODO: look up player by ID in FM's internal dictionary.
        return null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Lightweight data models — plain C# records, no IL2CPP types.
// The boundary between IL2CPP interop and the rest of the plugin is at the
// DataReader methods above; everything downstream uses these records.
// ─────────────────────────────────────────────────────────────────────────────

public record MatchData(
    string HomeTeam,
    string AwayTeam,
    int    HomeScore,
    int    AwayScore,
    string Minute,
    string Formation,
    float  Possession)
{
    public static readonly MatchData Empty = new(
        HomeTeam    : "",
        AwayTeam    : "",
        HomeScore   : 0,
        AwayScore   : 0,
        Minute      : "",
        Formation   : "",
        Possession  : 0f);

    public bool IsActive => HomeTeam.Length > 0;

    public override string ToString() =>
        $"{HomeTeam} {HomeScore}–{AwayScore} {AwayTeam} ({Minute}')";
}

public record PlayerData(
    int    Id,
    string Name,
    string Position,
    int    CurrentAbility,
    int    Morale,
    int    Fitness);
