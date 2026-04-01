using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>Current tactical setup: formation, mentality, and all instructions.</summary>
    public sealed class Tactics
    {
        /// <summary>Formation string, e.g. "4-3-3", "4-2-3-1 Wide".</summary>
        [JsonPropertyName("formation")]
        public string? Formation { get; set; }

        /// <summary>Team mentality, e.g. "Attacking", "Balanced", "Defensive".</summary>
        [JsonPropertyName("mentality")]
        public string? Mentality { get; set; }

        /// <summary>Playing style descriptor, e.g. "Tiki-Taka", "Counter", "Direct".</summary>
        [JsonPropertyName("playingStyle")]
        public string? PlayingStyle { get; set; }

        [JsonPropertyName("inPossessionStyle")]
        public string? InPossessionStyle { get; set; }

        [JsonPropertyName("outOfPossessionStyle")]
        public string? OutOfPossessionStyle { get; set; }

        [JsonPropertyName("transitionStyle")]
        public string? TransitionStyle { get; set; }

        /// <summary>Active team instructions (e.g. "Press More", "Float Crosses").</summary>
        [JsonPropertyName("teamInstructions")]
        public List<string>? TeamInstructions { get; set; }

        /// <summary>Per-player role and duty assignments.</summary>
        [JsonPropertyName("playerRoles")]
        public List<PlayerRole>? PlayerRoles { get; set; }
    }

    /// <summary>A single player's role, duty, and individual instructions in the current tactic.</summary>
    public sealed class PlayerRole
    {
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }

        /// <summary>Tactical position slot, e.g. "ML", "DCR", "ST".</summary>
        [JsonPropertyName("slot")]
        public string? Slot { get; set; }

        /// <summary>Role name, e.g. "Mezzala", "Ball Playing Defender".</summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>Duty, e.g. "Attack", "Support", "Defend", "Automatic".</summary>
        [JsonPropertyName("duty")]
        public string? Duty { get; set; }

        [JsonPropertyName("instructions")]
        public List<string>? Instructions { get; set; }
    }
}
