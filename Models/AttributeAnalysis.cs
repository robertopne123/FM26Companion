using System.Collections.Generic;
using System.Text.Json.Serialization;
using static Gaffer.RoleLibrary;

namespace Gaffer.Models
{
    /// <summary>
    /// Full squad-level attribute identity report.
    /// FM26 uses separate in-possession and out-of-possession roles, so each player
    /// receives two independent role scores rather than one combined role.
    /// </summary>
    public sealed class AttributeIdentityReport
    {
        [JsonPropertyName("players")]
        public List<PlayerIdentityResult> Players { get; set; } = new();

        /// <summary>
        /// Players flagged as misaligned on at least one axis (IP or OP),
        /// sorted by the larger of the two misalignment gaps, descending.
        /// </summary>
        [JsonPropertyName("misaligned")]
        public List<PlayerIdentityResult> Misaligned { get; set; } = new();

        /// <summary>Narrative analysis from Claude — null until the async call completes.</summary>
        [JsonPropertyName("claudeNarrative")]
        public string? ClaudeNarrative { get; set; }
    }

    /// <summary>
    /// FM26 dual-role identity result for a single player.
    /// Separates in-possession fit from out-of-possession fit — they are scored
    /// independently because FM26 assigns each role independently.
    /// </summary>
    public sealed class PlayerIdentityResult
    {
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }

        [JsonPropertyName("registeredPosition")]
        public string? RegisteredPosition { get; set; }

        /// <summary>Normalised position group (GK / CB / FB / WB / DM / CM / Wide / AM / ST).</summary>
        [JsonPropertyName("registeredGroup")]
        public string? RegisteredGroup { get; set; }

        // ── In-possession analysis ────────────────────────────────────────────────

        /// <summary>The IP role this player's attributes best fit across all positions.</summary>
        [JsonPropertyName("bestIpRole")]
        public string? BestIpRole { get; set; }

        /// <summary>Position group of the best IP role.</summary>
        [JsonPropertyName("bestIpGroup")]
        public string? BestIpGroup { get; set; }

        /// <summary>0–100 fitness score for the best IP role.</summary>
        [JsonPropertyName("bestIpScore")]
        public double BestIpScore { get; set; }

        /// <summary>Best IP role that falls within the player's registered position group.</summary>
        [JsonPropertyName("bestIpRoleInGroup")]
        public string? BestIpRoleInGroup { get; set; }

        [JsonPropertyName("bestIpScoreInGroup")]
        public double BestIpScoreInGroup { get; set; }

        /// <summary>Score gap: bestIpScore − bestIpScoreInGroup. Large gap = IP misalignment.</summary>
        [JsonPropertyName("ipMisalignmentGap")]
        public double IpMisalignmentGap { get; set; }

        /// <summary>True when best IP group differs from registered group AND gap ≥ threshold.</summary>
        [JsonPropertyName("isIpMisaligned")]
        public bool IsIpMisaligned { get; set; }

        /// <summary>Top 4 IP role scores across all positions, descending.</summary>
        [JsonPropertyName("topIpRoles")]
        public List<RoleScore> TopIpRoles { get; set; } = new();

        // ── Out-of-possession analysis ────────────────────────────────────────────

        [JsonPropertyName("bestOpRole")]
        public string? BestOpRole { get; set; }

        [JsonPropertyName("bestOpGroup")]
        public string? BestOpGroup { get; set; }

        [JsonPropertyName("bestOpScore")]
        public double BestOpScore { get; set; }

        [JsonPropertyName("bestOpRoleInGroup")]
        public string? BestOpRoleInGroup { get; set; }

        [JsonPropertyName("bestOpScoreInGroup")]
        public double BestOpScoreInGroup { get; set; }

        [JsonPropertyName("opMisalignmentGap")]
        public double OpMisalignmentGap { get; set; }

        [JsonPropertyName("isOpMisaligned")]
        public bool IsOpMisaligned { get; set; }

        [JsonPropertyName("topOpRoles")]
        public List<RoleScore> TopOpRoles { get; set; } = new();

        // ── Combined summary ──────────────────────────────────────────────────────

        /// <summary>True if either IP or OP is misaligned.</summary>
        [JsonPropertyName("isMisaligned")]
        public bool IsMisaligned => IsIpMisaligned || IsOpMisaligned;

        /// <summary>The larger of the two misalignment gaps — used for sorting priority.</summary>
        [JsonPropertyName("maxMisalignmentGap")]
        public double MaxMisalignmentGap => System.Math.Max(IpMisalignmentGap, OpMisalignmentGap);
    }

    /// <summary>A single role-score pairing for a player.</summary>
    public sealed class RoleScore
    {
        [JsonPropertyName("roleName")]
        public string RoleName { get; init; } = string.Empty;

        [JsonPropertyName("positionGroup")]
        public string PositionGroup { get; init; } = string.Empty;

        [JsonPropertyName("category")]
        public RoleCategory Category { get; init; }

        [JsonPropertyName("score")]
        public double Score { get; init; }
    }
}
