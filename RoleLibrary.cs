using System;
using System.Collections.Generic;
using System.Linq;
using Gaffer.Models;

namespace Gaffer
{
    /// <summary>
    /// Defines every FM26 role as an attribute weight vector and provides
    /// the scoring engine that computes how well a player's attributes
    /// fit each role.
    ///
    /// Scoring formula:
    ///   score = Σ(attributeValue × weight) / Σ(20 × weight) × 100
    /// Produces 0–100 where 100 = every key attribute is 20/20.
    /// Attributes not present in the player's data are assumed 10 (league average).
    ///
    /// Weights are sourced from FM's published role key/preferred attribute lists
    /// and weighted by whether an attribute is "key" (1.0), "preferred" (0.7),
    /// or "relevant" (0.4) to the role.
    /// </summary>
    internal static class RoleLibrary
    {
        // ── Position group mapping ─────────────────────────────────────────────────

        private static readonly Dictionary<string, string> PositionGroupMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // Goalkeepers
            ["GK"] = "GK",
            // Centre-backs
            ["DC"] = "CB", ["DCL"] = "CB", ["DCR"] = "CB", ["CB"] = "CB",
            // Full-backs
            ["DL"] = "FB", ["DR"] = "FB",
            // Wing-backs
            ["WBL"] = "WB", ["WBR"] = "WB",
            // Defensive mid
            ["DM"] = "DM", ["DMC"] = "DM",
            // Central mid
            ["ML"] = "CM", ["MC"] = "CM", ["MR"] = "CM",
            // Attacking mid
            ["AML"] = "AM", ["AMC"] = "AM", ["AMR"] = "AM",
            // Wide / Wingers
            ["W"] = "W",
            // Strikers
            ["ST"] = "ST", ["SC"] = "ST", ["FC"] = "ST",
        };

        /// <summary>Returns the normalised position group for an FM position string, or "CM" as fallback.</summary>
        public static string GetPositionGroup(string? fmPosition)
        {
            if (fmPosition == null) return "CM";
            return PositionGroupMap.TryGetValue(fmPosition.Trim(), out var g) ? g : "CM";
        }

        // ── Role definitions ───────────────────────────────────────────────────────

        /// <summary>All defined FM26 roles. Add new roles here — scoring picks them up automatically.</summary>
        public static readonly IReadOnlyList<RoleDefinition> All = new List<RoleDefinition>
        {
            // ── Goalkeepers ──────────────────────────────────────────────────────
            new("Goalkeeper",            "GK",
                (A.Reflexes, 1.0), (A.Handling, 1.0), (A.Positioning, 1.0),
                (A.AerialReach, 0.7), (A.Communication, 0.6), (A.Concentration, 0.7)),

            new("Sweeper Keeper",        "GK",
                (A.Reflexes, 0.9), (A.Handling, 0.8), (A.Passing, 0.7),
                (A.FirstTouch, 0.6), (A.Pace, 0.6), (A.Composure, 0.7), (A.Decisions, 0.7)),

            // ── Centre-backs ────────────────────────────────────────────────────
            new("Central Defender",      "CB",
                (A.Heading, 1.0), (A.Tackling, 0.9), (A.Positioning, 1.0),
                (A.Marking, 0.9), (A.Anticipation, 0.9), (A.Composure, 0.7), (A.Strength, 0.7)),

            new("Ball-Playing Defender", "CB",
                (A.Passing, 1.0), (A.Vision, 0.8), (A.Composure, 1.0),
                (A.FirstTouch, 0.8), (A.Decisions, 0.9), (A.Heading, 0.6), (A.Tackling, 0.7)),

            new("No-Nonsense CB",        "CB",
                (A.Heading, 1.0), (A.Tackling, 1.0), (A.Strength, 1.0),
                (A.JumpingReach, 0.9), (A.Marking, 0.9), (A.Positioning, 0.8), (A.Aggression, 0.6)),

            new("Stopper",               "CB",
                (A.Heading, 0.9), (A.Tackling, 1.0), (A.Strength, 0.8),
                (A.Anticipation, 0.9), (A.Positioning, 0.8), (A.Aggression, 0.7), (A.Bravery, 0.7)),

            new("Cover",                 "CB",
                (A.Positioning, 1.0), (A.Anticipation, 1.0), (A.Pace, 0.8),
                (A.Decisions, 0.8), (A.Concentration, 0.7), (A.Tackling, 0.6)),

            // ── Full-backs ───────────────────────────────────────────────────────
            new("Full Back (Defend)",    "FB",
                (A.Tackling, 1.0), (A.Marking, 1.0), (A.Positioning, 0.9),
                (A.Concentration, 0.8), (A.Anticipation, 0.7), (A.Stamina, 0.7)),

            new("Full Back (Support)",   "FB",
                (A.Crossing, 0.9), (A.Tackling, 0.8), (A.Pace, 0.8),
                (A.Stamina, 0.9), (A.WorkRate, 0.8), (A.Positioning, 0.7)),

            new("Full Back (Attack)",    "FB",
                (A.Crossing, 1.0), (A.Pace, 1.0), (A.Stamina, 0.9),
                (A.Dribbling, 0.8), (A.WorkRate, 0.8), (A.Acceleration, 0.8)),

            new("Inverted Full Back",    "FB",
                (A.Passing, 1.0), (A.Dribbling, 0.9), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.Vision, 0.7), (A.Tackling, 0.6)),

