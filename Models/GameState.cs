using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>
    /// Root aggregate of all game state sent to Claude as JSON context.
    /// Populated by DataReader.GetCurrentGameState() and serialised into the system prompt.
    /// All properties are nullable — DataReader may not be able to read every field
    /// depending on current game state (e.g. no match in progress, pre-season, etc.).
    ///
    /// Phase 2 note: this object is the JSON payload shape for the future mobile companion
    /// backend — keep it flat and serialisable with no circular references.
    /// </summary>
    public sealed class GameState
    {
        [JsonPropertyName("clubName")]
        public string? ClubName { get; set; }

        [JsonPropertyName("managerName")]
        public string? ManagerName { get; set; }

        [JsonPropertyName("leaguePosition")]
        public int? LeaguePosition { get; set; }

        [JsonPropertyName("currentDate")]
        public string? CurrentDate { get; set; }

        [JsonPropertyName("squad")]
        public Squad? Squad { get; set; }

        [JsonPropertyName("tactics")]
        public Tactics? Tactics { get; set; }

        /// <summary>Populated only when a match is in progress.</summary>
        [JsonPropertyName("currentMatch")]
        public MatchData? CurrentMatch { get; set; }

        [JsonPropertyName("finances")]
        public Finances? Finances { get; set; }

        [JsonPropertyName("recentResults")]
        public List<MatchResult>? RecentResults { get; set; }

        [JsonPropertyName("upcomingFixtures")]
        public List<UpcomingFixture>? UpcomingFixtures { get; set; }
    }
}
