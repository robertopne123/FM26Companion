using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Gaffer.Models
{
    /// <summary>
    /// Individual player data. Attributes are nullable — they may not be available
    /// in all game states (e.g. scouted players with hidden attributes).
    /// </summary>
    public sealed class Player
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        /// <summary>The position the player is registered under in the club (e.g. "ST").</summary>
        [JsonPropertyName("registeredPosition")]
        public string? RegisteredPosition { get; set; }

        /// <summary>All positions the player is naturally suited to (e.g. ["ST", "AM"]).</summary>
        [JsonPropertyName("naturalPositions")]
        public List<string>? NaturalPositions { get; set; }

        /// <summary>Physical condition 0–100. Below 70 indicates fatigue risk.</summary>
        [JsonPropertyName("condition")]
        public int? Condition { get; set; }

        [JsonPropertyName("morale")]
        public string? Morale { get; set; }

        [JsonPropertyName("isInjured")]
        public bool? IsInjured { get; set; }

        [JsonPropertyName("isSuspended")]
        public bool? IsSuspended { get; set; }

        /// <summary>Whole years remaining on current contract.</summary>
        [JsonPropertyName("contractYearsRemaining")]
        public double? ContractYearsRemaining { get; set; }

        // ── Season stats ──────────────────────────────────────────────────────────

        [JsonPropertyName("gamesPlayed")]
        public int? GamesPlayed { get; set; }

        [JsonPropertyName("goals")]
        public int? Goals { get; set; }

        [JsonPropertyName("assists")]
        public int? Assists { get; set; }

        /// <summary>Average match rating × 10 (integer avoids float noise in JSON).</summary>
        [JsonPropertyName("averageRatingTimes10")]
        public int? AverageRatingTimes10 { get; set; }

        // ── Attributes ────────────────────────────────────────────────────────────
        // Used for: tactical analysis, position identity, Moneyball scouting.
        // Populated once FM26 IL2CPP attribute field names are identified.

        [JsonPropertyName("attributes")]
        public PlayerAttributes? Attributes { get; set; }
    }

    /// <summary>
    /// FM26 player attributes. All rated 1–20.
    /// Populated by DataReader once IL2CPP field names are confirmed.
    /// </summary>
    public sealed class PlayerAttributes
    {
        // Technical
        [JsonPropertyName("passing")]      public int? Passing      { get; set; }
        [JsonPropertyName("shooting")]     public int? Shooting     { get; set; }
        [JsonPropertyName("dribbling")]    public int? Dribbling    { get; set; }
        [JsonPropertyName("firstTouch")]   public int? FirstTouch   { get; set; }
        [JsonPropertyName("heading")]      public int? Heading      { get; set; }
        [JsonPropertyName("tackling")]     public int? Tackling     { get; set; }
        [JsonPropertyName("crossing")]     public int? Crossing     { get; set; }
        [JsonPropertyName("longShots")]    public int? LongShots    { get; set; }
        [JsonPropertyName("marking")]      public int? Marking      { get; set; }
        [JsonPropertyName("finishing")]    public int? Finishing    { get; set; }

        // Mental
        [JsonPropertyName("decisions")]      public int? Decisions      { get; set; }
        [JsonPropertyName("composure")]      public int? Composure      { get; set; }
        [JsonPropertyName("positioning")]    public int? Positioning    { get; set; }
        [JsonPropertyName("anticipation")]   public int? Anticipation   { get; set; }
        [JsonPropertyName("vision")]         public int? Vision         { get; set; }
        [JsonPropertyName("workRate")]       public int? WorkRate       { get; set; }
        [JsonPropertyName("leadership")]     public int? Leadership     { get; set; }
        [JsonPropertyName("teamwork")]       public int? Teamwork       { get; set; }
        [JsonPropertyName("aggression")]     public int? Aggression     { get; set; }
        [JsonPropertyName("concentration")]  public int? Concentration  { get; set; }
        [JsonPropertyName("bravery")]        public int? Bravery        { get; set; }
        [JsonPropertyName("determination")]  public int? Determination  { get; set; }
        [JsonPropertyName("flair")]          public int? Flair          { get; set; }

        // Physical
        [JsonPropertyName("pace")]         public int? Pace         { get; set; }
        [JsonPropertyName("strength")]     public int? Strength     { get; set; }
        [JsonPropertyName("stamina")]      public int? Stamina      { get; set; }
        [JsonPropertyName("agility")]      public int? Agility      { get; set; }
        [JsonPropertyName("balance")]      public int? Balance      { get; set; }
        [JsonPropertyName("acceleration")] public int? Acceleration { get; set; }
        [JsonPropertyName("jumpingReach")] public int? JumpingReach { get; set; }

        // Goalkeeper (only populated for GK-registered players)
        [JsonPropertyName("reflexes")]     public int? Reflexes     { get; set; }
        [JsonPropertyName("handling")]     public int? Handling     { get; set; }
        [JsonPropertyName("communication")]public int? Communication{ get; set; }
        [JsonPropertyName("oneOnOnes")]    public int? OneOnOnes    { get; set; }
        [JsonPropertyName("aerialReach")]  public int? AerialReach  { get; set; }
    }
}
