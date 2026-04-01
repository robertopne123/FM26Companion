using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Gaffer
{
    /// <summary>
    /// BepInEx 6 entry point. Wires all components together — nothing else lives here.
    /// </summary>
    [BepInPlugin(Constants.PluginGuid, Constants.PluginName, Constants.PluginVersion)]
    public class Plugin : BasePlugin
    {
        // IL2CPP PATTERN: BasePlugin is BepInEx 6's base class for IL2CPP games.
        // Mono plugins use BaseUnityPlugin. IL2CPP plugins use BasePlugin with Load()
        // instead of Awake(), because the plugin is not itself a MonoBehaviour.

        internal static new ManualLogSource Log { get; private set; } = null!;

        // Config entries exposed statically so GafferBehaviour and ClaudeClient can read them
        // without passing references around.
        internal static BepInEx.Configuration.ConfigEntry<string> ApiKey    { get; private set; } = null!;
        internal static BepInEx.Configuration.ConfigEntry<string> ToggleKey { get; private set; } = null!;

        public override void Load()
        {
            Log = base.Log;

            BindConfig();
            RegisterIl2CppTypes();
            SpawnHost();

            Log.LogInfo("Gaffer loaded successfully");
        }

        // ── Config ────────────────────────────────────────────────────────────────

        private void BindConfig()
        {
            ApiKey = Config.Bind(
                Constants.ConfigSectionClaude,
                Constants.ConfigKeyApiKey,
                "",
                "Your Anthropic API key. Get one at https://console.anthropic.com — never share this file.");

            ToggleKey = Config.Bind(
                Constants.ConfigSectionGeneral,
                Constants.ConfigKeyToggleKey,
                "F8",
                "Keyboard key to toggle the Gaffer panel open/closed (e.g. F8, F9, BackQuote).");
        }

        // ── IL2CPP type registration ───────────────────────────────────────────────

        private static void RegisterIl2CppTypes()
        {
            // IL2CPP PATTERN: Any class that inherits MonoBehaviour and is NOT a type
            // that already exists in the game's IL2CPP binary must be registered here
            // before AddComponent<T>() is called. ClassInjector injects the managed
            // type into the IL2CPP type system so the runtime can instantiate it.
            // Failing to call this before AddComponent will throw an exception.
            ClassInjector.RegisterTypeInIl2Cpp<GafferBehaviour>();
            ClassInjector.RegisterTypeInIl2Cpp<AlertEngine>();
        }

        // ── Host GameObject ────────────────────────────────────────────────────────

        private static void SpawnHost()
        {
            var host = new GameObject(Constants.HostObjectName);

            // DontDestroyOnLoad keeps the host alive across scene loads (menus, matches, etc.)
            Object.DontDestroyOnLoad(host);

            // GafferBehaviour owns the UI panel and coordinates all Claude calls
            host.AddComponent<GafferBehaviour>();

            // AlertEngine runs independently, monitoring state changes in the background
            host.AddComponent<AlertEngine>();
        }
    }
}
