using System;
using System.Linq;
using System.Text.Json;
using BepInEx.Configuration;
using Gaffer.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Gaffer.UI;

/// <summary>Owns all Unity uGUI rendering and interactions for the Gaffer panel.</summary>
public class GafferUI : MonoBehaviour
{
    private ConfigEntry<string>? _toggleShortcut;
    private DataReader? _dataReader;
    private ClaudeClient? _claudeClient;
    private AlertEngine? _alertEngine;
    private AttributeIdentityAnalyzer? _attributeIdentityAnalyzer;

    private GameObject? _window;
    private Text? _outputText;
    private bool _isVisible = true;

    public GafferUI(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>Injects required collaborators after component creation.</summary>
    public void Initialise(ConfigEntry<string> toggleShortcut, DataReader dataReader, ClaudeClient claudeClient, AlertEngine alertEngine, AttributeIdentityAnalyzer attributeIdentityAnalyzer)
    {
        _toggleShortcut = toggleShortcut;
        _dataReader = dataReader;
        _claudeClient = claudeClient;
        _alertEngine = alertEngine;
        _attributeIdentityAnalyzer = attributeIdentityAnalyzer;
    }

    private void Start()
    {
        BuildCanvas();
    }

    private void Update()
    {
        if (IsTogglePressed())
        {
            ToggleVisibility();
        }

        if (_dataReader is null || _alertEngine is null || _outputText is null)
        {
            return;
        }

        var snapshot = _dataReader.ReadCurrentState();
        var alerts = _alertEngine.Evaluate(snapshot);
        if (alerts.Count > 0)
        {
            _outputText.text = $"[{alerts[0].Title}] {alerts[0].Body}";
        }
    }

    private bool IsTogglePressed()
    {
        if (_toggleShortcut is null || string.IsNullOrWhiteSpace(_toggleShortcut.Value))
        {
            return Input.GetKeyDown(KeyCode.F8);
        }

        return Enum.TryParse(_toggleShortcut.Value, true, out KeyCode keyCode)
            ? Input.GetKeyDown(keyCode)
            : Input.GetKeyDown(KeyCode.F8);
    }

    private void BuildCanvas()
    {
        var canvasObject = new GameObject("GafferCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15000;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _window = BuildWindow(canvasObject.transform);
    }

    private GameObject BuildWindow(Transform parent)
    {
        var panel = new GameObject("GafferPanel");
        panel.transform.SetParent(parent, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(460f, 340f);

        var panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.12f, 0.14f, 0.95f);

        BuildTitleBar(panel.transform, rect);
        BuildBody(panel.transform);

        return panel;
    }

    private void BuildTitleBar(Transform parent, RectTransform windowRect)
    {
        var titleBar = new GameObject("TitleBar");
        titleBar.transform.SetParent(parent, false);

        var titleRect = titleBar.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 36f);

        var titleImage = titleBar.AddComponent<Image>();
        titleImage.color = new Color(0.13f, 0.17f, 0.2f, 1f);

        var dragHandler = titleBar.AddComponent<DragHandler>();
        dragHandler.Initialise(windowRect);

        var titleText = CreateText("Title", titleBar.transform, "Gaffer", 16, FontStyle.Bold);
        var titleTextRect = titleText.GetComponent<RectTransform>();
        if (titleTextRect != null)
        {
            titleTextRect.anchorMin = new Vector2(0f, 0f);
            titleTextRect.anchorMax = new Vector2(1f, 1f);
            titleTextRect.offsetMin = new Vector2(12f, 0f);
            titleTextRect.offsetMax = new Vector2(-50f, 0f);
        }

        var closeButton = CreateButton("CloseButton", titleBar.transform, "X", new Vector2(40f, 28f));
        var closeRect = closeButton.GetComponent<RectTransform>();
        if (closeRect != null)
        {
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.anchoredPosition = new Vector2(-6f, 0f);
        }

        closeButton.onClick.AddListener(new Action(Hide));
    }

    private void BuildBody(Transform parent)
    {
        var body = new GameObject("Body");
        body.transform.SetParent(parent, false);

        var bodyRect = body.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(12f, 12f);
        bodyRect.offsetMax = new Vector2(-12f, -46f);

        var testButton = CreateButton("TestConnectionButton", body.transform, "Test Connection", new Vector2(160f, 34f));
        var testRect = testButton.GetComponent<RectTransform>();
        if (testRect != null)
        {
            testRect.anchorMin = new Vector2(0f, 1f);
            testRect.anchorMax = new Vector2(0f, 1f);
            testRect.pivot = new Vector2(0f, 1f);
            testRect.anchoredPosition = new Vector2(0f, 0f);
        }

        testButton.onClick.AddListener(new Action(OnTestConnectionClicked));

        var identityButton = CreateButton("AttributeIdentityButton", body.transform, "Attribute Identity", new Vector2(160f, 34f));
        var identityRect = identityButton.GetComponent<RectTransform>();
        if (identityRect != null)
        {
            identityRect.anchorMin = new Vector2(0f, 1f);
            identityRect.anchorMax = new Vector2(0f, 1f);
            identityRect.pivot = new Vector2(0f, 1f);
            identityRect.anchoredPosition = new Vector2(172f, 0f);
        }

        identityButton.onClick.AddListener(new Action(OnAttributeIdentityClicked));

        var output = CreateText("Output", body.transform, "Ready. Press Test Connection or Attribute Identity.", 13, FontStyle.Normal);
        _outputText = output;
        _outputText.alignment = TextAnchor.UpperLeft;
        _outputText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _outputText.verticalOverflow = VerticalWrapMode.Overflow;

        var outputRect = output.GetComponent<RectTransform>();
        if (outputRect != null)
        {
            outputRect.anchorMin = new Vector2(0f, 0f);
            outputRect.anchorMax = new Vector2(1f, 1f);
            outputRect.offsetMin = new Vector2(0f, 0f);
            outputRect.offsetMax = new Vector2(0f, -46f);
        }
    }

    private void OnAttributeIdentityClicked()
    {
        if (_outputText is null || _dataReader is null || _attributeIdentityAnalyzer is null)
        {
            return;
        }

        var snapshot = _dataReader.ReadCurrentState();
        var analysis = _attributeIdentityAnalyzer.Analyze(snapshot);

        if (analysis.Count == 0)
        {
            _outputText.text = "No player attribute data found yet. Populate DataReader attribute extraction first.";
            return;
        }

        var lines = analysis
            .Take(5)
            .Select(result => $"{result.PlayerName} ({result.RegisteredPosition}) -> {result.SuggestedPrimaryRole}; Misaligned: {result.IsMisaligned}");

        _outputText.text = string.Join("\n", lines);
    }

    private async void OnTestConnectionClicked()
    {
        if (_outputText is null || _dataReader is null || _claudeClient is null)
        {
            return;
        }

        _outputText.text = "Connecting to Claude...";

        try
        {
            GameStateSnapshot snapshot = _dataReader.ReadCurrentState();
            string json = JsonSerializer.Serialize(snapshot);
            string response = await _claudeClient.SendAnalysisAsync(json, "This is a pipeline test. Confirm connectivity and summarize one tactical priority.");

            // IL2CPP async work continues off-thread; always marshal UI mutations back to Unity main thread.
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_outputText != null)
                {
                    _outputText.text = response;
                }
            });
        }
        catch (Exception ex)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                if (_outputText != null)
                {
                    _outputText.text = $"Test failed: {ex.Message}";
                }
            });
        }
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle style)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);

        var rectTransform = gameObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = Vector2.zero;

        var text = gameObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = new Color(0.85f, 0.88f, 0.9f, 1f);

        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 size)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);

        var rect = gameObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;

        var image = gameObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.24f, 0.28f, 1f);

        var button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelText = CreateText("Label", gameObject.transform, label, 12, FontStyle.Bold);
        labelText.alignment = TextAnchor.MiddleCenter;
        var labelRect = labelText.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        return button;
    }

    private void ToggleVisibility()
    {
        if (_isVisible)
        {
            Hide();
            return;
        }

        Show();
    }

    private void Show()
    {
        _isVisible = true;
        _window?.SetActive(true);
    }

    private void Hide()
    {
        _isVisible = false;
        _window?.SetActive(false);
    }
}
