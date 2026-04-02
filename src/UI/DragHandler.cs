using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gaffer.UI;

/// <summary>Handles panel dragging by moving the target window RectTransform.</summary>
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    private RectTransform? _target;
    private Vector2 _offset;

    public DragHandler(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>Sets the RectTransform that should move when this drag handle is dragged.</summary>
    public void Initialise(RectTransform target)
    {
        _target = target;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_target == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _target.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint);

        _offset = _target.anchoredPosition - localPoint;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_target == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _target.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint);

        _target.anchoredPosition = localPoint + _offset;
    }
}
