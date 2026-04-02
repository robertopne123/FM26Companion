using System;
using System.Collections.Generic;
using System.Linq;
using Gaffer.Models;

namespace Gaffer;

/// <summary>Finds attribute-vs-position mismatches and recommends FM26 IP/OOP role identities.</summary>
public sealed class AttributeIdentityAnalyzer
{
    private static readonly IReadOnlyList<RoleProfile> Profiles = new List<RoleProfile>
    {
        new("Ball-Playing CB", "IP", new[] { "Passing", "Vision", "Composure", "FirstTouch", "Decisions" }),
        new("No-Nonsense CB", "OOP", new[] { "Tackling", "Marking", "Positioning", "Strength", "Bravery" }),
        new("Pressing Full-Back", "OOP", new[] { "WorkRate", "Aggression", "Stamina", "Acceleration", "Tackling" }),
        new("Playmaking Wing-Back", "IP", new[] { "Crossing", "Passing", "Vision", "Technique", "Decisions" }),
        new("Deep-Lying Playmaker", "IP", new[] { "Passing", "Vision", "Composure", "Technique", "Decisions" }),
        new("Pressing DM", "OOP", new[] { "WorkRate", "Stamina", "Aggression", "Positioning", "Tackling" }),
        new("Box-to-Box Playmaker", "IP", new[] { "Stamina", "WorkRate", "Passing", "Vision", "OffTheBall" }),
        new("Channel Midfielder", "IP", new[] { "OffTheBall", "Acceleration", "Dribbling", "WorkRate", "Decisions" }),
        new("Tracking AM", "OOP", new[] { "WorkRate", "Teamwork", "Stamina", "Positioning", "Aggression" }),
        new("Advanced Playmaker", "IP", new[] { "Vision", "Passing", "Technique", "Flair", "Decisions" }),
        new("Wide Outlet Midfielder", "OOP", new[] { "Acceleration", "Pace", "OffTheBall", "Dribbling", "Composure" }),
        new("Central Outlet AM", "OOP", new[] { "OffTheBall", "Composure", "Anticipation", "Acceleration", "Finishing" })
    };

    /// <summary>Analyzes the current squad and returns ranked role-identity recommendations per player.</summary>
    public IReadOnlyList<AttributeIdentityResult> Analyze(GameStateSnapshot snapshot)
    {
        var results = new List<AttributeIdentityResult>();

        foreach (var player in snapshot.Squad.Players)
        {
            if (player.Attributes.Count == 0)
            {
                continue;
            }

            var topFits = Profiles
                .Select(profile => new RoleFitScore(profile.Name, profile.Phase, ComputeScore(player.Attributes, profile)))
                .OrderByDescending(score => score.Score)
                .Take(3)
                .ToList();

            if (topFits.Count == 0)
            {
                continue;
            }

            var primary = topFits[0];
            var secondary = topFits.Count > 1 ? topFits[1] : topFits[0];

            bool misaligned = !RoleLooksCompatible(player.RegisteredPosition, primary.RoleName);

            results.Add(new AttributeIdentityResult(
                player.PlayerId,
                player.Name,
                player.RegisteredPosition,
                $"{primary.RoleName} ({primary.Phase})",
                $"{secondary.RoleName} ({secondary.Phase})",
                misaligned,
                topFits,
                BuildSummary(player, primary, secondary, misaligned)));
        }

        return results.OrderByDescending(result => result.IsMisaligned).ThenBy(result => result.PlayerName).ToList();
    }

    private static double ComputeScore(IReadOnlyDictionary<string, int> attributes, RoleProfile profile)
    {
        double total = 0;

        foreach (string attribute in profile.KeyAttributes)
        {
            if (attributes.TryGetValue(attribute, out int value))
            {
                total += Math.Clamp(value, 1, 20);
            }
            else
            {
                total += 1;
            }
        }

        return total / profile.KeyAttributes.Length;
    }

    private static bool RoleLooksCompatible(string registeredPosition, string roleName)
    {
        string position = registeredPosition.ToLowerInvariant();
        string role = roleName.ToLowerInvariant();

        if (position.Contains("cb") || position.Contains("centre-back")) return role.Contains("cb") || role.Contains("back");
        if (position.Contains("dm")) return role.Contains("dm") || role.Contains("playmaker") || role.Contains("midfielder");
        if (position.Contains("cm")) return role.Contains("midfielder") || role.Contains("playmaker");
        if (position.Contains("am")) return role.Contains("am") || role.Contains("playmaker") || role.Contains("outlet");
        if (position.Contains("wb") || position.Contains("fb")) return role.Contains("back");
        if (position.Contains("wing")) return role.Contains("wide") || role.Contains("wing");

        return true;
    }

    private static string BuildSummary(PlayerState player, RoleFitScore primary, RoleFitScore secondary, bool misaligned)
    {
        string flag = misaligned ? "Misaligned" : "Aligned";
        return $"{flag}: {player.Name} best fits {primary.RoleName} ({primary.Phase}) with backup {secondary.RoleName} ({secondary.Phase}).";
    }

    private sealed record RoleProfile(string Name, string Phase, string[] KeyAttributes);
}
