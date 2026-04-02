using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gaffer.Models;
using Gaffer.Prompts;
using static Gaffer.RoleLibrary;

namespace Gaffer
{
    /// <summary>
    /// Orchestrates FM26 attribute identity analysis.
    ///
    /// FM26 uses separate in-possession (IP) and out-of-possession (OP) roles per player.
    /// Analysis therefore produces two independent scores per player:
    ///   - IP score: how well a player's on-ball attributes suit each IP role
    ///   - OP score: how well a player's off-ball attributes suit each OP role
    ///
    /// A player is flagged as misaligned when either their best IP role or their best OP
    /// role falls in a different position group to their registered position, AND the gap
    /// between their best score and their best in-group score exceeds MisalignmentThreshold.
    ///
    /// Two-phase design:
    ///   1. AnalyseSquad()              — pure C# scoring, instant, no API.
    ///   2. AnalyseSquadWithClaudeAsync() — adds Claude narrative on top.
    /// </summary>
    internal static class AttributeAnalyser
    {
        /// <summary>
        /// Minimum score gap (0–100) for a misalignment to be flagged.
        /// Best-fit role must outscore best in-group role by this amount.
        /// </summary>
        public const double MisalignmentThreshold = 8.0;

        // ── Public entry points ───────────────────────────────────────────────────

        /// <summary>
        /// Runs local IP+OP scoring for the entire squad synchronously.
        /// Returns immediately — no API call.
        /// </summary>
        public static AttributeIdentityReport AnalyseSquad(Squad? squad)
        {
            var report = new AttributeIdentityReport();
            if (squad == null || squad.Players.Count == 0)
                return report;

            foreach (var player in squad.Players)
            {
                var result = AnalysePlayer(player);
                report.Players.Add(result);
                if (result.IsMisaligned)
                    report.Misaligned.Add(result);
            }

            report.Misaligned = report.Misaligned
                .OrderByDescending(p => p.MaxMisalignmentGap)
                .ToList();

            return report;
        }

        /// <summary>
        /// Runs local scoring then calls Claude for tactical narrative.
        /// Throws on API error — callers should catch.
        /// </summary>
        public static async Task<AttributeIdentityReport> AnalyseSquadWithClaudeAsync(
            Squad?  squad,
            string  apiKey,
            string  gameStateJson)
        {
            var report       = AnalyseSquad(squad);
            var systemPrompt = Constants.ClaudeSystemPromptBase
                + AttributeIdentityPrompt.SystemAddendum
                + $"\n\nCurrent game state:\n{gameStateJson}";
            var userMessage  = AttributeIdentityPrompt.Build(report);

            report.ClaudeNarrative = await ClaudeClient.SendAsync(
                apiKey, systemPrompt, userMessage).ConfigureAwait(false);

            return report;
        }

        // ── Per-player analysis ───────────────────────────────────────────────────

        /// <summary>
        /// Scores a single player across all IP and OP roles independently,
        /// then determines misalignment on each axis.
        /// </summary>
        public static PlayerIdentityResult AnalysePlayer(Player player)
        {
            var group   = GetPositionGroup(player.RegisteredPosition);
            var attrs   = player.Attributes;

            var allIp   = ScoreAllInPossession(attrs);
            var allOp   = ScoreAllOutOfPossession(attrs);

            var bestIp  = allIp[0];
            var bestOp  = allOp[0];

            var bestIpInGroup = allIp.FirstOrDefault(r => r.PositionGroup == group);
            var bestOpInGroup = allOp.FirstOrDefault(r => r.PositionGroup == group);

            var ipGap = bestIp.Score - (bestIpInGroup?.Score ?? 0);
            var opGap = bestOp.Score - (bestOpInGroup?.Score ?? 0);

            return new PlayerIdentityResult
            {
                PlayerName             = player.Name,
                RegisteredPosition     = player.RegisteredPosition,
                RegisteredGroup        = group,

                // IP
                BestIpRole             = bestIp.RoleName,
                BestIpGroup            = bestIp.PositionGroup,
                BestIpScore            = Round(bestIp.Score),
                BestIpRoleInGroup      = bestIpInGroup?.RoleName,
                BestIpScoreInGroup     = Round(bestIpInGroup?.Score ?? 0),
                IpMisalignmentGap      = Round(ipGap),
                IsIpMisaligned         = bestIp.PositionGroup != group && ipGap >= MisalignmentThreshold,
                TopIpRoles             = allIp.Take(4).ToList(),

                // OP
                BestOpRole             = bestOp.RoleName,
                BestOpGroup            = bestOp.PositionGroup,
                BestOpScore            = Round(bestOp.Score),
                BestOpRoleInGroup      = bestOpInGroup?.RoleName,
                BestOpScoreInGroup     = Round(bestOpInGroup?.Score ?? 0),
                OpMisalignmentGap      = Round(opGap),
                IsOpMisaligned         = bestOp.PositionGroup != group && opGap >= MisalignmentThreshold,
                TopOpRoles             = allOp.Take(4).ToList(),
            };
        }

        // ── Local summary formatter ───────────────────────────────────────────────

        /// <summary>
        /// Formats the local scoring results as panel text, displayed before Claude replies.
        /// Separates IP and OP misalignments so the manager can see which axis is the problem.
        /// </summary>
        public static string FormatLocalSummary(AttributeIdentityReport report)
        {
            if (report.Players.Count == 0)
                return "No squad data available — DataReader stubs active. Wire up FM26 IL2CPP types to see real results.";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Attribute Identity Analysis  ({report.Players.Count} players scored)");
            sb.AppendLine();

            if (report.Misaligned.Count == 0)
            {
                sb.AppendLine("No significant misalignments detected.");
                sb.AppendLine("All players' best IP and OP roles align with their registered position group.");
                return sb.ToString();
            }

            sb.AppendLine($"⚠  {report.Misaligned.Count} misaligned player(s) — sorted by largest gap:");
            sb.AppendLine();

            foreach (var p in report.Misaligned)
            {
                sb.Append($"  {p.PlayerName ?? "?"} [{p.RegisteredPosition ?? "?"}]");

                if (p.IsIpMisaligned)
                    sb.Append($"  IP→ {p.BestIpRole} ({p.BestIpGroup}, {p.BestIpScore:F0}/100, +{p.IpMisalignmentGap:F0})");

                if (p.IsOpMisaligned)
                    sb.Append($"  OP→ {p.BestOpRole} ({p.BestOpGroup}, {p.BestOpScore:F0}/100, +{p.OpMisalignmentGap:F0})");

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Sending to Claude for tactical analysis…");
            return sb.ToString();
        }

        private static double Round(double v) => Math.Round(v, 1);
    }
}
