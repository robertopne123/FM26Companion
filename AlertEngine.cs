using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gaffer
{
    /// <summary>
    /// Monitors FM26 game state changes independently of user interaction and raises
    /// proactive alerts via GafferBehaviour.ShowAlert().
    ///
    /// Runs on the same host GameObject as GafferBehaviour but in its own MonoBehaviour
    /// so it can be enabled/disabled independently and satisfies single-responsibility.
    ///
    /// Checks are time-gated (every CheckIntervalSeconds) to avoid hammering the
    /// IL2CPP bridge every frame. DataReader methods return null gracefully when game
    /// state is not available, so checks are safe at all times.
    /// </summary>
    public class AlertEngine : MonoBehaviour
    {
        // IL2CPP PATTERN: Required constructor — see GafferBehaviour for explanation.
        public AlertEngine(IntPtr ptr) : base(ptr) { }

        // ── Configuration ─────────────────────────────────────────────────────────

        private const float CheckIntervalSeconds = 30f;

        // Alert thresholds
        private const int   MoraleLowThreshold      = 3;   // FM morale values typically 1–20
        private const int   ConditionLowThreshold    = 65;  // % condition — below this = risk
        private const int   InjuryAlertCount         = 3;   // alert when 3+ players injured
        private const int   ContractExpiryMonths     = 6;   // flag contracts expiring within N months

        // ── State snapshots for change detection ──────────────────────────────────

        private Dictionary<string, string>? _lastMoraleSnapshot;
        private Dictionary<string, int>?    _lastConditionSnapshot;
        private List<string>?               _lastInjuredPlayers;
        private int?                        _lastLeaguePosition;
        private float                       _lastCheckTime = -999f;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Time.time - _lastCheckTime < CheckIntervalSeconds)
                return;

            _lastCheckTime = Time.time;

            try
            {
                RunChecks();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[AlertEngine] Check cycle threw: {ex.Message}");
            }
        }

        // ── Check cycle ───────────────────────────────────────────────────────────

        private void RunChecks()
        {
            CheckMoraleDrops();
            CheckConditionCrisis();
            CheckInjuryCascade();
            CheckContractRisks();
            CheckLeaguePositionChange();
        }

        /// <summary>
        /// Detects players whose morale has dropped significantly since last check.
        /// Reads: DataReader.GetSquadMoraleSnapshot()
        /// </summary>
        private void CheckMoraleDrops()
        {
            var current = DataReader.GetSquadMoraleSnapshot();
            if (current == null || current.Count == 0) return;

            if (_lastMoraleSnapshot != null)
            {
                var dropped = new List<string>();
                foreach (var (player, morale) in current)
                {
                    // Phase 1 TODO: parse morale string to numeric value and compare
                    // against _lastMoraleSnapshot[player]. For now, detect new "Unhappy" states.
                    if (_lastMoraleSnapshot.TryGetValue(player, out var prev)
                        && IsMoraleDrop(prev, morale))
                    {
                        dropped.Add($"{player} ({prev} → {morale})");
                    }
                }

                if (dropped.Count > 0)
                    RaiseAlert($"Morale drop: {string.Join(", ", dropped)}");
            }

            _lastMoraleSnapshot = current;
        }

        /// <summary>
        /// Flags when multiple players fall below the condition threshold simultaneously —
        /// indicator of fixture congestion or recovery mismanagement.
        /// Reads: DataReader.GetSquadConditionSnapshot()
        /// </summary>
        private void CheckConditionCrisis()
        {
            var current = DataReader.GetSquadConditionSnapshot();
            if (current == null || current.Count == 0) return;

            var lowCondition = new List<string>();
            foreach (var (player, condition) in current)
            {
                if (condition < ConditionLowThreshold)
                    lowCondition.Add($"{player} ({condition}%)");
            }

            // Only alert if 3+ players are low — single low condition is normal
            if (lowCondition.Count >= 3)
                RaiseAlert($"Fatigue alert — {lowCondition.Count} players below {ConditionLowThreshold}% condition: {string.Join(", ", lowCondition)}");

            _lastConditionSnapshot = current;
        }

        /// <summary>
        /// Detects when injury count crosses the threshold — may indicate overtraining
        /// or a bad run of physical contact matches.
        /// Reads: DataReader.GetInjuredPlayerNames()
        /// </summary>
        private void CheckInjuryCascade()
        {
            var injured = DataReader.GetInjuredPlayerNames();
            if (injured == null) return;

            var prevCount = _lastInjuredPlayers?.Count ?? 0;

            if (injured.Count >= InjuryAlertCount && injured.Count > prevCount)
                RaiseAlert($"Injury cascade: {injured.Count} players injured — {string.Join(", ", injured)}");

            _lastInjuredPlayers = injured;
        }

        /// <summary>
        /// Flags players approaching contract expiry — contract risk for the club.
        /// Reads: DataReader.GetExpiringContractNames()
        /// </summary>
        private void CheckContractRisks()
        {
            var expiring = DataReader.GetExpiringContractNames(ContractExpiryMonths);
            if (expiring == null || expiring.Count == 0) return;

            RaiseAlert($"Contract risk: {string.Join(", ", expiring)} expire within {ContractExpiryMonths} months");
        }

        /// <summary>
        /// Detects league position changes and alerts on significant drops.
        /// Reads: DataReader.GetLeaguePosition()
        /// </summary>
        private void CheckLeaguePositionChange()
        {
            var pos = DataReader.GetLeaguePosition();
            if (pos == null) return;

            if (_lastLeaguePosition.HasValue && pos > _lastLeaguePosition + 2)
                RaiseAlert($"League position dropped from {_lastLeaguePosition} to {pos}");

            _lastLeaguePosition = pos;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Determines whether a morale string represents a decline.
        /// Phase 1: stub — always returns false until morale values are mapped.
        /// FM26 morale strings are TODO in Constants.cs.
        /// </summary>
        private static bool IsMoraleDrop(string previous, string current)
        {
            // TODO: Map FM26 morale strings to numeric values using a dictionary in Constants.cs
            // e.g. "Excellent" = 5, "Good" = 4, "Okay" = 3, "Poor" = 2, "Unhappy" = 1
            // Return current value < previous value
            return false; // Phase 1 stub
        }

        /// <summary>Dispatches an alert to the Gaffer UI panel.</summary>
        private void RaiseAlert(string message)
        {
            Plugin.Log.LogInfo($"[AlertEngine] {message}");

            // IL2CPP PATTERN: AlertEngine and GafferBehaviour are on the same host
            // GameObject. GetComponent<T>() is bridged through Il2CppInterop — works
            // identically to standard Mono Unity code here.
            var ui = GetComponent<GafferBehaviour>();
            ui?.ShowAlert(message);
        }
    }
}
