using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Covers the binding matrix BuildMovementAction assembles per control scheme:
// which keyboard sets are live and which gamepad stick drives movement.
// 1P: left stick + d-pad. 2P: left stick → P1, right stick → P2 (same split as
// the on-screen touch sticks). Start never runs in Edit Mode, but
// ApplyControlScheme is self-contained and enables the action.
public class PlayerControllerControlSchemeTests : InputTestFixture
{
    private GameObject playerObject;
    private PlayerController player;
    private Gamepad gamepad;
    private Keyboard keyboard;

    [SetUp]
    public void SetUpPlayer()
    {
        playerObject = new GameObject("Player");
        player = playerObject.AddComponent<PlayerController>();
        gamepad = InputSystem.AddDevice<Gamepad>();
        keyboard = InputSystem.AddDevice<Keyboard>();
    }

    [TearDown]
    public void TearDownPlayer()
    {
        player.movementAction?.Disable();
        player.movementAction?.Dispose();
        Object.DestroyImmediate(playerObject);
    }

    private Vector2 ReadMovement()
    {
        return player.movementAction.ReadValue<Vector2>();
    }

    // Stick values pass through the default deadzone/normalization processors,
    // so full deflection is asserted as > 0.9 rather than exactly 1.

    [Test]
    public void WasdAndArrows_LeftStick_ProducesMovement()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.WasdAndArrows);

        Set(gamepad.leftStick, Vector2.right);

        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f),
            "Single-player scheme should accept gamepad left-stick input.");
    }

    [Test]
    public void WasdAndArrows_Dpad_ProducesMovement()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.WasdAndArrows);

        Press(gamepad.dpad.up);

        Assert.That(ReadMovement().y, Is.GreaterThan(0.9f),
            "Single-player scheme should accept gamepad d-pad input.");
    }

    [Test]
    public void WasdAndArrows_BothKeyboardSetsWork()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.WasdAndArrows);

        Press(keyboard.dKey);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "WASD should steer in the single-player scheme.");
        Release(keyboard.dKey);

        Press(keyboard.leftArrowKey);
        Assert.That(ReadMovement().x, Is.LessThan(-0.9f), "Arrow keys should steer in the single-player scheme.");
    }

    [Test]
    public void ArrowsAndRightStick_RightStickAndArrowsWork_WasdAndLeftStickIgnored()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.ArrowsAndRightStick);

        Press(keyboard.wKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f),
            "WASD belongs to Player 1 in two-player mode and must not drive Player 2.");
        Release(keyboard.wKey);

        Press(keyboard.upArrowKey);
        Assert.That(ReadMovement().y, Is.GreaterThan(0.9f), "Arrow keys should steer Player 2.");
        Release(keyboard.upArrowKey);

        Set(gamepad.leftStick, Vector2.left);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f),
            "The left stick belongs to Player 1 and must not drive Player 2.");
        Set(gamepad.leftStick, Vector2.zero);

        Set(gamepad.rightStick, Vector2.left);
        Assert.That(ReadMovement().x, Is.LessThan(-0.9f), "The right stick should steer Player 2.");
        Set(gamepad.rightStick, Vector2.zero);
    }

    [Test]
    public void WasdAndLeftStick_LeftStickWorks_ArrowsAndRightStickIgnored()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.WasdAndLeftStick);

        Set(gamepad.rightStick, Vector2.right);
        Press(keyboard.rightArrowKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f),
            "Arrows and the right stick belong to Player 2 and must not drive Player 1.");
        Release(keyboard.rightArrowKey);
        Set(gamepad.rightStick, Vector2.zero);

        Set(gamepad.leftStick, Vector2.right);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f),
            "The left stick should steer Player 1 in two-player mode.");
        Set(gamepad.leftStick, Vector2.zero);

        Press(keyboard.dKey);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "WASD should still steer Player 1.");
    }

    [Test]
    public void ArrowsOnly_IgnoresGamepadAndWasd()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.ArrowsOnly);

        Set(gamepad.leftStick, Vector2.right);
        Set(gamepad.rightStick, Vector2.right);
        Press(keyboard.dKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f));

        Press(keyboard.rightArrowKey);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "Arrow keys should still steer.");
    }
}
