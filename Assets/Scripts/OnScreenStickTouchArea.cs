using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

// Expands an OnScreenStick's pointer target without moving its visual handle.
// The large outer ring receives the pointer and forwards the complete gesture
// to the smaller handle, where OnScreenStick keeps its normal movement logic.
public sealed class OnScreenStickTouchArea : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private OnScreenStick target;

    public void Initialize(OnScreenStick stick)
    {
        target = stick;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        target?.OnPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        target?.OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        target?.OnPointerUp(eventData);
    }
}
