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

    /// <summary>
    /// A single player's FM26 dual-role assignment in the current tactic.
    /// FM26 assigns in-possession and out-of-possession roles independently per slot.
    /// TODO: confirm exact field names from FM26 IL2CPP type once interop DLLs are available.
    /// </summary>
    public sealed class PlayerRole
    {
        [JsonPropertyName("playerName")]
        public string? PlayerName { get; set; }

        /// <summary>
        /// Tactical position slot, e.g. "MCL", "RCM", "AML", "DCR".
        /// FM26 uses the codes: DCL/DC/DCR, DL/DR, WBL/WBR, LDM/DM/RDM,
        /// LCM/MC/RCM, ML/MR, AMCL/AMC/AMCR, AML/AMR, ST.
        /// </summary>
        [JsonPropertyName("slot")]
        public string? Slot { get; set; }

        /// <summary>
        /// The role assigned for when the team is in possession of the ball.
        /// FM26 in-possession role name (e.g. "Mezzala", "Ball-Playing Defender").
        /// TODO: update string values once FM26 role names are confirmed from the game UI.
        /// </summary>
        [JsonPropertyName("inPossessionRole")]
        public string? InPossessionRole { get; set; }

        /// <summary>
        /// The role assigned for when the team is out of possession.
        /// FM26 out-of-possession role name (e.g. "Ball Winner", "Pressing Forward").
        /// TODO: update string values once FM26 role names are confirmed from the game UI.
        /// </summary>
        [JsonPropertyName("outOfPossessionRole")]
        public string? OutOfPossessionRole { get; set; }

        [JsonPropertyName("inPossessionInstructions")]
        public List<string>? InPossessionInstructions { get; set; }

        [JsonPropertyName("outOfPossessionInstructions")]
        public List<string>? OutOfPossessionInstructions { get; set; }
    }
}
