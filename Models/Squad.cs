using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>All registered squad players with summary statistics.</summary>
    public sealed class Squad
    {
        [JsonPropertyName("players")]
        public List<Player> Players { get; set; } = new();

        [JsonPropertyName("averageAge")]
        public double? AverageAge { get; set; }

        [JsonPropertyName("averageCondition")]
        public double? AverageCondition { get; set; }

        [JsonPropertyName("averageMorale")]
        public string? DominantMorale { get; set; }

        [JsonPropertyName("injuredCount")]
        public int? InjuredCount { get; set; }

        [JsonPropertyName("suspendedCount")]
        public int? SuspendedCount { get; set; }
    }
}
