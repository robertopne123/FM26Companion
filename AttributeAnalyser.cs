using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gaffer.Models;
using Gaffer.Prompts;

namespace Gaffer
{
    /// <summary>
    /// Orchestrates attribute identity analysis.
    ///
    /// Two-phase design:
    ///   1. Local analysis  — pure C# role scoring via RoleLibrary. Instant, no API.
    ///                        Works even with partial attribute data (missing attrs → average).
    ///   2. Claude analysis — sends misalignment report to Claude for tactical narrative.
    ///                        Optional; skipped if API key is absent or call fails.
    ///
    /// Callers always receive the full local analysis immediately. Claude's narrative
    /// is populated on the same report object when the async phase completes.
    /// </summary>
    internal static class AttributeAnalyser
    {
        /// <summary>
        /// Score gap (0–100) above which a player is considered misaligned.
        /// i.e. best-fit role must outscore best role in current group by this much.
        /// Higher = fewer (but more confident) misalignment flags.
        /// </summary>
        public const double MisalignmentThreshold = 8.0;

        // ── Public entry points ───────────────────────────────────────────────────

        /// <summary>
        /// Runs local attribute scoring for the entire squad synchronously.
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

            // Sort misaligned by gap descending — biggest misalignments first
            report.Misaligned = report.Misaligned
                .OrderByDescending(p => p.MisalignmentGap)
                .ToList();

            return report;
        }

        /// <summary>
        /// Runs local scoring then calls Claude for narrative analysis.
        /// The returned report has ClaudeNarrative populated when the task completes.
        /// Throws on API error — callers should catch.
        /// </summary>
        public static async Task<AttributeIdentityReport> AnalyseSquadWithClaudeAsync(
            Squad?  squad,
            string  apiKey,
            string  gameStateJson)
        {
            var report = AnalyseSquad(squad);

            var systemPrompt = Constants.ClaudeSystemPromptBase
                + AttributeIdentityPrompt.SystemAddendum
                + $"\n\nCurrent game state context:\n{gameStateJson}";

            var userMessage  = AttributeIdentityPrompt.Build(report);

            report.ClaudeNarrative = await ClaudeClient.SendAsync(
                apiKey, systemPrompt, userMessage).ConfigureAwait(false);

            return report;
        }

        // ── Per-player analysis ───────────────────────────────────────────────────

        /// <summary>
        /// Scores a single player against all roles and determines whether
        /// their attributes indicate a position misalignment.
        /// </summary>
        public static PlayerIdentityResult AnalysePlayer(Player player)
        {
            var registeredGroup = RoleLibrary.GetPositionGroup(player.RegisteredPosition);
            var allScores       = RoleLibrary.ScoreAllRoles(player.Attributes);

            // Best fit across all roles
            var best = allScores[0]; // already sorted descending

            // Best fit within the player's current registered position group
            var bestInGroup = allScores
                .FirstOrDefault(r => r.PositionGroup == registeredGroup);

            var bestInGroupScore = bestInGroup?.Score ?? 0;
            var bestInGroupRole  = bestInGroup?.RoleName;

            var gap = best.Score - bestInGroupScore;

            return new PlayerIdentityResult
            {
                PlayerName             = player.Name,
                RegisteredPosition     = player.RegisteredPosition,
                RegisteredGroup        = registeredGroup,
                BestFitRole            = best.RoleName,
                BestFitGroup           = best.PositionGroup,
                BestFitScore           = Math.Round(best.Score, 1),
                BestRoleInCurrentGroup = bestInGroupRole,
                BestScoreInCurrentGroup= Math.Round(bestInGroupScore, 1),
                MisalignmentGap        = Math.Round(gap, 1),
                IsMisaligned           = best.PositionGroup != registeredGroup
                                         && gap >= MisalignmentThreshold,
                TopRoles               = allScores.Take(5).ToList()
            };
        }

        // ── Formatting helpers for display ────────────────────────────────────────

        /// <summary>
        /// Formats the local analysis portion of the report as readable panel text.
        /// Used to display results before Claude's narrative arrives.
        /// </summary>
        public static string FormatLocalSummary(AttributeIdentityReport report)
        {
            if (report.Players.Count == 0)
                return "No squad data available. DataReader stubs are active — wire up FM26 types to see real results.";

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"Attribute Identity Analysis — {report.Players.Count} players scored");
            lines.AppendLine();

            if (report.Misaligned.Count == 0)
            {
                lines.AppendLine("No significant misalignments detected.");
                lines.AppendLine("All players' best-fit roles are within their registered position groups.");
            }
            else
            {
                lines.AppendLine($"⚠  {report.Misaligned.Count} misaligned player(s):");
                lines.AppendLine();

                foreach (var p in report.Misaligned)
                {
                    lines.AppendLine(
                        $"  {p.PlayerName ?? "Unknown"} " +
                        $"[{p.RegisteredPosition ?? "?"}] → best fit: {p.BestFitRole} " +
                        $"({p.BestFitGroup}, {p.BestFitScore:F0}/100, gap +{p.MisalignmentGap:F0})");
                }
            }

            lines.AppendLine();
            lines.AppendLine("Fetching Claude's tactical analysis…");
            return lines.ToString();
        }
    }
}
