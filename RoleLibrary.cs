using System;
using System.Collections.Generic;
using System.Linq;
using Gaffer.Models;

namespace Gaffer
{
    /// <summary>
    /// FM26 role library. FM26 separates roles into two independent assignments per player:
    ///   - InPossession  — what the player does when their team has the ball
    ///   - OutOfPossession — what the player does when their team is defending/pressing
    ///
    /// Each player slot therefore has TWO role scores: one IP, one OP.
    /// Identity analysis checks whether each role is suited to the player's attributes
    /// and whether the optimal IP+OP combination matches the player's registered position.
    ///
    /// ROLE NAMES: These use descriptive names derived from FM26 conventions.
    /// Update the Name strings below once you have confirmed the exact FM26 UI strings
    /// (check the tactics screen role dropdowns). Attribute weights will remain valid
    /// regardless — they are based on football logic, not FM naming.
    ///
    /// POSITION GROUPS used throughout:
    ///   GK, CB, FB, WB, DM, CM, Wide, AM, ST
    ///
    /// FM26 position code → group mapping (corrected from FM24):
    ///   GK                      → GK
    ///   DCL / DC / DCR          → CB
    ///   DL / DR                 → FB
    ///   WBL / WBR               → WB
    ///   LDM / DM / RDM          → DM
    ///   LCM / MC / RCM          → CM
    ///   ML / MR                 → Wide   (wide midfielders — NOT central mid)
    ///   AMCL / AMC / AMCR       → AM
    ///   AML / AMR               → Wide   (attacking wingers — NOT AM)
    ///   ST                      → ST
    /// </summary>
    internal static class RoleLibrary
    {
        // ── Position group mapping ─────────────────────────────────────────────────

        private static readonly Dictionary<string, string> PositionGroupMap =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // Goalkeeper
            ["GK"]   = "GK",
            // Centre-backs
            ["DC"]   = "CB", ["DCL"] = "CB", ["DCR"] = "CB",
            // Full-backs
            ["DL"]   = "FB", ["DR"]  = "FB",
            // Wing-backs
            ["WBL"]  = "WB", ["WBR"] = "WB",
            // Defensive midfielders (left / central / right slots)
            ["DM"]   = "DM", ["DMC"] = "DM", ["LDM"] = "DM", ["RDM"] = "DM",
            // Central midfielders (left / central / right slots)
            ["MC"]   = "CM", ["LCM"] = "CM", ["RCM"] = "CM",
            // Wide midfielders — ML/MR are wide players, NOT central midfielders
            ["ML"]   = "Wide", ["MR"] = "Wide",
            // Advanced central midfielders
            ["AMC"]  = "AM", ["AMCL"] = "AM", ["AMCR"] = "AM",
            // Attacking wingers — AML/AMR are wingers, NOT AMs
            ["AML"]  = "Wide", ["AMR"] = "Wide",
            // Strikers
            ["ST"]   = "ST", ["SC"]  = "ST", ["FC"]  = "ST",
        };

        /// <summary>Returns the normalised position group for an FM26 position code.</summary>
        public static string GetPositionGroup(string? fmPosition)
        {
            if (fmPosition == null) return "CM";
            return PositionGroupMap.TryGetValue(fmPosition.Trim(), out var g) ? g : "CM";
        }

        // ── Role categories ────────────────────────────────────────────────────────

        public enum RoleCategory { InPossession, OutOfPossession }

        // ── In-possession roles ────────────────────────────────────────────────────
        // These describe what the player does when their team has the ball.
        // Attribute weights skew toward on-ball qualities.

        public static readonly IReadOnlyList<RoleDefinition> InPossessionRoles = new List<RoleDefinition>
        {
            // ── GK ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 name for ball-playing GK (Sweeper Keeper or similar)
            new("Ball-Playing Goalkeeper", "GK", RoleCategory.InPossession,
                (A.Handling, 0.9), (A.Passing, 1.0), (A.FirstTouch, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.8), (A.Reflexes, 0.6)),

            new("Traditional Goalkeeper", "GK", RoleCategory.InPossession,
                (A.Handling, 1.0), (A.Reflexes, 0.9), (A.AerialReach, 0.7),
                (A.Positioning, 0.8), (A.Communication, 0.7)),

            // ── CB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 name — "Ball-Playing Defender" or "Libero" or new name
            new("Ball-Playing Defender", "CB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.FirstTouch, 0.9),
                (A.Composure, 1.0), (A.Decisions, 0.9), (A.Dribbling, 0.5)),

            // TODO: confirm FM26 name — defender who simply receives and distributes
            new("Defender (Possession)", "CB", RoleCategory.InPossession,
                (A.Passing, 0.8), (A.Composure, 0.9), (A.FirstTouch, 0.7),
                (A.Decisions, 0.8), (A.Positioning, 0.7)),

            // TODO: confirm FM26 name — CB who carries forward (Libero style)
            new("Libero", "CB", RoleCategory.InPossession,
                (A.Dribbling, 0.9), (A.Passing, 1.0), (A.Vision, 0.9),
                (A.Composure, 1.0), (A.Decisions, 0.9), (A.Pace, 0.6)),

            // ── FB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 name — overlapping, crossing-focused
            new("Attacking Full Back", "FB", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Pace, 1.0), (A.Dribbling, 0.8),
                (A.Stamina, 0.9), (A.Acceleration, 0.9), (A.WorkRate, 0.8)),

