using UnityEngine;
using UnityEngine.EventSystems;

namespace FM26Companion.UI;

/// <summary>
/// Allows the Gaffer panel to be repositioned by dragging its title bar.
/// Kept in its own class so GafferPanel stays focused on layout/content.
/// </summary>
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    // ── IL2CPP constructor requirement ────────────────────────────────────────
    // Required for any MonoBehaviour subclass registered via ClassInjector.
    // See Plugin.cs for the full explanation.
    public DragHandler(System.IntPtr ptr) : base(ptr) { }

    private RectTransform? _rectTransform;
    private Canvas? _canvas;
    private Vector2 _dragOffset;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        // Walk up to find the Canvas — needed for the reference pixel-per-unit
        // scaling used by RectTransformUtility.ScreenPointToLocalPointInRectangle.
        _canvas = GetComponentInParent<Canvas>();
    }

    // ── IL2CPP event interface note ───────────────────────────────────────────
    // IBeginDragHandler / IDragHandler are Unity EventSystem interfaces.
    // In standard Mono BepInEx these are plain C# interface implementations.
    // In IL2CPP, the Il2CppInterop layer generates proxy types for them so that
    // calling OnBeginDrag / OnDrag from the native Unity event system crosses
    // the IL2CPP→Mono boundary correctly. No extra work is needed here; just be
    // aware that if you see "method not found" errors at runtime it usually means
    // the interface proxy DLL wasn't generated (re-run the game to regenerate interop/).

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_rectTransform == null || _canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            _canvas.worldCamera,
            out var localPoint);

        _dragOffset = _rectTransform.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_rectTransform == null || _canvas == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform ?? _rectTransform,
            eventData.position,
            _canvas.worldCamera,
            out var localPoint);

        _rectTransform.anchoredPosition = localPoint + _dragOffset;
    }
}
