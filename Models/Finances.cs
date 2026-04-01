using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>Club financial position for budget and wage context.</summary>
    public sealed class Finances
    {
        /// <summary>Available transfer budget in the club's currency (raw value).</summary>
        [JsonPropertyName("transferBudget")]
        public long? TransferBudget { get; set; }

        /// <summary>Remaining weekly wage budget available for new signings.</summary>
        [JsonPropertyName("wageBudgetPerWeek")]
        public long? WageBudgetPerWeek { get; set; }

        /// <summary>Current total weekly wage bill across all squad contracts.</summary>
        [JsonPropertyName("currentWageBillPerWeek")]
        public long? CurrentWageBillPerWeek { get; set; }

        /// <summary>Club bank balance / financial reserves.</summary>
        [JsonPropertyName("clubBalance")]
        public long? ClubBalance { get; set; }

        /// <summary>Qualitative descriptor, e.g. "Healthy", "Tight", "Struggling".</summary>
        [JsonPropertyName("financialState")]
        public string? FinancialState { get; set; }

        /// <summary>ISO 4217 currency code, e.g. "GBP", "EUR".</summary>
        [JsonPropertyName("currencyCode")]
        public string? CurrencyCode { get; set; }
    }
}