            // TODO: confirm FM26 name — underlapping, combination play
            new("Inverted Full Back", "FB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Dribbling, 0.9), (A.Vision, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.FirstTouch, 0.8)),

            new("Supporting Full Back", "FB", RoleCategory.InPossession,
                (A.Passing, 0.8), (A.Crossing, 0.7), (A.Stamina, 0.8),
                (A.Decisions, 0.7), (A.FirstTouch, 0.7)),

            // ── WB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 names for wing-back in-possession variants
            new("Crossing Wing Back", "WB", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Pace, 1.0), (A.Stamina, 1.0),
                (A.Acceleration, 0.9), (A.Dribbling, 0.7), (A.WorkRate, 0.8)),

            new("Carrying Wing Back", "WB", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Pace, 1.0), (A.Stamina, 0.9),
                (A.Passing, 0.8), (A.Vision, 0.7), (A.Acceleration, 0.9)),

            new("Inverted Wing Back", "WB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Dribbling, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.FirstTouch, 0.8)),

            // ── DM ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 name — deep playmaker / Regista equivalent
            new("Regista", "DM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 1.0), (A.FirstTouch, 0.9), (A.LongShots, 0.7), (A.Flair, 0.6)),

            // TODO: confirm FM26 name — DM who distributes but doesn't roam
            new("Deep Distributor", "DM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Composure, 0.9), (A.Decisions, 0.9),
                (A.FirstTouch, 0.8), (A.Vision, 0.7), (A.Positioning, 0.7)),

            // ── CM ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 name — half-space runner / Mezzala equivalent
            new("Mezzala", "CM", RoleCategory.InPossession,
                (A.Passing, 0.9), (A.Dribbling, 1.0), (A.Vision, 1.0),
                (A.Decisions, 0.9), (A.Stamina, 0.8), (A.LongShots, 0.7), (A.Flair, 0.6)),

            // TODO: confirm FM26 name — central playmaker
            new("Central Playmaker", "CM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.FirstTouch, 0.9), (A.Teamwork, 0.6)),

            // TODO: confirm FM26 name — carries and drives forward
            new("Ball Carrier", "CM", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Pace, 0.8), (A.Stamina, 0.9),
                (A.Decisions, 0.8), (A.Composure, 0.8), (A.Acceleration, 0.8)),

            // TODO: confirm FM26 name — link, combination, Carrilero equivalent
            new("Link Midfielder", "CM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Stamina, 0.9), (A.WorkRate, 0.9),
                (A.Decisions, 0.8), (A.FirstTouch, 0.8), (A.Positioning, 0.7)),

            // ── Wide (ML / MR / AML / AMR) ───────────────────────────────────────
            // TODO: confirm FM26 names — wide roles covering both mid-line and AM-line wingers
            new("Wide Creator", "Wide", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Passing, 0.9), (A.Vision, 0.9),
                (A.Dribbling, 0.7), (A.Stamina, 0.8), (A.WorkRate, 0.7)),

            new("Wide Carrier", "Wide", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Pace, 1.0), (A.Acceleration, 1.0),
                (A.Agility, 0.9), (A.Stamina, 0.8), (A.Flair, 0.7)),

            new("Inverted Wide", "Wide", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Finishing, 0.9), (A.LongShots, 0.9),
                (A.Pace, 0.9), (A.Agility, 0.9), (A.Composure, 0.8)),

            new("Inside Forward", "Wide", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Dribbling, 0.9), (A.Composure, 1.0),
                (A.Agility, 0.8), (A.Anticipation, 0.8), (A.Acceleration, 0.8)),

            // ── AM (AMCL / AMC / AMCR) ──────────────────────────────────────────
            // TODO: confirm FM26 names — central attacking mid roles
            new("Enganche", "AM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 1.0),
                (A.FirstTouch, 1.0), (A.Decisions, 0.9), (A.Dribbling, 0.8), (A.Flair, 0.7)),

            new("Trequartista", "AM", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Passing, 0.8), (A.Finishing, 0.7), (A.Flair, 0.9), (A.Decisions, 0.8)),

            new("Advanced Playmaker", "AM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Decisions, 1.0),
                (A.Composure, 0.9), (A.FirstTouch, 0.9), (A.Dribbling, 0.7)),

            // ── ST ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 names for striker in-possession roles
            new("False Nine", "ST", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Passing, 1.0),
                (A.Composure, 0.9), (A.FirstTouch, 0.9), (A.Decisions, 0.9), (A.Flair, 0.7)),

            new("Deep Striker", "ST", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.FirstTouch, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.Dribbling, 0.8), (A.Vision, 0.7)),

            new("Finisher", "ST", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Composure, 1.0), (A.Anticipation, 0.9),
                (A.Positioning, 0.9), (A.Decisions, 0.8), (A.FirstTouch, 0.7)),

            new("Target Man", "ST", RoleCategory.InPossession,
                (A.Heading, 1.0), (A.Strength, 1.0), (A.JumpingReach, 1.0),
                (A.Balance, 0.8), (A.Bravery, 0.8), (A.Composure, 0.7), (A.Finishing, 0.6)),

            new("Complete Forward", "ST", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Dribbling, 0.8), (A.Passing, 0.7),
                (A.Composure, 0.9), (A.Heading, 0.7), (A.Pace, 0.8), (A.Decisions, 0.9)),
        };

        // ── Out-of-possession roles ────────────────────────────────────────────────
        // These describe what the player does when their team is defending/pressing.
        // Attribute weights skew toward off-ball and defensive qualities.

        public static readonly IReadOnlyList<RoleDefinition> OutOfPossessionRoles = new List<RoleDefinition>
        {
            // ── GK ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP GK role names
            new("Shot Stopper", "GK", RoleCategory.OutOfPossession,
                (A.Reflexes, 1.0), (A.Handling, 0.9), (A.Positioning, 1.0),
                (A.Concentration, 0.8), (A.AerialReach, 0.7), (A.Decisions, 0.7)),

            new("Sweeper Keeper", "GK", RoleCategory.OutOfPossession,
                (A.Reflexes, 0.9), (A.Positioning, 1.0), (A.Anticipation, 1.0),
                (A.Pace, 0.8), (A.Decisions, 0.9), (A.Composure, 0.7), (A.Handling, 0.8)),

            // ── CB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP CB role names
            new("Stopper", "CB", RoleCategory.OutOfPossession,
                (A.Heading, 1.0), (A.Tackling, 1.0), (A.Strength, 0.9),
                (A.Aggression, 0.8), (A.Bravery, 0.8), (A.JumpingReach, 0.9), (A.Anticipation, 0.7)),

            new("Cover", "CB", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Anticipation, 1.0), (A.Concentration, 0.9),
                (A.Pace, 0.8), (A.Decisions, 0.9), (A.Tackling, 0.6)),

            new("Marker", "CB", RoleCategory.OutOfPossession,
                (A.Marking, 1.0), (A.Tackling, 0.9), (A.Positioning, 0.9),
                (A.Concentration, 0.9), (A.Anticipation, 0.8), (A.Strength, 0.7)),

            // ── FB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP FB role names
            new("Defensive Full Back", "FB", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.Marking, 1.0), (A.Positioning, 0.9),
                (A.Concentration, 0.9), (A.Anticipation, 0.8), (A.Stamina, 0.7)),

            new("Pressing Full Back", "FB", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.Tackling, 0.8), (A.Anticipation, 0.8), (A.Acceleration, 0.8)),

            // ── WB ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP WB role names
            new("Defensive Wing Back", "WB", RoleCategory.OutOfPossession,
                (A.Tackling, 0.9), (A.Marking, 0.9), (A.Positioning, 0.9),
                (A.Stamina, 0.9), (A.WorkRate, 0.9), (A.Concentration, 0.8)),

            new("Pressing Wing Back", "WB", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.Acceleration, 0.9), (A.Tackling, 0.7), (A.Anticipation, 0.8)),

            // ── DM ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP DM role names
            new("Ball Winner", "DM", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.WorkRate, 1.0), (A.Stamina, 0.9),
                (A.Anticipation, 0.9), (A.Aggression, 0.8), (A.Positioning, 0.7), (A.Bravery, 0.7)),

            new("Screener", "DM", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Anticipation, 1.0), (A.Concentration, 0.9),
                (A.Marking, 0.8), (A.Tackling, 0.8), (A.Decisions, 0.8)),

            new("Pressing DM", "DM", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Anticipation, 0.9),
                (A.Pace, 0.7), (A.Tackling, 0.8), (A.Aggression, 0.7)),

            // ── CM ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP CM role names
            new("Pressing Midfielder", "CM", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Anticipation, 0.9),
                (A.Aggression, 0.7), (A.Pace, 0.7), (A.Determination, 0.8)),

            new("Holding Midfielder", "CM", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Tackling, 0.9), (A.Concentration, 0.9),
                (A.Marking, 0.8), (A.Decisions, 0.8), (A.Anticipation, 0.9)),

            new("Box-to-Box (OP)", "CM", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.WorkRate, 1.0), (A.Tackling, 0.8),
                (A.Determination, 0.7), (A.Anticipation, 0.7), (A.Positioning, 0.6)),

            // ── Wide (ML / MR / AML / AMR) ───────────────────────────────────────
            // TODO: confirm FM26 OP wide role names
            new("Wide Presser", "Wide", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.Anticipation, 0.8), (A.Aggression, 0.7), (A.Acceleration, 0.8)),

            new("Wide Tracker", "Wide", RoleCategory.OutOfPossession,
                (A.Tackling, 0.9), (A.WorkRate, 0.9), (A.Stamina, 0.9),
                (A.Positioning, 0.8), (A.Concentration, 0.8), (A.Marking, 0.7)),

            new("Wide Blocker", "Wide", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Concentration, 0.9), (A.Decisions, 0.9),
                (A.Anticipation, 0.8), (A.Tackling, 0.7), (A.Teamwork, 0.8)),

            // ── AM (AMCL / AMC / AMCR) ──────────────────────────────────────────
            // TODO: confirm FM26 OP AM role names
            new("High Presser", "AM", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Anticipation, 0.9),
                (A.Pace, 0.8), (A.Aggression, 0.7), (A.Determination, 0.8)),

            new("Shadow Blocker", "AM", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Anticipation, 0.9), (A.Concentration, 0.9),
                (A.Decisions, 0.8), (A.Teamwork, 0.8), (A.WorkRate, 0.7)),

            // ── ST ──────────────────────────────────────────────────────────────
            // TODO: confirm FM26 OP striker role names
            new("Pressing Forward", "ST", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Pace, 1.0), (A.Stamina, 1.0),
                (A.Anticipation, 0.9), (A.Acceleration, 0.9), (A.Aggression, 0.7), (A.Determination, 0.8)),

            new("High Line Holder", "ST", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Concentration, 0.9), (A.Teamwork, 0.9),
                (A.Anticipation, 0.8), (A.Decisions, 0.8), (A.WorkRate, 0.7)),

            new("Shadow Striker (OP)", "ST", RoleCategory.OutOfPossession,
                (A.Anticipation, 1.0), (A.Positioning, 1.0), (A.Concentration, 0.9),
                (A.Decisions, 0.8), (A.WorkRate, 0.7), (A.Pace, 0.6)),
        };

        // ── Convenience accessors ─────────────────────────────────────────────────

        /// <summary>All IP roles for a specific position group.</summary>
        public static IEnumerable<RoleDefinition> InPossessionForGroup(string group) =>
            InPossessionRoles.Where(r => r.PositionGroup == group);

        /// <summary>All OP roles for a specific position group.</summary>
        public static IEnumerable<RoleDefinition> OutOfPossessionForGroup(string group) =>
            OutOfPossessionRoles.Where(r => r.PositionGroup == group);

        // ── Scoring engine ────────────────────────────────────────────────────────

        /// <summary>Scores player attributes against all IP roles, sorted descending.</summary>
        public static List<RoleScore> ScoreAllInPossession(PlayerAttributes? attrs) =>
            Score(attrs, InPossessionRoles);

        /// <summary>Scores player attributes against all OP roles, sorted descending.</summary>
        public static List<RoleScore> ScoreAllOutOfPossession(PlayerAttributes? attrs) =>
            Score(attrs, OutOfPossessionRoles);

        /// <summary>Scores against a filtered subset, sorted descending.</summary>
        public static List<RoleScore> ScoreForGroup(PlayerAttributes? attrs, string group, RoleCategory category)
        {
            var pool = category == RoleCategory.InPossession
                ? InPossessionForGroup(group)
                : OutOfPossessionForGroup(group);
            return Score(attrs, pool);
        }

        private static List<RoleScore> Score(PlayerAttributes? attrs, IEnumerable<RoleDefinition> roles) =>
            roles
                .Select(r => new RoleScore
                {
                    RoleName      = r.Name,
                    PositionGroup = r.PositionGroup,
                    Category      = r.Category,
                    Score         = ScoreRole(attrs, r)
                })
                .OrderByDescending(r => r.Score)
                .ToList();

        /// <summary>Score a player against a single role definition (0–100).</summary>
        public static double ScoreRole(PlayerAttributes? attrs, RoleDefinition role)
        {
            double weighted = 0, maxWeighted = 0;
            foreach (var (attrKey, weight) in role.Weights)
            {
                var value    = GetAttribute(attrs, attrKey) ?? 10; // 10 = league average
                weighted    += value * weight;
                maxWeighted += 20.0  * weight;
            }
            return maxWeighted > 0 ? weighted / maxWeighted * 100.0 : 0;
        }

        // ── Attribute resolver ────────────────────────────────────────────────────

        private static int? GetAttribute(PlayerAttributes? a, string key) => a == null ? null : key switch
        {
            // Technical
            A.Passing       => a.Passing,
            A.Shooting      => a.Shooting,
            A.Dribbling     => a.Dribbling,
            A.FirstTouch    => a.FirstTouch,
            A.Heading       => a.Heading,
            A.Tackling      => a.Tackling,
            A.Crossing      => a.Crossing,
            A.LongShots     => a.LongShots,
            A.Marking       => a.Marking,
            A.Finishing     => a.Finishing,
            // Mental
            A.Decisions     => a.Decisions,
            A.Composure     => a.Composure,
            A.Positioning   => a.Positioning,
            A.Anticipation  => a.Anticipation,
            A.Vision        => a.Vision,
            A.WorkRate      => a.WorkRate,
            A.Leadership    => a.Leadership,
            A.Teamwork      => a.Teamwork,
            A.Aggression    => a.Aggression,
            A.Concentration => a.Concentration,
            A.Bravery       => a.Bravery,
            A.Determination => a.Determination,
            A.Flair         => a.Flair,
            // Physical
            A.Pace          => a.Pace,
            A.Strength      => a.Strength,
            A.Stamina       => a.Stamina,
            A.Agility       => a.Agility,
            A.Balance       => a.Balance,
            A.Acceleration  => a.Acceleration,
            A.JumpingReach  => a.JumpingReach,
            // GK
            A.Reflexes      => a.Reflexes,
            A.Handling      => a.Handling,
            A.Communication => a.Communication,
            A.OneOnOnes     => a.OneOnOnes,
            A.AerialReach   => a.AerialReach,
            _               => null
        };
    }

    // ── Role definition type ──────────────────────────────────────────────────────

    internal sealed class RoleDefinition
    {
        public string                                     Name          { get; }
        public string                                     PositionGroup { get; }
        public RoleLibrary.RoleCategory                   Category      { get; }
        public IReadOnlyList<(string Attr, double Weight)> Weights      { get; }

        public RoleDefinition(string name, string positionGroup,
            RoleLibrary.RoleCategory category, params (string, double)[] weights)
        {
            Name          = name;
            PositionGroup = positionGroup;
            Category      = category;
            Weights       = weights;
        }
    }

    // ── Attribute key aliases (RoleLibrary-internal only) ─────────────────────────

    internal static class A
    {
        public const string Passing       = "passing";
        public const string Shooting      = "shooting";
        public const string Dribbling     = "dribbling";
        public const string FirstTouch    = "firstTouch";
        public const string Heading       = "heading";
        public const string Tackling      = "tackling";
        public const string Crossing      = "crossing";
        public const string LongShots     = "longShots";
        public const string Marking       = "marking";
        public const string Finishing     = "finishing";
        public const string Decisions     = "decisions";
        public const string Composure     = "composure";
        public const string Positioning   = "positioning";
        public const string Anticipation  = "anticipation";
        public const string Vision        = "vision";
        public const string WorkRate      = "workRate";
        public const string Leadership    = "leadership";
        public const string Teamwork      = "teamwork";
        public const string Aggression    = "aggression";
        public const string Concentration = "concentration";
        public const string Bravery       = "bravery";
        public const string Determination = "determination";
        public const string Flair         = "flair";
        public const string Pace          = "pace";
        public const string Strength      = "strength";
        public const string Stamina       = "stamina";
        public const string Agility       = "agility";
        public const string Balance       = "balance";
        public const string Acceleration  = "acceleration";
        public const string JumpingReach  = "jumpingReach";
        public const string Reflexes      = "reflexes";
        public const string Handling      = "handling";
        public const string Communication = "communication";
        public const string OneOnOnes     = "oneOnOnes";
        public const string AerialReach   = "aerialReach";
    }
}
