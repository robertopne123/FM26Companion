using System;
using System.Text.Json;
using System.Threading.Tasks;
using BepInEx.Logging;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gaffer
{
    /// <summary>
    /// MonoBehaviour that owns the entire Gaffer UI panel.
    /// Handles: Canvas/panel construction, keyboard toggling, title-bar drag,
    /// "Test Connection" button, and rendering Claude API responses.
    /// All async callbacks are marshalled back to this thread via MainThreadDispatcher.
    /// </summary>
    public class GafferBehaviour : MonoBehaviour
    {
        // IL2CPP PATTERN: Every MonoBehaviour subclass injected via ClassInjector
        // must declare this constructor. IL2CPP manages object lifetime natively;
        // the IntPtr is the underlying Il2Cpp object pointer. Without this ctor,
        // ClassInjector.RegisterTypeInIl2Cpp<GafferBehaviour>() will throw at runtime.
        public GafferBehaviour(IntPtr ptr) : base(ptr) { }

        private Canvas         _canvas      = null!;
        private RectTransform  _panelRect   = null!;
        private RectTransform  _titleBarRect = null!;
        private Text           _responseText = null!;
        private Text           _statusLabel  = null!;
        private Button         _testButton   = null!;
        private bool           _panelVisible = true;
        private bool           _isDragging;
        private Vector2        _dragOffset;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            try
            {
                CreateCanvas();
                CreatePanel();
                Plugin.Log.LogInfo("Gaffer panel created");
            }
            catch (Exception ex)
            {
                // Never crash FM26 — log and bail
                Plugin.Log.LogError($"[GafferUI] Panel creation failed: {ex}");
            }
        }

        private void Update()
        {
            // Drain the main-thread dispatch queue (populated by async Claude calls)
            MainThreadDispatcher.Flush();

            HandleToggleKey();
            HandleDrag();
        }

        // ── Keyboard toggle ───────────────────────────────────────────────────────

        private void HandleToggleKey()
        {
            if (!Enum.TryParse<KeyCode>(Plugin.ToggleKey.Value, out var key))
                return;
            if (Input.GetKeyDown(key))
                TogglePanel();
        }

        private void TogglePanel()
        {
            if (_panelRect == null) return;
            _panelVisible = !_panelVisible;
            _panelRect.gameObject.SetActive(_panelVisible);
        }

        // ── Canvas & panel construction ───────────────────────────────────────────

        private void CreateCanvas()
        {
            var canvasObj = new GameObject(Constants.CanvasObjectName);
            DontDestroyOnLoad(canvasObj);

            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Sort order 999: guarantees Gaffer renders above FM26's native UI stack
            _canvas.sortingOrder = 999;

            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // IL2CPP PATTERN: FindObjectOfType<T>() is bridged through Il2CppInterop's
            // reflection layer. FM26 almost certainly has its own EventSystem; only create
            // one if absent to avoid duplicate input processing.
            if (FindObjectOfType<EventSystem>() == null)
            {
                var esObj = new GameObject(Constants.EventSystemObjectName);
                DontDestroyOnLoad(esObj);
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<StandaloneInputModule>();
            }
        }

        private void CreatePanel()
        {
            var panelObj = new GameObject("GafferPanel");
            panelObj.transform.SetParent(_canvas.transform, false);

            var bg = panelObj.AddComponent<Image>();
            bg.color = Constants.PanelBackground;

            _panelRect = panelObj.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(0f, 1f);
            _panelRect.anchorMax = new Vector2(0f, 1f);
            _panelRect.pivot     = new Vector2(0f, 1f);
            _panelRect.sizeDelta = new Vector2(Constants.PanelWidth, Constants.PanelHeight);
            _panelRect.anchoredPosition = new Vector2(Constants.PanelInitialX, Constants.PanelInitialY);

            CreateTitleBar(panelObj);
            CreateResponseArea(panelObj);
            CreateFooter(panelObj);
        }

        private void CreateTitleBar(GameObject parent)
        {
            var titleBarObj = new GameObject("TitleBar");
            titleBarObj.transform.SetParent(parent.transform, false);

            var bg = titleBarObj.AddComponent<Image>();
            bg.color = Constants.TitleBarBackground;

            _titleBarRect = titleBarObj.GetComponent<RectTransform>();
            _titleBarRect.anchorMin = new Vector2(0f, 1f);
            _titleBarRect.anchorMax = new Vector2(1f, 1f);
            _titleBarRect.pivot     = new Vector2(0.5f, 1f);
            _titleBarRect.sizeDelta = new Vector2(0f, Constants.TitleBarHeight);
            _titleBarRect.anchoredPosition = Vector2.zero;

            // Plugin name
            var titleObj  = MakeText("TitleText", titleBarObj, "Gaffer", 14, TextAnchor.MiddleLeft);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = new Vector2(12f, 0f);
            titleRect.offsetMax = new Vector2(-40f, 0f);

            // Close / hide button
            var closeObj = MakeButton("CloseButton", titleBarObj, "×", 16);
            var closeRect = closeObj.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot     = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(30f, 30f);
            closeRect.anchoredPosition = new Vector2(-5f, 0f);

            // IL2CPP PATTERN: Button.onClick.AddListener requires an explicit cast to
            // UnityAction (a delegate type defined in the IL2CPP runtime). With
            // Il2CppInterop 1.5+, a plain lambda wrapped in the cast works correctly.
            closeObj.GetComponent<Button>().onClick.AddListener(
                (UnityEngine.Events.UnityAction)TogglePanel);
        }

        private void CreateResponseArea(GameObject parent)
        {
            var scrollObj = new GameObject("ResponseScroll");
            scrollObj.transform.SetParent(parent.transform, false);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal        = false;
            scrollRect.vertical          = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType      = ScrollRect.MovementType.Clamped;

            var scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0f, Constants.FooterHeight);
            scrollRt.offsetMax = new Vector2(0f, -Constants.TitleBarHeight);

            // Viewport — clips content to the visible area
            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);

            var vpImg = viewportObj.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0f); // invisible mask surface

            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;
            viewportRt.anchoredPosition = Vector2.zero;

            // Content container — grows downward as text is appended
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);

            var layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.padding                = new RectOffset(10, 10, 8, 8);
            layout.childControlWidth      = true;
            layout.childControlHeight     = true;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            var sizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            // Response text element inside the content container
            var textObj = MakeText("ResponseText", contentObj,
                "Gaffer is ready.\nPress \"Test Connection\" to verify the Claude API pipeline.\n" +
                "Once confirmed, DataReader will be wired up for live FM26 data.",
                12, TextAnchor.UpperLeft);

            var textEl = textObj.AddComponent<LayoutElement>();
            textEl.flexibleWidth = 1f;

            var textSizer = textObj.AddComponent<ContentSizeFitter>();
            textSizer.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _responseText       = textObj.GetComponent<Text>();
            _responseText.color = Constants.TextColor;

            scrollRect.content  = contentRt;
            scrollRect.viewport = viewportRt;

            // Start scrolled to top
            scrollRect.normalizedPosition = new Vector2(0f, 1f);
        }

        private void CreateFooter(GameObject parent)
        {
            var footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(parent.transform, false);

            var bg = footerObj.AddComponent<Image>();
            bg.color = Constants.FooterBackground;

            var footerRt = footerObj.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot     = new Vector2(0.5f, 0f);
            footerRt.sizeDelta = new Vector2(0f, Constants.FooterHeight);
            footerRt.anchoredPosition = Vector2.zero;

            // Status label (left side)
            var statusObj = MakeText("StatusLabel", footerObj, "Ready", 11, TextAnchor.MiddleLeft);
            _statusLabel       = statusObj.GetComponent<Text>();
            _statusLabel.color = Constants.SubtleTextColor;

            var statusRt = statusObj.GetComponent<RectTransform>();
            statusRt.anchorMin = Vector2.zero;
            statusRt.anchorMax = Vector2.one;
            statusRt.offsetMin = new Vector2(10f, 0f);
            statusRt.offsetMax = new Vector2(-142f, 0f);

            // Test Connection button (right side)
            var btnObj = MakeButton("TestButton", footerObj, "Test Connection", 11);
            _testButton = btnObj.GetComponent<Button>();

            var btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1f, 0.5f);
            btnRt.anchorMax = new Vector2(1f, 0.5f);
            btnRt.pivot     = new Vector2(1f, 0.5f);
            btnRt.sizeDelta = new Vector2(134f, 28f);
            btnRt.anchoredPosition = new Vector2(-8f, 0f);

            _testButton.onClick.AddListener(
                (UnityEngine.Events.UnityAction)OnTestConnectionClicked);
        }

        // ── UI factory helpers ────────────────────────────────────────────────────
        // These belong in GafferUI because they build Gaffer-specific elements only.

        private static GameObject MakeText(string name, GameObject parent,
            string content, int fontSize, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            var t = obj.AddComponent<Text>();
            t.text = content;
            // IL2CPP PATTERN: Resources.GetBuiltinResource<T> works normally through
            // Il2CppInterop — Font is a concrete IL2CPP type, no generic issues.
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize          = fontSize;
            t.alignment         = alignment;
            t.color             = Constants.TextColor;
            t.supportRichText   = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;

            return obj;
        }

        private static GameObject MakeButton(string name, GameObject parent,
            string label, int fontSize)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            obj.AddComponent<Image>().color = Constants.ButtonBackground;
            obj.AddComponent<Button>();

            var labelObj = MakeText(name + "Label", obj, label, fontSize, TextAnchor.MiddleCenter);
            var labelRt  = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            return obj;
        }

        // ── Drag ─────────────────────────────────────────────────────────────────

        private void HandleDrag()
        {
            if (_panelRect == null || _titleBarRect == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                // IL2CPP PATTERN: RectTransformUtility.ScreenPointToLocalPointInRectangle
                // is bridged through Il2CppInterop. Pass null camera for ScreenSpaceOverlay
                // — overlay canvases don't use a camera for coordinate conversion.
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        _titleBarRect,
                        Input.mousePosition,
                        null,
                        out var localPoint)
                    && _titleBarRect.rect.Contains(localPoint))
                {
                    _isDragging  = true;
                    _dragOffset  = (Vector2)_panelRect.position - (Vector2)Input.mousePosition;
                }
            }

            if (Input.GetMouseButtonUp(0))
                _isDragging = false;

            if (_isDragging)
                _panelRect.position = (Vector2)Input.mousePosition + _dragOffset;
        }

        // ── Claude pipeline ───────────────────────────────────────────────────────

        private void OnTestConnectionClicked()
        {
            SetStatus("Connecting…");
            _testButton.interactable = false;

            var apiKey = Plugin.ApiKey.Value;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                AppendResponse(
                    "\n⚠  No API key set. Edit BepInEx/config/Gaffer.cfg → [Claude] ApiKey = sk-ant-…");
                SetStatus("No API key");
                _testButton.interactable = true;
                return;
            }

            // DataReader returns empty stub models in Phase 1 — still valid JSON context
            var state      = DataReader.GetCurrentGameState();
            var systemPrompt = BuildSystemPrompt(state);

            // Fire-and-forget; result is marshalled back to main thread in the lambda
            _ = SendTestAsync(apiKey, systemPrompt);
        }

        private static string BuildSystemPrompt(Models.GameState state)
        {
            var json = JsonSerializer.Serialize(state,
                new JsonSerializerOptions { WriteIndented = false });
            return $"{Constants.ClaudeSystemPromptBase}\n\nCurrent game state:\n{json}";
        }

        private async Task SendTestAsync(string apiKey, string systemPrompt)
        {
            try
            {
                var response = await ClaudeClient.SendAsync(
                    apiKey,
                    systemPrompt,
                    "This is a test connection from the Gaffer FM26 plugin. " +
                    "Reply with one sentence confirming you are connected and ready " +
                    "to provide tactical analysis for Football Manager 2026.");

                // IL2CPP PATTERN: async continuations run on a ThreadPool thread.
                // All Unity API writes (Text.text, Button.interactable, etc.) must
                // be dispatched to the main thread or Unity will throw.
                MainThreadDispatcher.Enqueue(() =>
                {
                    AppendResponse($"\n\nClaude: {response}");
                    SetStatus("Connected ✓");
                    _testButton.interactable = true;
                });
            }
            catch (Exception ex)
            {
                MainThreadDispatcher.Enqueue(() =>
                {
                    AppendResponse($"\n\n⚠  Connection error: {ex.Message}");
                    SetStatus("Error");
                    _testButton.interactable = true;
                });
            }
        }

        // ── Public surface for AlertEngine ────────────────────────────────────────

        /// <summary>Displays a proactive alert generated by AlertEngine.</summary>
        public void ShowAlert(string message)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                AppendResponse($"\n\n⚡ {message}");
                SetStatus("Alert");
            });
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private void AppendResponse(string text)
        {
            if (_responseText != null)
                _responseText.text += text;
        }

        private void SetStatus(string status)
        {
            if (_statusLabel != null)
                _statusLabel.text = status;
        }
    }
}