            // ── Wing-backs ───────────────────────────────────────────────────────
            new("Wing Back (Support)",   "WB",
                (A.Crossing, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.WorkRate, 0.9), (A.Tackling, 0.7), (A.Acceleration, 0.8)),

            new("Wing Back (Attack)",    "WB",
                (A.Crossing, 1.0), (A.Dribbling, 0.9), (A.Pace, 1.0),
                (A.Stamina, 1.0), (A.Acceleration, 0.9), (A.WorkRate, 0.8)),

            new("Complete Wing Back",    "WB",
                (A.Crossing, 1.0), (A.Dribbling, 1.0), (A.Pace, 1.0),
                (A.Stamina, 1.0), (A.Passing, 0.7), (A.Tackling, 0.7), (A.WorkRate, 0.9)),

            new("Inverted Wing Back",    "WB",
                (A.Passing, 1.0), (A.Dribbling, 0.9), (A.Stamina, 0.9),
                (A.Decisions, 0.9), (A.Vision, 0.8), (A.Composure, 0.7)),

            // ── Defensive midfielders ────────────────────────────────────────────
            new("Defensive Midfielder",  "DM",
                (A.Tackling, 1.0), (A.Positioning, 1.0), (A.Anticipation, 0.9),
                (A.Marking, 0.8), (A.Concentration, 0.8), (A.Stamina, 0.7)),

            new("Anchor",                "DM",
                (A.Positioning, 1.0), (A.Tackling, 0.9), (A.Marking, 0.9),
                (A.Anticipation, 0.9), (A.Concentration, 0.8), (A.Composure, 0.7)),

            new("Ball-Winning Midfielder","DM",
                (A.Tackling, 1.0), (A.WorkRate, 1.0), (A.Stamina, 0.9),
                (A.Anticipation, 0.9), (A.Aggression, 0.8), (A.Positioning, 0.7), (A.Bravery, 0.6)),

