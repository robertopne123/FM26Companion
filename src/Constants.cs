namespace Gaffer;

/// <summary>Central place for plugin constants and FM26 IL2CPP symbol names.</summary>
public static class Constants
{
    public const string PluginGuid = "com.gaffer.fm26";
    public const string PluginName = "Gaffer";
    public const string PluginVersion = "0.1.0";

    // FM26 IL2CPP type names and field names live here so game patches only require one-file updates.
    public static class FmTypes
    {
        public const string SquadController = "FM26.Game.SquadController";
        public const string PlayerModel = "FM26.Game.Player";
        public const string TacticsController = "FM26.Game.TacticsController";
        public const string MatchController = "FM26.Game.MatchController";
        public const string FinanceController = "FM26.Game.FinanceController";
        public const string FixtureController = "FM26.Game.FixtureController";
    }
}
