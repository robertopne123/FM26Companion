using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using FM26Companion.UI;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace FM26Companion;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    // Exposed so other classes (GafferPanel, DataReader, etc.) can write to the BepInEx log
    // without needing their own LogSource.
    internal static ManualLogSource Log = null!;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo("Gaffer: plugin loaded — FM26 IL2CPP injection confirmed.");

        // Wire up Claude API key from BepInEx config before any UI is built.
        ClaudeClient.Instance.Initialise(Config);

        // ── IL2CPP-specific: Register custom MonoBehaviour subclasses ──────────
        // In standard Mono BepInEx you can add any MonoBehaviour freely.
        // In IL2CPP, the runtime only knows about types that existed at compile
        // time (from the original C# → IL2CPP pass). Any NEW MonoBehaviour you
        // define in your plugin must be registered here so IL2CPP allocates the
        // correct native object size and vtable for it.
        // Forgetting this causes a native crash the moment Unity calls a virtual
        // method (Awake, Update, etc.) on the component.
        ClassInjector.RegisterTypeInIl2Cpp<GafferPanel>();
        ClassInjector.RegisterTypeInIl2Cpp<DragHandler>();

        // Create a persistent host GameObject that survives scene loads.
        // GafferPanel's Awake() will build the full uGUI hierarchy.
        var host = new GameObject("GafferHost");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<GafferPanel>();

        Log.LogInfo("Gaffer: UI host initialised.");
    }
}