            new("Deep-Lying Playmaker (DM)","DM",
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Composure, 1.0),
                (A.Decisions, 0.9), (A.FirstTouch, 0.8), (A.Tackling, 0.5), (A.Anticipation, 0.7)),

            // ── Central midfielders ──────────────────────────────────────────────
            new("Box-to-Box Midfielder", "CM",
                (A.Stamina, 1.0), (A.WorkRate, 1.0), (A.Passing, 0.8),
                (A.Tackling, 0.8), (A.Decisions, 0.7), (A.Shooting, 0.6), (A.Determination, 0.6)),

            new("Mezzala",               "CM",
                (A.Passing, 0.9), (A.Dribbling, 0.9), (A.Vision, 1.0),
                (A.Decisions, 0.9), (A.Stamina, 0.8), (A.LongShots, 0.7), (A.Flair, 0.5)),

            new("Regista",               "CM",
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 1.0), (A.FirstTouch, 0.9), (A.LongShots, 0.7), (A.Flair, 0.6)),

            new("Carrilero",             "CM",
                (A.Passing, 0.9), (A.Stamina, 1.0), (A.WorkRate, 1.0),
                (A.Decisions, 0.8), (A.Positioning, 0.8), (A.Tackling, 0.7)),

            new("Central Midfielder (Defend)","CM",
                (A.Tackling, 0.9), (A.Positioning, 0.9), (A.Stamina, 0.8),
                (A.Decisions, 0.8), (A.Concentration, 0.8), (A.Marking, 0.7), (A.Passing, 0.6)),

            new("Deep-Lying Playmaker (CM)","CM",
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.FirstTouch, 0.9), (A.Anticipation, 0.6)),

            // ── Attacking midfielders ────────────────────────────────────────────
            new("Advanced Playmaker",    "AM",
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Decisions, 1.0),
                (A.Composure, 0.9), (A.FirstTouch, 0.9), (A.Dribbling, 0.7), (A.Flair, 0.5)),

            new("Enganche",              "AM",
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 1.0),
                (A.FirstTouch, 1.0), (A.Decisions, 0.9), (A.Dribbling, 0.8), (A.Flair, 0.7)),

            new("Trequartista",          "AM",
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.Passing, 0.7), (A.Finishing, 0.7), (A.Flair, 0.8)),

            new("Shadow Striker",        "AM",
                (A.Finishing, 1.0), (A.Composure, 0.9), (A.Anticipation, 1.0),
                (A.Decisions, 0.8), (A.Dribbling, 0.8), (A.Acceleration, 0.7)),

            // ── Wide players ─────────────────────────────────────────────────────
            new("Winger (Attack)",       "W",
                (A.Pace, 1.0), (A.Dribbling, 1.0), (A.Crossing, 0.9),
                (A.Acceleration, 1.0), (A.Agility, 0.8), (A.WorkRate, 0.7), (A.Flair, 0.6)),

            new("Inverted Winger",       "W",
                (A.Dribbling, 1.0), (A.Pace, 0.9), (A.Finishing, 0.8),
                (A.LongShots, 0.8), (A.Agility, 0.9), (A.Acceleration, 0.9), (A.Flair, 0.7)),

            new("Inside Forward",        "W",
                (A.Dribbling, 0.9), (A.Finishing, 1.0), (A.Pace, 0.9),
                (A.Composure, 0.9), (A.Agility, 0.8), (A.Acceleration, 0.9)),

            new("Raumdeuter",            "W",
                (A.Anticipation, 1.0), (A.Positioning, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.Finishing, 0.9), (A.Concentration, 0.7)),

            new("Wide Midfielder (Support)","W",
                (A.Crossing, 0.9), (A.Stamina, 1.0), (A.WorkRate, 1.0),
                (A.Passing, 0.8), (A.Tackling, 0.7), (A.Pace, 0.7)),

            // ── Strikers ─────────────────────────────────────────────────────────
            new("Advanced Forward",      "ST",
                (A.Finishing, 1.0), (A.Composure, 1.0), (A.Pace, 0.9),
                (A.Acceleration, 0.9), (A.Dribbling, 0.7), (A.Decisions, 0.8)),

            new("Poacher",               "ST",
                (A.Finishing, 1.0), (A.Anticipation, 1.0), (A.Composure, 1.0),
                (A.Positioning, 1.0), (A.Acceleration, 0.8), (A.Concentration, 0.6)),

            new("Target Man",            "ST",
                (A.Heading, 1.0), (A.Strength, 1.0), (A.JumpingReach, 1.0),
                (A.Composure, 0.7), (A.Finishing, 0.7), (A.Balance, 0.8), (A.Bravery, 0.7)),

            new("False Nine",            "ST",
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Passing, 1.0),
                (A.Composure, 0.9), (A.FirstTouch, 0.9), (A.Decisions, 0.9), (A.Flair, 0.7)),

            new("Deep-Lying Forward",    "ST",
                (A.Passing, 1.0), (A.FirstTouch, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.Dribbling, 0.7), (A.Vision, 0.7)),

            new("Pressing Forward",      "ST",
                (A.WorkRate, 1.0), (A.Pace, 1.0), (A.Stamina, 1.0),
                (A.Anticipation, 0.8), (A.Acceleration, 0.9), (A.Aggression, 0.7), (A.Determination, 0.7)),

            new("Complete Forward",      "ST",
                (A.Finishing, 1.0), (A.Composure, 0.9), (A.Heading, 0.8),
                (A.Pace, 0.8), (A.Dribbling, 0.8), (A.Passing, 0.7), (A.Decisions, 0.9)),
        };

        // ── Scoring engine ────────────────────────────────────────────────────────

        /// <summary>
        /// Scores a player's attributes against all roles, sorted descending.
        /// Attributes absent from the player's data default to 10 (league average).
        /// </summary>
        public static List<Models.RoleScore> ScoreAllRoles(PlayerAttributes? attrs)
        {
            return All
                .Select(role => new Models.RoleScore
                {
                    RoleName      = role.Name,
                    PositionGroup = role.PositionGroup,
                    Score         = ScoreRole(attrs, role)
                })
                .OrderByDescending(r => r.Score)
                .ToList();
        }

        /// <summary>Score a player against a single role definition (0–100).</summary>
        public static double ScoreRole(PlayerAttributes? attrs, RoleDefinition role)
        {
            double weighted = 0, maxWeighted = 0;

            foreach (var (attrKey, weight) in role.Weights)
            {
                var value = GetAttribute(attrs, attrKey) ?? 10; // 10 = league average fallback
                weighted    += value  * weight;
                maxWeighted += 20.0  * weight;   // 20 = FM attribute ceiling
            }

            return maxWeighted > 0 ? weighted / maxWeighted * 100.0 : 0;
        }

        // ── Attribute resolver ────────────────────────────────────────────────────

        private static int? GetAttribute(PlayerAttributes? a, string key)
        {
            if (a == null) return null;
            return key switch
            {
                // Technical
                A.Passing      => a.Passing,
                A.Shooting     => a.Shooting,
                A.Dribbling    => a.Dribbling,
                A.FirstTouch   => a.FirstTouch,
                A.Heading      => a.Heading,
                A.Tackling     => a.Tackling,
                A.Crossing     => a.Crossing,
                A.LongShots    => a.LongShots,
                A.Marking      => a.Marking,
                A.Finishing    => a.Finishing,
                // Mental
                A.Decisions    => a.Decisions,
                A.Composure    => a.Composure,
                A.Positioning  => a.Positioning,
                A.Anticipation => a.Anticipation,
                A.Vision       => a.Vision,
                A.WorkRate     => a.WorkRate,
                A.Leadership   => a.Leadership,
                A.Teamwork     => a.Teamwork,
                A.Aggression   => a.Aggression,
                A.Concentration=> a.Concentration,
                A.Bravery      => a.Bravery,
                A.Determination=> a.Determination,
                A.Flair        => a.Flair,
                // Physical
                A.Pace         => a.Pace,
                A.Strength     => a.Strength,
                A.Stamina      => a.Stamina,
                A.Agility      => a.Agility,
                A.Balance      => a.Balance,
                A.Acceleration => a.Acceleration,
                A.JumpingReach => a.JumpingReach,
                // Goalkeeper
                A.Reflexes     => a.Reflexes,
                A.Handling     => a.Handling,
                A.Communication=> a.Communication,
                A.OneOnOnes    => a.OneOnOnes,
                A.AerialReach  => a.AerialReach,
                _              => null
            };
        }
    }

    // ── Role definition type ──────────────────────────────────────────────────────

    /// <summary>An FM role with its attribute weight vector.</summary>
    internal sealed class RoleDefinition
    {
        public string Name          { get; }
        public string PositionGroup { get; }

        /// <summary>Attribute key → importance weight (0.0–1.0).</summary>
        public IReadOnlyList<(string Attr, double Weight)> Weights { get; }

        public RoleDefinition(string name, string positionGroup,
            params (string, double)[] weights)
        {
            Name          = name;
            PositionGroup = positionGroup;
            Weights       = weights;
        }
    }

    // ── Attribute key constants ───────────────────────────────────────────────────
    // Short-hand aliases used only within RoleLibrary to keep role definitions readable.

    internal static class A
    {
        // Technical
        public const string Passing      = "passing";
        public const string Shooting     = "shooting";
        public const string Dribbling    = "dribbling";
        public const string FirstTouch   = "firstTouch";
        public const string Heading      = "heading";
        public const string Tackling     = "tackling";
        public const string Crossing     = "crossing";
        public const string LongShots    = "longShots";
        public const string Marking      = "marking";
        public const string Finishing    = "finishing";
        // Mental
        public const string Decisions    = "decisions";
        public const string Composure    = "composure";
        public const string Positioning  = "positioning";
        public const string Anticipation = "anticipation";
        public const string Vision       = "vision";
        public const string WorkRate     = "workRate";
        public const string Leadership   = "leadership";
        public const string Teamwork     = "teamwork";
        public const string Aggression   = "aggression";
        public const string Concentration= "concentration";
        public const string Bravery      = "bravery";
        public const string Determination= "determination";
        public const string Flair        = "flair";
        // Physical
        public const string Pace         = "pace";
        public const string Strength     = "strength";
        public const string Stamina      = "stamina";
        public const string Agility      = "agility";
        public const string Balance      = "balance";
        public const string Acceleration = "acceleration";
        public const string JumpingReach = "jumpingReach";
        // Goalkeeper
        public const string Reflexes     = "reflexes";
        public const string Handling     = "handling";
        public const string Communication= "communication";
        public const string OneOnOnes    = "oneOnOnes";
        public const string AerialReach  = "aerialReach";
    }
}
