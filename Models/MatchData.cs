using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>
    /// Live match data (populated only when a match is in progress)
    /// and historical result / fixture data.
    /// </summary>
    public sealed class MatchData
    {
        [JsonPropertyName("isInMatch")]
        public bool IsInMatch { get; set; }

        [JsonPropertyName("homeTeam")]
        public string? HomeTeam { get; set; }

        [JsonPropertyName("awayTeam")]
        public string? AwayTeam { get; set; }

        [JsonPropertyName("homeScore")]
        public int? HomeScore { get; set; }

        [JsonPropertyName("awayScore")]
        public int? AwayScore { get; set; }

        [JsonPropertyName("minute")]
        public int? Minute { get; set; }

        /// <summary>Formatted scoreline, e.g. "Arsenal 2–1 Chelsea (67')".</summary>
        [JsonPropertyName("scoreline")]
        public string? Scoreline { get; set; }

        /// <summary>Players currently on the pitch for our club.</summary>
        [JsonPropertyName("currentXI")]
        public List<string>? CurrentXI { get; set; }

        /// <summary>Substitutes available on the bench.</summary>
        [JsonPropertyName("benchPlayers")]
        public List<string>? BenchPlayers { get; set; }

        /// <summary>Substitutions made so far this match.</summary>
        [JsonPropertyName("substitutionsMade")]
        public int? SubstitutionsMade { get; set; }
    }

    /// <summary>A completed match result for form analysis.</summary>
    public sealed class MatchResult
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("opponent")]
        public string? Opponent { get; set; }

        /// <summary>"W", "D", or "L".</summary>
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("score")]
        public string? Score { get; set; }

        [JsonPropertyName("wasHome")]
        public bool? WasHome { get; set; }

        [JsonPropertyName("competition")]
        public string? Competition { get; set; }
    }

    /// <summary>An upcoming scheduled fixture for rotation/planning context.</summary>
    public sealed class UpcomingFixture
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("opponent")]
        public string? Opponent { get; set; }

        [JsonPropertyName("isHome")]
        public bool? IsHome { get; set; }

        [JsonPropertyName("competition")]
        public string? Competition { get; set; }

        /// <summary>Days until this fixture from the current game date.</summary>
        [JsonPropertyName("daysAway")]
        public int? DaysAway { get; set; }
    }
}
