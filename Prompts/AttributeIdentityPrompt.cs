using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Gaffer.Models;

namespace Gaffer.Prompts
{
    /// <summary>
    /// Builds the Claude user message for FM26 attribute identity analysis.
    /// Isolated here so the prompt can be tuned independently of the scoring logic.
    ///
    /// FM26 assigns in-possession and out-of-possession roles independently per player.
    /// The prompt separates IP and OP misalignments and asks Claude to reason about each axis.
    /// </summary>
    internal static class AttributeIdentityPrompt
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented  = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Builds the user message for Claude. Sends only the pre-computed
        /// misalignment data — not full attribute dumps — to stay within token budget.
        /// Capped at 8 players to avoid blowing context on large squads.
        /// </summary>
        public static string Build(AttributeIdentityReport report)
        {
            var misaligned = report.Misaligned;
            var total      = report.Players.Count;

            if (misaligned.Count == 0)
            {
                return
                    $"I've run an FM26 dual-role attribute identity analysis on my squad of {total} players. " +
                    "The local scoring engine found no significant misalignments — every player's best " +
                    "in-possession role and best out-of-possession role fall within their registered " +
                    "position group. Briefly confirm this is healthy and highlight any borderline " +
                    "cases (top 3 players by max gap even if below the flagging threshold).";
            }

            var summaries = misaligned
                .OrderByDescending(p => p.MaxMisalignmentGap)
                .Take(8)
                .Select(p => new
                {
                    player           = p.PlayerName,
                    registeredAs     = $"{p.RegisteredPosition} ({p.RegisteredGroup})",
                    inPossession     = p.IsIpMisaligned ? new
                    {
                        misaligned       = true,
                        bestFitRole      = p.BestIpRole,
                        bestFitGroup     = p.BestIpGroup,
                        score            = $"{p.BestIpScore:F0}/100",
                        bestInGroupRole  = p.BestIpRoleInGroup,
                        bestInGroupScore = $"{p.BestIpScoreInGroup:F0}/100",
                        gap              = $"+{p.IpMisalignmentGap:F0} pts",
                        top3             = p.TopIpRoles.Take(3)
                                            .Select(r => $"{r.RoleName} ({r.PositionGroup}): {r.Score:F0}")
                                            .ToList()
                    } : (object)new { misaligned = false },
                    outOfPossession  = p.IsOpMisaligned ? new
                    {
                        misaligned       = true,
                        bestFitRole      = p.BestOpRole,
                        bestFitGroup     = p.BestOpGroup,
                        score            = $"{p.BestOpScore:F0}/100",
                        bestInGroupRole  = p.BestOpRoleInGroup,
                        bestInGroupScore = $"{p.BestOpScoreInGroup:F0}/100",
                        gap              = $"+{p.OpMisalignmentGap:F0} pts",
                        top3             = p.TopOpRoles.Take(3)
                                            .Select(r => $"{r.RoleName} ({r.PositionGroup}): {r.Score:F0}")
                                            .ToList()
                    } : (object)new { misaligned = false }
                })
                .ToList();

            var json = JsonSerializer.Serialize(summaries, JsonOpts);

            return
                $"I've run an FM26 attribute identity analysis on my squad of {total} players.\n\n" +
                $"FM26 assigns in-possession and out-of-possession roles independently. " +
                $"The local scoring engine found {misaligned.Count} player(s) with significant " +
                "misalignments on at least one axis. Pre-computed data below:\n\n" +
                $"```json\n{json}\n```\n\n" +
                "For each misaligned player, address each misaligned axis (IP and/or OP) separately:\n" +
                "1. **Why** their attributes point to the best-fit role on that axis.\n" +
                "2. **Whether it's a problem or opportunity** — wasted potential vs hidden positional flexibility.\n" +
                "3. **Specific FM26 recommendation**: change their in-possession role, out-of-possession role, " +
                   "retrain to a new position, adjust their slot in the formation, or sell.\n\n" +
                "Prioritise by max gap — biggest misalignment first. " +
                "If a player has both IP and OP misalignments pointing to the same group, treat that " +
                "as a strong signal. If they point to different groups, that's an interesting split — explain it.";
        }

        /// <summary>
        /// System prompt addendum specific to identity analysis.
        /// Appended to the base Gaffer system prompt for this request type.
        /// </summary>
        public static string SystemAddendum =>
            "\n\nFor this request you are running FM26 dual-role attribute identity analysis. " +
            "FM26 assigns two independent roles per player: one in-possession (what they do with the ball) " +
            "and one out-of-possession (what they do when defending or pressing). " +
            "Pre-computed fitness scores (0–100) are provided for both axes, where 100 means every key " +
            "attribute for that role is at the FM maximum of 20, and 50 represents a league-average player. " +
            "Use scores as evidence for your tactical reasoning, not as conclusions in themselves. " +
            "Your job is to add the football context and actionable FM26 recommendations the algorithm cannot provide.";
    }
}
