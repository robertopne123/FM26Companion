using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>
    /// Full squad-level attribute identity report.
    /// LocalAnalysis is always populated (pure C#, no API).
    /// ClaudeNarrative is populated after the async Claude call completes.
    /// </summary>
    public sealed class AttributeIdentityReport
    {
        [JsonPropertyName("players")]
        public List<PlayerIdentityResult> Players { get; set; } = new();

        /// <summary>
        /// Filtered shortlist: players whose best-fit role is in a different position group
        /// to their registered position, with a score gap above the misalignment threshold.
        /// </summary>
        [JsonPropertyName("misaligned")]
        public List<PlayerIdentityResult> Misaligned { get; set; } = new();

        /// <summary>Narrative analysis from Claude — null until async call completes.</summary>
        [JsonPropertyName("claudeNarrative")]
        public string? ClaudeNarrative { get; set; }
    }

    /// <summary>Identity result for a single player.</summary>
    public sealed class PlayerIdentityResult
    {
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }

        [JsonPropertyName("registeredPosition")]
        public string? RegisteredPosition { get; set; }

        /// <summary>Position group derived from registered position (e.g. "CB", "CM", "ST").</summary>
        [JsonPropertyName("registeredGroup")]
        public string? RegisteredGroup { get; set; }

        /// <summary>The FM role this player's attributes best fit overall.</summary>
        [JsonPropertyName("bestFitRole")]
        public string? BestFitRole { get; set; }

        /// <summary>Position group of the best-fit role.</summary>
        [JsonPropertyName("bestFitGroup")]
        public string? BestFitGroup { get; set; }

        /// <summary>0–100 fitness score for the best-fit role.</summary>
        [JsonPropertyName("bestFitScore")]
        public double BestFitScore { get; set; }

        /// <summary>
        /// True when bestFitGroup differs from registeredGroup AND
        /// bestFitScore exceeds the best score within the registered group
        /// by more than AttributeAnalyser.MisalignmentThreshold.
        /// </summary>
        [JsonPropertyName("isMisaligned")]
        public bool IsMisaligned { get; set; }

        /// <summary>
        /// The best-scoring role within the player's current registered position group.
        /// Useful for showing what they're "best as" in their current slot even if misaligned.
        /// </summary>
        [JsonPropertyName("bestRoleInCurrentGroup")]
        public string? BestRoleInCurrentGroup { get; set; }

        /// <summary>Score for bestRoleInCurrentGroup.</summary>
        [JsonPropertyName("bestScoreInCurrentGroup")]
        public double BestScoreInCurrentGroup { get; set; }

        /// <summary>
        /// Score gap between best-fit role and best role in current group.
        /// A large gap = strong misalignment signal.
        /// </summary>
        [JsonPropertyName("misalignmentGap")]
        public double MisalignmentGap { get; set; }

        /// <summary>Top 5 role scores across all roles, descending.</summary>
        [JsonPropertyName("topRoles")]
        public List<RoleScore> TopRoles { get; set; } = new();
    }

    /// <summary>A single role-score pairing for a player.</summary>
    public sealed class RoleScore
    {
        [JsonPropertyName("roleName")]
        public string RoleName { get; init; } = string.Empty;

        [JsonPropertyName("positionGroup")]
        public string PositionGroup { get; init; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; init; }
    }
}
