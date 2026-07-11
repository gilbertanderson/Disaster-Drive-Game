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
    // Waits a realtime duration, frame by frame, so it keeps advancing while
    // the game is paused (Time.timeScale = 0).
    public static IEnumerator WaitRealtime(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return null;
    }

    // Waits until the condition holds or the timeout elapses; the caller
    // asserts on the condition afterwards.
    public static IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    // Deflects a stick, holds it for a realtime duration, then recenters it.
    public static IEnumerator HoldStick(InputTestFixture input, StickControl stick, Vector2 direction, float seconds)
    {
        input.Set(stick, direction, queueEventOnly: true);
        yield return WaitRealtime(seconds);
        input.Set(stick, Vector2.zero, queueEventOnly: true);
        yield return null;
    }

    // Holds a button (e.g. a d-pad direction) for a realtime duration.
    public static IEnumerator HoldButton(InputTestFixture input, ButtonControl button, float seconds)
    {
        input.Press(button, queueEventOnly: true);
        yield return WaitRealtime(seconds);
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

        yield return WaitRealtime(seconds);

        ExecuteEvents.Execute(stickHandle, eventData, ExecuteEvents.pointerUpHandler);
        yield return null;
    }
}
