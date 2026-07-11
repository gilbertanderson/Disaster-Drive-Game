using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

// Covers the binding matrix BuildMovementAction assembles per control scheme:
// which keyboard sets are live and whether the gamepad (left stick + d-pad,
// also fed by the on-screen touch stick) drives movement. Start never runs in
// Edit Mode, but ApplyControlScheme is self-contained and enables the action.
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
    public void ArrowsAndGamepad_StickDpadAndArrowsWork_WasdIgnored()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.ArrowsAndGamepad);

        Press(keyboard.wKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f),
            "WASD belongs to Player 1 in two-player mode and must not drive Player 2.");
        Release(keyboard.wKey);

        Press(keyboard.upArrowKey);
        Assert.That(ReadMovement().y, Is.GreaterThan(0.9f), "Arrow keys should steer Player 2.");
        Release(keyboard.upArrowKey);

        Set(gamepad.leftStick, Vector2.left);
        Assert.That(ReadMovement().x, Is.LessThan(-0.9f), "The left stick should steer Player 2.");
        Set(gamepad.leftStick, Vector2.zero);

        Press(gamepad.dpad.right);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "The d-pad should steer Player 2.");
    }

    [Test]
    public void WasdOnly_IgnoresGamepadAndArrows()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.WasdOnly);

        Set(gamepad.leftStick, Vector2.right);
        Press(gamepad.dpad.right);
        Press(keyboard.rightArrowKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f),
            "The pad belongs to Player 2 in two-player mode and must not drive Player 1.");

        Press(keyboard.dKey);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "WASD should still steer Player 1.");
    }

    [Test]
    public void ArrowsOnly_IgnoresGamepadAndWasd()
    {
        player.ApplyControlScheme(PlayerController.ControlScheme.ArrowsOnly);

        Set(gamepad.leftStick, Vector2.right);
        Press(keyboard.dKey);
        Assert.That(ReadMovement().magnitude, Is.LessThan(0.01f));

        Press(keyboard.rightArrowKey);
        Assert.That(ReadMovement().x, Is.GreaterThan(0.9f), "Arrow keys should still steer.");
    }
}
