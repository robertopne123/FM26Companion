using UnityEngine;

namespace Gaffer
{
    /// <summary>
    /// Single source of truth for all magic strings and shared constants.
    /// All FM26 IL2CPP type names, field names, and Unity object names live here.
    /// When an FM26 patch renames a type or field, update it here — nowhere else.
    /// </summary>
    internal static class Constants
    {
        // ── Plugin identity ────────────────────────────────────────────────────────

        public const string PluginGuid    = "com.gaffer.fm26";
        public const string PluginName    = "Gaffer";
        public const string PluginVersion = "1.0.0";

        // ── BepInEx config sections / keys ─────────────────────────────────────────

        public const string ConfigSectionClaude  = "Claude";
        public const string ConfigSectionGeneral = "General";

        public const string ConfigKeyApiKey    = "ApiKey";
        public const string ConfigKeyToggleKey = "ToggleKey";

        // ── Claude API ─────────────────────────────────────────────────────────────

        public const string ClaudeModel   = "claude-sonnet-4-20250514";
        public const int    ClaudeMaxTokens = 1000;
        public const string ClaudeApiUrl  = "https://api.anthropic.com/v1/messages";
        public const string ClaudeApiVersion = "2023-06-01";

        public const string ClaudeSystemPromptBase =
            "You are Gaffer, an expert Football Manager 2026 coaching analyst embedded " +
            "directly inside the game. You have full access to the manager's live squad data, " +
            "tactics, match state, finances, and fixture list provided as structured JSON. " +
            "Be concise, tactical, and specific. Use FM terminology. Never break the fourth " +
            "wall — you are an assistant coach, not an AI. Responses should be under 200 words " +
            "unless the manager asks for a detailed breakdown.";

        // ── Unity object names ────────────────────────────────────────────────────

        public const string HostObjectName        = "GafferHost";
        public const string CanvasObjectName      = "GafferCanvas";
        public const string EventSystemObjectName = "GafferEventSystem";

        // ── Panel dimensions and layout ───────────────────────────────────────────

        public const float PanelWidth    = 440f;
        public const float PanelHeight   = 620f;
        public const float PanelInitialX = 20f;   // pixels from left edge
        public const float PanelInitialY = -20f;  // pixels from top edge (negative = down)
        public const float TitleBarHeight = 38f;
        public const float FooterHeight   = 42f;

        // ── Colour palette (dark theme — blends with FM26's aesthetic) ─────────────

        public static readonly Color PanelBackground  = new(0.10f, 0.10f, 0.12f, 0.94f);
        public static readonly Color TitleBarBackground = new(0.14f, 0.14f, 0.18f, 1.00f);
        public static readonly Color FooterBackground  = new(0.12f, 0.12f, 0.15f, 1.00f);
        public static readonly Color ButtonBackground  = new(0.22f, 0.38f, 0.60f, 1.00f);
        public static readonly Color TextColor         = new(0.88f, 0.88f, 0.92f, 1.00f);
        public static readonly Color SubtleTextColor   = new(0.55f, 0.55f, 0.60f, 1.00f);

        // ── FM26 IL2CPP type names ─────────────────────────────────────────────────
        //
        // These are the fully-qualified IL2CPP type names as they appear in the
        // BepInEx/interop/ dump. Identify them by:
        //   1. Searching the interop DLLs with ILSpy/dnSpy after first BepInEx run
        //   2. Using BepInEx's Il2CppDumper output (BepInEx/utils/dump.cs)
        //   3. Logging via: foreach (var t in Il2CppSystem.AppDomain.CurrentDomain
        //                       .GetAssemblies()) Log(t.GetTypes())
        //
        // All are marked TODO — populate after running FM26 with BepInEx once.

        // Root game manager — entry point for all FM26 game state
        // TODO: Identify via Il2CppDumper — likely "GameEngine" or "FootballManager"
        public const string FM_GameManagerTypeName = "TODO_GameManager";

        // Player data
        // TODO: Identify the IL2CPP type representing a player entity
        public const string FM_PlayerTypeName = "TODO_Player";
        // TODO: Field on the player type that holds the name string
        public const string FM_PlayerFieldName = "TODO_playerName";
        // TODO: Field or property that returns player attributes object
        public const string FM_PlayerFieldAttributes = "TODO_attributes";
        // TODO: Field that holds the player's condition value (0–100 int)
        public const string FM_PlayerFieldCondition = "TODO_condition";
        // TODO: Field that holds morale enum/int
        public const string FM_PlayerFieldMorale = "TODO_morale";

        // Squad / club
        // TODO: Type holding the collection of players in a club's squad
        public const string FM_SquadTypeName = "TODO_Squad";
        // TODO: Collection field on the club type that holds squad members
        public const string FM_ClubFieldSquad = "TODO_squad";
        // TODO: The human manager's club type
        public const string FM_HumanClubTypeName = "TODO_HumanClub";

        // Tactics
        // TODO: Type representing current tactical setup
        public const string FM_TacticsTypeName = "TODO_Tactics";
        // TODO: Field holding the formation string (e.g. "4-3-3")
        public const string FM_TacticsFieldFormation = "TODO_formation";
        // TODO: Field holding mentality enum/string
        public const string FM_TacticsFieldMentality = "TODO_mentality";

        // Match state
        // TODO: Type representing in-progress match data
        public const string FM_MatchTypeName = "TODO_Match";
        // TODO: Field holding current match minute
        public const string FM_MatchFieldMinute = "TODO_matchMinute";

        // Finances
        // TODO: Type holding club financial data
        public const string FM_FinancesTypeName = "TODO_Finances";
        // TODO: Field holding transfer budget
        public const string FM_FinancesFieldTransferBudget = "TODO_transferBudget";
        // TODO: Field holding weekly wage budget
        public const string FM_FinancesFieldWageBudget = "TODO_wageBudget";
    }
}
