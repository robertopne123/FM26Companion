using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Gaffer.UI;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Gaffer;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Plugin : BasePlugin
{
    internal static ManualLogSource Logger = null!;

    public override void Load()
    {
        Logger = Log;

        ConfigEntry<string> apiKey = Config.Bind("Claude", "ApiKey", string.Empty, "Anthropic API key.");
        ConfigEntry<string> toggleShortcut = Config.Bind("UI", "ToggleShortcut", "F8", "Keyboard shortcut to toggle the Gaffer panel.");

        var dataReader = new DataReader();
        var claudeClient = new ClaudeClient(apiKey);
        var alertEngine = new AlertEngine();
        var attributeIdentityAnalyzer = new AttributeIdentityAnalyzer();

        // IL2CPP-specific: these custom MonoBehaviour types must be registered before AddComponent<T>().
        ClassInjector.RegisterTypeInIl2Cpp<MainThreadDispatcher>();
        ClassInjector.RegisterTypeInIl2Cpp<GafferUI>();
        ClassInjector.RegisterTypeInIl2Cpp<DragHandler>();

        var host = new GameObject("GafferHost");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<MainThreadDispatcher>();

        var ui = host.AddComponent<GafferUI>();
        ui.Initialise(toggleShortcut, dataReader, claudeClient, alertEngine, attributeIdentityAnalyzer);

        Logger.LogInfo("Gaffer loaded successfully");
    }
}
