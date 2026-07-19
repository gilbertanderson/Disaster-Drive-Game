using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

// A fixed-origin virtual joystick where the touch hit-test area (this
// component's own RectTransform — the large outer ring, sized so a finger
// landing anywhere on the pad starts a drag) is decoupled from the visual
// element that actually slides (a separate handle RectTransform assigned via
// `handle`). Unity's stock OnScreenStick always moves whatever RectTransform
// it is attached to, so attaching it to the big ring drags the ring itself
// along with the handle. This component reimplements the same fixed-origin
// drag using plain uGUI pointer events instead, so the ring stays put and
// only the handle translates.
public class DisasterOnScreenStick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private string controlPathBacking;
    public RectTransform handle;
    public float movementRange = 50f;

    protected override string controlPathInternal
    {
        get => controlPathBacking;
        set => controlPathBacking = value;
    }

    private RectTransform areaRect;
    private Vector2 handleRestPosition;
    private int activePointerId = int.MinValue;
    private Vector2 pointerDownLocalPos;

    private void Awake()
    {
        areaRect = transform as RectTransform;
        if (handle != null)
            handleRestPosition = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (activePointerId != int.MinValue)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                areaRect, eventData.position, eventData.pressEventCamera, out pointerDownLocalPos))
            return;
        activePointerId = eventData.pointerId;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId || handle == null)
            return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                areaRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
            return;

        Vector2 delta = Vector2.ClampMagnitude(localPos - pointerDownLocalPos, movementRange);
        handle.anchoredPosition = handleRestPosition + delta;
        SendValueToControl(new Vector2(delta.x / movementRange, delta.y / movementRange));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
            return;
        activePointerId = int.MinValue;
        if (handle != null)
            handle.anchoredPosition = handleRestPosition;
        SendValueToControl(Vector2.zero);
    }

    private void OnDisable()
    {
        activePointerId = int.MinValue;
        if (handle != null)
            handle.anchoredPosition = handleRestPosition;
    }
}
