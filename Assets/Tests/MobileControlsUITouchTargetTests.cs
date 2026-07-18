using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

public class MobileControlsUITouchTargetTests : InputTestFixture
{
    private GameObject parent;

    [TearDown]
    public void TearDownObjects()
    {
        if (parent != null)
            Object.DestroyImmediate(parent);
    }

    [Test]
    public void BuildStick_MakesWholeRingInteractiveAndIsolatesPointerInput()
    {
        parent = new GameObject("Canvas", typeof(RectTransform));
        MethodInfo buildStick = typeof(MobileControlsUI).GetMethod(
            "BuildStick", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(buildStick, Is.Not.Null);
        object[] arguments =
        {
            parent.transform,
            "StickArea",
            null,
            Vector2.zero,
            Vector2.zero,
            "<Gamepad>/leftStick",
            null
        };

        var root = (GameObject)buildStick.Invoke(null, arguments);
        var stick = (OnScreenStick)arguments[6];
        var handleImage = root.transform.Find("StickHandle").GetComponent<Image>();

        Assert.That(root.GetComponent<OnScreenStickTouchArea>(), Is.Not.Null,
            "The visible outer ring should own a gesture-forwarding touch target.");
        Assert.That(handleImage.raycastTarget, Is.False,
            "The center knob must not prevent the larger ring from receiving touches.");
        Assert.That(stick.useIsolatedInputActions, Is.True,
            "Device switching must not cancel an active iOS stick drag.");
    }
}
