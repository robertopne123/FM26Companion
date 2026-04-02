using System;
using System.Collections.Generic;

namespace Gaffer.Models;

public record GameStateSnapshot(
    DateTime CapturedAtUtc,
    SquadState Squad,
    TacticsState Tactics,
    MatchState Match,
    FinanceState Finances,
    IReadOnlyList<FixtureState> Fixtures);

public record SquadState(IReadOnlyList<PlayerState> Players);

public record PlayerState(
    int PlayerId,
    string Name,
    string RegisteredPosition,
    int Morale,
    int Condition,
    int MatchSharpness,
    IReadOnlyDictionary<string, int> Attributes);

public record TacticsState(string Formation, string InPossessionShape, string OutOfPossessionShape);

public record MatchState(bool IsMatchLive, int Minute, int HomeScore, int AwayScore, string Opponent);

public record FinanceState(decimal Balance, decimal WageBudget, decimal TransferBudget);

public record FixtureState(DateTime DateUtc, string Opponent, bool IsHome, bool IsPlayed);

public record AlertMessage(string Title, string Body, DateTime TimestampUtc);
