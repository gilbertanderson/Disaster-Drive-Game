using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Coroutine input helpers for Play Mode tests: gamepad and touch siblings of
// RubricE2ETests' keyboard HoldKey. Device events are queued only
// (queueEventOnly: true) so the player loop's own input update processes them,
// matching how real hardware input reaches the game.
internal static class InputSimulationHelpers
{
    // Deflects a stick, holds it for a realtime duration, then recenters it.
    public static IEnumerator HoldStick(InputTestFixture input, StickControl stick, Vector2 direction, float seconds)
    {
        input.Set(stick, direction, queueEventOnly: true);

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        input.Set(stick, Vector2.zero, queueEventOnly: true);
        yield return null;
    }

    // Holds a button (e.g. a d-pad direction) for a realtime duration.
    public static IEnumerator HoldButton(InputTestFixture input, ButtonControl button, float seconds)
    {
        input.Press(button, queueEventOnly: true);

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        input.Release(button, queueEventOnly: true);
        yield return null;
    }

    // A quick touch tap: enough to flip InputModeWatcher into Touch mode.
    public static IEnumerator Tap(InputTestFixture input, Vector2 position, int touchId = 1)
    {
        input.BeginTouch(touchId, position, queueEventOnly: true);
        yield return null;
        yield return null;
        input.EndTouch(touchId, position, queueEventOnly: true);
        yield return null;
    }

    // Drives an OnScreenStick through the same EventSystem callbacks a real
    // touch drag produces: pointer down on the handle, one drag past the
    // stick's movement range, a realtime hold, then release. The controls live
    // on a screen-space-overlay canvas, so UI positions are already screen
    // pixels and no event camera is needed.
    public static IEnumerator DragOnScreenStick(GameObject stickHandle, Vector2 screenDelta, float seconds)
    {
        Vector2 start = stickHandle.transform.position;
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = start,
            pressPosition = start
        };
        ExecuteEvents.Execute(stickHandle, eventData, ExecuteEvents.pointerDownHandler);

        eventData.position = start + screenDelta;
        ExecuteEvents.Execute(stickHandle, eventData, ExecuteEvents.dragHandler);

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;

        ExecuteEvents.Execute(stickHandle, eventData, ExecuteEvents.pointerUpHandler);
        yield return null;
    }
}
