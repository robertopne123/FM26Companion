using FM26Companion;
using UnityEngine;
using UnityEngine.UI;

namespace FM26Companion.UI;

/// <summary>
/// Main Gaffer overlay panel. Responsible only for building and owning the
/// uGUI hierarchy. Logic (Claude calls, FM data reads) is triggered via
/// method calls from this class but lives in ClaudeClient / DataReader.
/// </summary>
public class GafferPanel : MonoBehaviour
{
    private GameObject? _panel;
    private Text? _statusText;
    private bool _visible = true;

    // ── IL2CPP constructor requirement ────────────────────────────────────────
    // All MonoBehaviour subclasses registered with ClassInjector MUST expose
    // an IntPtr constructor. Unity's IL2CPP runtime passes the native object
    // pointer through this path when it instantiates the component via
    // AddComponent<T>(). Without it the runtime will throw a MissingMethodException
    // on the Mono side when trying to wrap the native allocation.
    public GafferPanel(System.IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        Plugin.Log.LogInfo("Gaffer: GafferPanel.Awake()");
        BuildUI();
    }

    private void BuildUI()
    {
        // ── Canvas ────────────────────────────────────────────────────────────
        // We create our own Canvas rather than searching for FM's, so that
        // Gaffer is always on top and never affected by FM's canvas sort order.
        var canvasGo = new GameObject("GafferCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;  // paint over everything

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // ── Panel window ──────────────────────────────────────────────────────
        _panel = CreatePanel(canvasGo.transform);
        BuildTitleBar(_panel.transform);
        BuildBody(_panel.transform);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("GafferPanel");
        panel.transform.SetParent(parent, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360f, 480f);
        rt.anchoredPosition = new Vector2(-20f, 20f);
        // Anchor: top-right of screen
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);

        // Semi-transparent dark background
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.10f, 0.92f);

        // Drag: attach the handler so the user can reposition the window
        panel.AddComponent<DragHandler>();

        return panel;
    }

    private void BuildTitleBar(Transform parent)
    {
        // ── Title bar container ───────────────────────────────────────────────
        var bar = new GameObject("TitleBar");
        bar.transform.SetParent(parent, false);

        var rt = bar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 36f);
        rt.anchoredPosition = Vector2.zero;

        var img = bar.AddComponent<Image>();
        img.color = new Color(0.10f, 0.45f, 0.20f, 1f);  // FM-ish green

        // ── "Gaffer" title text ───────────────────────────────────────────────
        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(bar.transform, false);

        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.sizeDelta = new Vector2(-40f, 0f);  // leave room for close button
        titleRt.anchoredPosition = new Vector2(8f, 0f);

        var title = titleGo.AddComponent<Text>();
        title.text      = "Gaffer";
        title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize  = 16;
        title.fontStyle = FontStyle.Bold;
        title.color     = Color.white;
        title.alignment = TextAnchor.MiddleLeft;

        // ── Close button ──────────────────────────────────────────────────────
        var closeGo = new GameObject("CloseButton");
        closeGo.transform.SetParent(bar.transform, false);

        var closeRt = closeGo.AddComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 0f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot     = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(36f, 0f);
        closeRt.anchoredPosition = Vector2.zero;

        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.75f, 0.15f, 0.15f, 1f);

        var closeBtn = closeGo.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;

        // ── IL2CPP event note ────────────────────────────────────────────────
        // In standard Mono BepInEx you'd write:  closeBtn.onClick.AddListener(Hide);
        // In IL2CPP the delegate must be wrapped in an Il2CppSystem.Action so it
        // crosses the Mono↔IL2CPP boundary correctly. The Il2CppInterop layer
        // provides the implicit conversion from System.Action.
        closeBtn.onClick.AddListener(new System.Action(Hide));

        var closeLabelGo = new GameObject("Label");
        closeLabelGo.transform.SetParent(closeGo.transform, false);

        var closeLabelRt = closeLabelGo.AddComponent<RectTransform>();
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.sizeDelta = Vector2.zero;

        var closeLabel = closeLabelGo.AddComponent<Text>();
        closeLabel.text      = "✕";
        closeLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeLabel.fontSize  = 14;
        closeLabel.color     = Color.white;
        closeLabel.alignment = TextAnchor.MiddleCenter;
    }

    private void BuildBody(Transform parent)
    {
        // ── Body area ─────────────────────────────────────────────────────────
        var body = new GameObject("Body");
        body.transform.SetParent(parent, false);

        var rt = body.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(8f, 8f);
        rt.offsetMax = new Vector2(-8f, -40f);

        // ── Status / response text ────────────────────────────────────────────
        var statusGo = new GameObject("StatusText");
        statusGo.transform.SetParent(body.transform, false);

        var statusRt = statusGo.AddComponent<RectTransform>();
        statusRt.anchorMin = Vector2.zero;
        statusRt.anchorMax = Vector2.one;
        statusRt.sizeDelta = Vector2.zero;

        _statusText = statusGo.AddComponent<Text>();
        _statusText.text      = "Gaffer online.\nClaude API: not tested yet.\n\nPress [G] to send a test message.";
        _statusText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _statusText.fontSize  = 13;
        _statusText.color     = new Color(0.85f, 0.85f, 0.85f, 1f);
        _statusText.alignment = TextAnchor.UpperLeft;
        _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _statusText.verticalOverflow   = VerticalWrapMode.Overflow;
    }

    private void Update()
    {
        // Quick hotkey to re-show the panel if closed
        if (Input.GetKeyDown(KeyCode.G) && Input.GetKey(KeyCode.LeftShift))
        {
            ToggleVisibility();
            return;
        }

        // Test Claude API connection with a single keypress (dev convenience)
        if (Input.GetKeyDown(KeyCode.G) && !Input.GetKey(KeyCode.LeftShift))
        {
            SendTestMessage();
        }
    }

    /// <summary>
    /// Fires a test message to Claude and displays the response in the panel.
    /// Remove or replace once real FM data is being fed in.
    /// </summary>
    private async void SendTestMessage()
    {
        if (_statusText == null) return;
        _statusText.text = "Sending test message to Claude…";

        try
        {
            var response = await ClaudeClient.Instance.SendMessageAsync(
                "You are Gaffer, a football manager assistant. " +
                "Say hello and confirm you are ready to analyse Football Manager 2026.");

            // We're back on the Unity main thread because ClaudeClient posts back
            // via the main-thread dispatcher. Safe to touch UI here.
            _statusText.text = response;
        }
        catch (System.Exception ex)
        {
            _statusText.text = $"Claude API error:\n{ex.Message}";
            Plugin.Log.LogError($"Gaffer: Claude test failed — {ex}");
        }
    }

    public void Show()
    {
        _visible = true;
        _panel?.SetActive(true);
    }

    public void Hide()
    {
        _visible = false;
        _panel?.SetActive(false);
    }

    public void ToggleVisibility()
    {
        if (_visible) Hide(); else Show();
    }

    /// <summary>
    /// Update the text body from outside (e.g. DataReader events, proactive alerts).
    /// Always call from the Unity main thread.
    /// </summary>
    public void SetStatusText(string text)
    {
        if (_statusText != null)
            _statusText.text = text;
    }
}
