using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gaffer.Models;

namespace Gaffer.Prompts
{
    /// <summary>
    /// Builds the Claude user message for attribute identity analysis.
    /// Isolated here so the prompt can be tuned independently of the analysis logic.
    ///
    /// The local RoleLibrary engine scores every player against every role first.
    /// This class takes that pre-computed output and formats it into a message that
    /// asks Claude for tactical narrative — the parts a scoring formula can't provide.
    /// </summary>
    internal static class AttributeIdentityPrompt
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Builds the user message to send to Claude.
        /// Focuses on misaligned players only — sending the full squad would waste tokens.
        /// </summary>
        public static string Build(AttributeIdentityReport report)
        {
            var misaligned = report.Misaligned;
            var total      = report.Players.Count;

            if (misaligned.Count == 0)
            {
                return
                    $"I've run an attribute identity analysis on my squad of {total} players. " +
                    "The local scoring engine found no significant misalignments — every player's " +
                    "best-fit role is within their current registered position group. " +
                    "Briefly confirm this is a good sign and mention any players who are borderline " +
                    "(the top 3 players by misalignment gap, even if they didn't cross the threshold).";
            }

            // Summarise misaligned players for Claude — top roles + gap, not full attribute dumps
            var summaries = misaligned
                .OrderByDescending(p => p.MisalignmentGap)
                .Take(8)   // cap to avoid blowing context on very large squads
                .Select(p => new
                {
                    player           = p.PlayerName,
                    registeredAs     = $"{p.RegisteredPosition} ({p.RegisteredGroup})",
                    bestFitRole      = p.BestFitRole,
                    bestFitGroup     = p.BestFitGroup,
                    bestFitScore     = $"{p.BestFitScore:F0}/100",
                    currentGroupBest = p.BestRoleInCurrentGroup,
                    currentGroupScore= $"{p.BestScoreInCurrentGroup:F0}/100",
                    gap              = $"{p.MisalignmentGap:F0} pts",
                    top3Roles        = p.TopRoles.Take(3).Select(r =>
                        $"{r.RoleName} ({r.PositionGroup}): {r.Score:F0}").ToList()
                })
                .ToList();

            var json = JsonSerializer.Serialize(summaries, JsonOpts);

            return
                $"I've run an attribute identity analysis on my squad of {total} players.\n\n" +
                $"The local scoring engine found {misaligned.Count} misaligned player(s) — " +
                "players whose best-fit role is in a different position group to where they're " +
                "currently registered.\n\n" +
                "Here is the pre-computed scoring data:\n\n" +
                $"```json\n{json}\n```\n\n" +
                "For each misaligned player:\n" +
                "1. Explain concisely why their attribute profile points to the best-fit role.\n" +
                "2. State whether this misalignment is a problem (wasted potential), an opportunity " +
                   "(hidden gem in disguise), or irrelevant in context.\n" +
                "3. Give a specific tactical recommendation: retrain position, change role/duty " +
                   "within current position, use differently in set formations, or sell.\n\n" +
                "Prioritise your response by misalignment gap — biggest gap first. " +
                "Be direct and specific. No player is just 'versatile' without elaboration.";
        }

        /// <summary>
        /// Builds the system prompt addendum for identity analysis context.
        /// Prepended to the standard Gaffer system prompt.
        /// </summary>
        public static string SystemAddendum =>
            "\n\nFor this request you are performing attribute identity analysis. " +
            "You have been given pre-computed role fitness scores (0–100) for each player, " +
            "where 100 means every key attribute for that role is at the FM maximum of 20. " +
            "A score of 50 represents an average player for that role. " +
            "Use these scores as evidence, not conclusions — your job is to add tactical context " +
            "and actionable recommendations that the scoring formula cannot provide.";
    }
}
