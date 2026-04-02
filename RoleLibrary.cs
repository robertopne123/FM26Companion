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
    /// ROLE NAMES: Verified FM26 role names as they appear in the tactics screen.
    /// Removed FM24-only roles: Mezzala, Enganche, Trequartista.
    /// Box-to-Box Midfielder is an OOP-only role; Channel Midfielder is the IP equivalent.
    /// FM26 uses no duty system — each player has one IP role and one OP role independently.
    ///
    /// POSITION GROUPS used throughout:
    ///   GK, CB, FB, WB, DM, CM, WideMid, Winger, AM, ST
    ///
    /// FM26 position code → group mapping:
    ///   GK                      → GK
    ///   DCL / DC / DCR          → CB
    ///   DL / DR                 → FB
    ///   WBL / WBR               → WB
    ///   LDM / DM / RDM          → DM
    ///   LCM / MC / RCM          → CM
    ///   ML / MR                 → WideMid  (wide midfielders on the mid-line)
    ///   AMCL / AMC / AMCR       → AM
    ///   AML / AMR               → Winger   (attacking wingers on the AM-line — NOT ML/MR)
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
            // Wide midfielders — ML/MR sit on the mid-line, distinct from wingers
            ["ML"]   = "WideMid", ["MR"] = "WideMid",
            // Advanced central midfielders
            ["AMC"]  = "AM", ["AMCL"] = "AM", ["AMCR"] = "AM",
            // Attacking wingers — AML/AMR on the AM-line, entirely different role set from ML/MR
            ["AML"]  = "Winger", ["AMR"] = "Winger",
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
        // Verified FM26 role names. Duties removed — each player has one IP role independently.

        public static readonly IReadOnlyList<RoleDefinition> InPossessionRoles = new List<RoleDefinition>
        {
            // ── GK ──────────────────────────────────────────────────────────────
            new("Goalkeeper", "GK", RoleCategory.InPossession,
                (A.Handling, 1.0), (A.Reflexes, 0.9), (A.Positioning, 0.8),
                (A.Concentration, 0.8), (A.Decisions, 0.7), (A.Communication, 0.6)),

            new("Line-Holding Keeper", "GK", RoleCategory.InPossession,
                (A.Reflexes, 1.0), (A.Handling, 0.9), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Communication, 0.7)),

            new("Sweeper Keeper", "GK", RoleCategory.InPossession,
                (A.Reflexes, 0.9), (A.Positioning, 1.0), (A.Anticipation, 1.0),
                (A.Pace, 0.8), (A.Decisions, 0.9), (A.Composure, 0.8), (A.Handling, 0.7)),

            new("Ball-Playing Goalkeeper", "GK", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Handling, 0.9), (A.Composure, 1.0),
                (A.FirstTouch, 0.9), (A.Decisions, 0.9), (A.Reflexes, 0.6)),

            new("No-Nonsense Goalkeeper", "GK", RoleCategory.InPossession,
                (A.Handling, 1.0), (A.Reflexes, 0.9), (A.AerialReach, 0.8),
                (A.Communication, 0.8), (A.Concentration, 0.9)),

            // ── CB ──────────────────────────────────────────────────────────────
            new("Centre-Back", "CB", RoleCategory.InPossession,
                (A.Heading, 0.9), (A.Tackling, 1.0), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Strength, 0.8), (A.Decisions, 0.7)),

            new("Ball-Playing Centre-Back", "CB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.FirstTouch, 0.9),
                (A.Composure, 1.0), (A.Decisions, 0.9), (A.Dribbling, 0.6)),

            new("Overlapping Centre-Back", "CB", RoleCategory.InPossession,
                (A.Pace, 0.9), (A.Stamina, 0.9), (A.Crossing, 0.8),
                (A.Dribbling, 0.7), (A.Passing, 0.7), (A.Acceleration, 0.8)),

            new("Advanced Centre-Back", "CB", RoleCategory.InPossession,
                (A.Dribbling, 0.9), (A.Passing, 0.9), (A.Composure, 1.0),
                (A.Decisions, 0.9), (A.Vision, 0.8), (A.Pace, 0.7)),

            new("Wide Centre-Back", "CB", RoleCategory.InPossession,
                (A.Pace, 0.8), (A.Stamina, 0.8), (A.Crossing, 0.7),
                (A.Tackling, 0.9), (A.Marking, 0.9), (A.Positioning, 0.8)),

            // ── FB ──────────────────────────────────────────────────────────────
            new("Full-Back", "FB", RoleCategory.InPossession,
                (A.Marking, 0.8), (A.Tackling, 0.8), (A.Positioning, 0.9),
                (A.Crossing, 0.7), (A.Stamina, 0.8), (A.Decisions, 0.7)),

            new("Holding Full-Back", "FB", RoleCategory.InPossession,
                (A.Tackling, 1.0), (A.Marking, 1.0), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Decisions, 0.8), (A.Composure, 0.6)),

            new("Inside Full-Back", "FB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Decisions, 0.9),
                (A.Composure, 0.9), (A.FirstTouch, 0.8), (A.Dribbling, 0.6)),

            new("Inverted Full-Back", "FB", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Passing, 1.0), (A.Vision, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.LongShots, 0.6)),

            // ── WB ──────────────────────────────────────────────────────────────
            new("Wing-Back", "WB", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Pace, 1.0), (A.Stamina, 1.0),
                (A.Acceleration, 0.9), (A.Dribbling, 0.7), (A.WorkRate, 0.8)),

            new("Holding Wing-Back", "WB", RoleCategory.InPossession,
                (A.Tackling, 0.9), (A.Marking, 0.9), (A.Positioning, 1.0),
                (A.Stamina, 0.8), (A.Concentration, 0.9), (A.WorkRate, 0.7)),

            new("Inside Wing-Back", "WB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Dribbling, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.FirstTouch, 0.8)),

            new("Inverted Wing-Back", "WB", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Passing, 0.9), (A.Vision, 0.8),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.LongShots, 0.7)),

            new("Playmaking Wing-Back", "WB", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Crossing, 0.8),
                (A.Decisions, 0.9), (A.Composure, 0.9), (A.FirstTouch, 0.8)),

            new("Advanced Wing-Back", "WB", RoleCategory.InPossession,
                (A.Pace, 1.0), (A.Acceleration, 1.0), (A.Crossing, 0.9),
                (A.Dribbling, 0.8), (A.Stamina, 1.0), (A.WorkRate, 0.8)),

            new("Pressing Wing-Back", "WB", RoleCategory.InPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.Anticipation, 0.8), (A.Acceleration, 0.8), (A.Aggression, 0.7)),

            // ── DM ──────────────────────────────────────────────────────────────
            new("Defensive Midfielder", "DM", RoleCategory.InPossession,
                (A.Tackling, 1.0), (A.Positioning, 1.0), (A.Marking, 0.9),
                (A.Decisions, 0.8), (A.Concentration, 0.9), (A.Passing, 0.6)),

            new("Wide Covering DM", "DM", RoleCategory.InPossession,
                (A.Stamina, 1.0), (A.Pace, 0.8), (A.Tackling, 0.9),
                (A.Positioning, 0.9), (A.WorkRate, 1.0), (A.Concentration, 0.8)),

            new("Wide Outlet Midfielder", "DM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 0.9), (A.Decisions, 0.9),
                (A.FirstTouch, 0.8), (A.Composure, 0.8), (A.Positioning, 0.6)),

            // ── CM ──────────────────────────────────────────────────────────────
            new("Central Midfielder", "CM", RoleCategory.InPossession,
                (A.Passing, 0.9), (A.Decisions, 0.9), (A.FirstTouch, 0.8),
                (A.Stamina, 0.8), (A.WorkRate, 0.8), (A.Positioning, 0.7)),

            // Channel Midfielder — the IP counterpart to the OOP Box-to-Box Midfielder
            new("Channel Midfielder", "CM", RoleCategory.InPossession,
                (A.Stamina, 1.0), (A.Pace, 0.8), (A.Decisions, 0.9),
                (A.Dribbling, 0.8), (A.Anticipation, 0.9), (A.WorkRate, 1.0)),

            new("Midfield Playmaker", "CM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.FirstTouch, 0.9), (A.Flair, 0.6)),

            new("Deep-Lying Playmaker", "CM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 1.0),
                (A.Decisions, 0.9), (A.FirstTouch, 0.9), (A.Positioning, 0.7)),

            new("Wide Central Midfielder", "CM", RoleCategory.InPossession,
                (A.Crossing, 0.8), (A.Stamina, 1.0), (A.Pace, 0.7),
                (A.Passing, 0.8), (A.WorkRate, 1.0), (A.Decisions, 0.7)),

            new("Box-to-Box Playmaker", "CM", RoleCategory.InPossession,
                (A.Passing, 0.9), (A.Stamina, 1.0), (A.WorkRate, 1.0),
                (A.Decisions, 0.8), (A.Vision, 0.8), (A.Dribbling, 0.7)),

            // ── WideMid (ML / MR) ────────────────────────────────────────────────
            // Wide midfielders on the mid-line — more industrious, less attacking than wingers
            new("Wide Midfielder", "WideMid", RoleCategory.InPossession,
                (A.Crossing, 0.9), (A.Stamina, 1.0), (A.WorkRate, 1.0),
                (A.Passing, 0.8), (A.Decisions, 0.8), (A.FirstTouch, 0.7)),

            new("Tracking Wide Midfielder", "WideMid", RoleCategory.InPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Tackling, 0.8),
                (A.Pace, 0.7), (A.Crossing, 0.7), (A.Concentration, 0.8)),

            new("Wide Outlet Wide Midfielder", "WideMid", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Crossing, 0.9), (A.Vision, 0.8),
                (A.Decisions, 0.9), (A.FirstTouch, 0.8), (A.WorkRate, 0.7)),

            // ── Winger (AML / AMR) ───────────────────────────────────────────────
            // Attacking wingers on the AM-line — pace, dribbling, direct threat
            new("Winger", "Winger", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Pace, 1.0), (A.Dribbling, 0.9),
                (A.Acceleration, 1.0), (A.Stamina, 0.8), (A.Agility, 0.8)),

            new("Inside Winger", "Winger", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Finishing, 0.9), (A.LongShots, 0.9),
                (A.Pace, 0.9), (A.Agility, 0.9), (A.Composure, 0.8)),

            new("Half-Space Winger", "Winger", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Passing, 0.9), (A.Vision, 0.8),
                (A.Decisions, 0.9), (A.Pace, 0.8), (A.Flair, 0.7)),

            new("Wide Outlet Winger", "Winger", RoleCategory.InPossession,
                (A.Crossing, 1.0), (A.Pace, 0.9), (A.FirstTouch, 0.8),
                (A.Decisions, 0.8), (A.Passing, 0.8), (A.WorkRate, 0.7)),

            new("Wide Playmaker", "Winger", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Crossing, 0.8),
                (A.Decisions, 0.9), (A.Composure, 0.9), (A.Flair, 0.7)),

            new("Wide Forward", "Winger", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Pace, 1.0), (A.Dribbling, 0.9),
                (A.Composure, 0.9), (A.Acceleration, 0.9), (A.Anticipation, 0.8)),

            // ── AM (AMCL / AMC / AMCR) ──────────────────────────────────────────
            new("Attacking Midfielder", "AM", RoleCategory.InPossession,
                (A.Passing, 1.0), (A.Vision, 1.0), (A.Composure, 0.9),
                (A.Decisions, 0.9), (A.FirstTouch, 0.9), (A.Dribbling, 0.8)),

            new("Free Role", "AM", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Passing, 0.9),
                (A.Composure, 0.9), (A.Decisions, 0.9), (A.Flair, 0.8), (A.Anticipation, 0.7)),

            // ── ST ──────────────────────────────────────────────────────────────
            new("Centre-Forward", "ST", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Composure, 1.0), (A.Anticipation, 0.9),
                (A.Positioning, 0.9), (A.Decisions, 0.8), (A.FirstTouch, 0.7)),

            new("Target Man", "ST", RoleCategory.InPossession,
                (A.Heading, 1.0), (A.Strength, 1.0), (A.JumpingReach, 1.0),
                (A.Balance, 0.8), (A.Bravery, 0.8), (A.Composure, 0.7), (A.Finishing, 0.6)),

            new("Poacher", "ST", RoleCategory.InPossession,
                (A.Finishing, 1.0), (A.Anticipation, 1.0), (A.Positioning, 1.0),
                (A.Composure, 0.9), (A.Decisions, 0.7), (A.Acceleration, 0.7)),

            new("Inside Forward", "ST", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Finishing, 0.9), (A.Composure, 0.9),
                (A.Agility, 0.8), (A.Pace, 0.8), (A.LongShots, 0.7)),

            new("False Nine", "ST", RoleCategory.InPossession,
                (A.Dribbling, 1.0), (A.Vision, 1.0), (A.Passing, 1.0),
                (A.Composure, 0.9), (A.FirstTouch, 0.9), (A.Decisions, 0.9), (A.Flair, 0.6)),

            new("Half-Space Forward", "ST", RoleCategory.InPossession,
                (A.Dribbling, 0.9), (A.Finishing, 0.9), (A.Decisions, 0.9),
                (A.Pace, 0.8), (A.Agility, 0.8), (A.Anticipation, 0.8)),

            new("Second Striker", "ST", RoleCategory.InPossession,
                (A.Passing, 0.9), (A.FirstTouch, 1.0), (A.Vision, 0.8),
                (A.Decisions, 0.9), (A.Composure, 0.9), (A.Dribbling, 0.7)),

            new("Channel Forward", "ST", RoleCategory.InPossession,
                (A.Pace, 1.0), (A.Acceleration, 1.0), (A.Finishing, 0.9),
                (A.Stamina, 0.8), (A.Dribbling, 0.7), (A.Anticipation, 0.8)),
        };

        // ── Out-of-possession roles ────────────────────────────────────────────────
        // Verified FM26 OOP role names. Attribute weights skew toward off-ball and defensive qualities.
        // Note: Box-to-Box Midfielder exists only here (OOP); Channel Midfielder is the IP equivalent.

        public static readonly IReadOnlyList<RoleDefinition> OutOfPossessionRoles = new List<RoleDefinition>
        {
            // ── GK ──────────────────────────────────────────────────────────────
            new("Sweeper Keeper", "GK", RoleCategory.OutOfPossession,
                (A.Reflexes, 0.9), (A.Positioning, 1.0), (A.Anticipation, 1.0),
                (A.Pace, 0.8), (A.Decisions, 0.9), (A.Handling, 0.8), (A.Composure, 0.7)),

            new("Line-Holding Keeper", "GK", RoleCategory.OutOfPossession,
                (A.Reflexes, 1.0), (A.Handling, 0.9), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Communication, 0.8)),

            // ── CB ──────────────────────────────────────────────────────────────
            new("Centre-Back", "CB", RoleCategory.OutOfPossession,
                (A.Heading, 1.0), (A.Tackling, 1.0), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Marking, 0.8), (A.Strength, 0.8)),

            new("Stopping Centre-Back", "CB", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.Heading, 1.0), (A.Strength, 0.9),
                (A.Aggression, 0.8), (A.Bravery, 0.8), (A.JumpingReach, 0.9), (A.Anticipation, 0.7)),

            new("Wide Centre-Back", "CB", RoleCategory.OutOfPossession,
                (A.Pace, 0.9), (A.Marking, 1.0), (A.Tackling, 0.9),
                (A.Positioning, 0.9), (A.Stamina, 0.8), (A.Concentration, 0.8)),

            // ── FB ──────────────────────────────────────────────────────────────
            new("Full-Back", "FB", RoleCategory.OutOfPossession,
                (A.Tackling, 0.9), (A.Marking, 0.9), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Stamina, 0.7), (A.Decisions, 0.7)),

            new("Holding Full-Back", "FB", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.Marking, 1.0), (A.Positioning, 1.0),
                (A.Concentration, 1.0), (A.Decisions, 0.8), (A.Anticipation, 0.7)),

            // ── WB ──────────────────────────────────────────────────────────────
            new("Wing-Back", "WB", RoleCategory.OutOfPossession,
                (A.Tackling, 0.8), (A.Marking, 0.8), (A.Stamina, 1.0),
                (A.WorkRate, 0.9), (A.Positioning, 0.8), (A.Concentration, 0.7)),

            new("Holding Wing-Back", "WB", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.Marking, 1.0), (A.Positioning, 1.0),
                (A.Concentration, 0.9), (A.Stamina, 0.8), (A.Decisions, 0.7)),

            new("Pressing Wing-Back", "WB", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Pace, 0.9),
                (A.Acceleration, 0.9), (A.Anticipation, 0.8), (A.Aggression, 0.7)),

            new("Inverted Wing-Back", "WB", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Tackling, 0.9), (A.Marking, 0.8),
                (A.Concentration, 0.9), (A.Decisions, 0.8), (A.Stamina, 0.7)),

            // ── DM ──────────────────────────────────────────────────────────────
            new("Box-to-Box Midfielder", "DM", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.WorkRate, 1.0), (A.Tackling, 0.8),
                (A.Determination, 0.8), (A.Anticipation, 0.8), (A.Positioning, 0.7)),

            new("Ball-Winning Midfielder", "DM", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.WorkRate, 1.0), (A.Stamina, 0.9),
                (A.Aggression, 0.8), (A.Anticipation, 0.9), (A.Positioning, 0.7), (A.Bravery, 0.7)),

            new("Wide Covering Midfielder", "DM", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.Pace, 0.8), (A.Tackling, 0.9),
                (A.WorkRate, 1.0), (A.Positioning, 0.8), (A.Concentration, 0.8)),

            // ── CM ──────────────────────────────────────────────────────────────
            new("Box-to-Box Midfielder", "CM", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.WorkRate, 1.0), (A.Tackling, 0.8),
                (A.Determination, 0.8), (A.Anticipation, 0.8), (A.Positioning, 0.7)),

            new("Ball-Winning Midfielder", "CM", RoleCategory.OutOfPossession,
                (A.Tackling, 1.0), (A.WorkRate, 1.0), (A.Stamina, 0.9),
                (A.Aggression, 0.8), (A.Anticipation, 0.9), (A.Positioning, 0.7), (A.Bravery, 0.7)),

            new("Wide Covering Midfielder", "CM", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.Pace, 0.8), (A.Tackling, 0.9),
                (A.WorkRate, 1.0), (A.Positioning, 0.8), (A.Concentration, 0.8)),

            // ── WideMid (ML / MR) ────────────────────────────────────────────────
            new("Tracking Wide Midfielder", "WideMid", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Tackling, 0.9),
                (A.Positioning, 0.9), (A.Concentration, 0.9), (A.Marking, 0.7)),

            new("Wide Outlet Wide Midfielder", "WideMid", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Decisions, 0.9), (A.WorkRate, 0.9),
                (A.Concentration, 0.8), (A.Stamina, 0.8), (A.Anticipation, 0.8)),

            new("Wide Covering Midfielder", "WideMid", RoleCategory.OutOfPossession,
                (A.Stamina, 1.0), (A.Pace, 0.8), (A.Tackling, 1.0),
                (A.WorkRate, 1.0), (A.Positioning, 0.8), (A.Marking, 0.7)),

            // ── Winger (AML / AMR) ───────────────────────────────────────────────
            new("Tracking Winger", "Winger", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Tackling, 0.8),
                (A.Pace, 0.8), (A.Positioning, 0.8), (A.Concentration, 0.7)),

            new("Wide Outlet Winger", "Winger", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Decisions, 0.9), (A.WorkRate, 0.9),
                (A.Stamina, 0.8), (A.Concentration, 0.8), (A.Anticipation, 0.7)),

            // Inside Outlet Winger — confirmed key attributes: Off the Ball, Decisions, Anticipation
            new("Inside Outlet Winger", "Winger", RoleCategory.OutOfPossession,
                (A.Anticipation, 1.0), (A.Decisions, 1.0), (A.Positioning, 0.9),
                (A.WorkRate, 0.8), (A.Concentration, 0.8), (A.Stamina, 0.7)),

            // ── AM (AMCL / AMC / AMCR) ──────────────────────────────────────────
            new("Tracking AM", "AM", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Anticipation, 0.9),
                (A.Positioning, 0.8), (A.Decisions, 0.8), (A.Pace, 0.7)),

            new("Central-Outlet AM", "AM", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Decisions, 1.0), (A.Anticipation, 0.9),
                (A.Concentration, 0.9), (A.WorkRate, 0.8), (A.Teamwork, 0.7)),

            new("Splitting-Outlet AM", "AM", RoleCategory.OutOfPossession,
                (A.Anticipation, 1.0), (A.Decisions, 1.0), (A.Positioning, 0.9),
                (A.Pace, 0.8), (A.WorkRate, 0.8), (A.Concentration, 0.7)),

            // ── ST ──────────────────────────────────────────────────────────────
            new("Tracking Centre Forward", "ST", RoleCategory.OutOfPossession,
                (A.WorkRate, 1.0), (A.Stamina, 1.0), (A.Anticipation, 0.9),
                (A.Pace, 0.9), (A.Acceleration, 0.8), (A.Determination, 0.8)),

            new("Central Outlet Centre Forward", "ST", RoleCategory.OutOfPossession,
                (A.Positioning, 1.0), (A.Decisions, 1.0), (A.WorkRate, 0.8),
                (A.Concentration, 0.9), (A.Anticipation, 0.9), (A.Teamwork, 0.7)),

            new("Splitting Outlet Centre Forward", "ST", RoleCategory.OutOfPossession,
                (A.Anticipation, 1.0), (A.Decisions, 1.0), (A.Pace, 0.9),
                (A.Positioning, 0.9), (A.WorkRate, 0.8), (A.Acceleration, 0.7)),
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
