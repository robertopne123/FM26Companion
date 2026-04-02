using System;
using System.Collections.Generic;
using Gaffer.Models;

namespace Gaffer;

/// <summary>Monitors game-state changes and emits proactive alerts without user prompts.</summary>
public sealed class AlertEngine
{
    private GameStateSnapshot? _lastState;

    /// <summary>Compares current and previous snapshots and returns newly detected alert messages.</summary>
    public IReadOnlyList<AlertMessage> Evaluate(GameStateSnapshot current)
    {
        var alerts = new List<AlertMessage>();

        if (_lastState is not null)
        {
            if (current.Squad.Players.Count > 0 && _lastState.Squad.Players.Count > 0)
            {
                alerts.AddRange(DetectMoraleDrops(_lastState, current));
                alerts.AddRange(DetectConditionRisk(current));
            }
        }

        _lastState = current;
        return alerts;
    }

    private static IEnumerable<AlertMessage> DetectMoraleDrops(GameStateSnapshot previous, GameStateSnapshot current)
    {
        foreach (var player in current.Squad.Players)
        {
            var prior = FindPlayer(previous, player.PlayerId);
            if (prior is null)
            {
                continue;
            }

            if (prior.Morale - player.Morale >= 3)
            {
                yield return new AlertMessage(
                    "Morale Drop",
                    $"{player.Name} morale dropped from {prior.Morale} to {player.Morale}.",
                    DateTime.UtcNow);
            }
        }
    }

    private static IEnumerable<AlertMessage> DetectConditionRisk(GameStateSnapshot current)
    {
        foreach (var player in current.Squad.Players)
        {
            if (player.Condition <= 70)
            {
                yield return new AlertMessage(
                    "Condition Risk",
                    $"{player.Name} condition is {player.Condition}; consider rotation.",
                    DateTime.UtcNow);
            }
        }
    }

    private static PlayerState? FindPlayer(GameStateSnapshot snapshot, int playerId)
    {
        foreach (var player in snapshot.Squad.Players)
        {
            if (player.PlayerId == playerId)
            {
                return player;
            }
        }

        return null;
    }
}
