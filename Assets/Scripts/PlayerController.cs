using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Which keyboard keys steer this vehicle. Solo play keeps both sets live;
    // two-player mode gives WASD to P1 and the arrow keys to P2.
    public enum ControlScheme
    {
        WasdAndArrows,
        WasdOnly,
        ArrowsOnly,
        ArrowsAndGamepad
    }

    public int playerIndex;                   // 0 = Player 1, 1 = Player 2
    public float speed = 10f;                 // Movement speed in units per second
    private float baseSpeed = 10f;              // Unmodified speed before vehicle stat multipliers
    private float minX;                       // Left edge of the playable area
    private float maxX;                       // Right edge of the playable area
    private float minZ;                       // Bottom edge of the playable area
    private float maxZ;                       // Top edge of the playable area
    public InputAction movementAction;        // Input action bound to movement keys/stick
    public Camera gameCamera;                 // Camera used to derive the playable bounds
    public float boundaryPadding = 0.5f;      // Inset from the screen edges so the player stays fully visible
    public string wallsParentName = "Walls";  // Parent object whose children are the side walls
    public float wallPadding = 0.5f;          // Keeps the vehicle's body from clipping into a wall
    [SerializeField] private float exitSpeedMultiplier = 1.5f;
    [SerializeField] private float exitOffScreenMargin = 0.5f;
    [SerializeField] private float exitMinDuration = 0.75f;
    public float movementDeadzone = 0.25f;    // Ignore tiny input noise for dirt emitter direction
    [SerializeField] private float crashKnockbackSpeed = 5f;    // Shove applied when two vehicles collide
    [SerializeField] private float crashKnockbackDuration = 0.3f;  // Window where physics, not input, drives the vehicle

    private Rigidbody playerRb;               // Cached Rigidbody for physics-based movement
    private Collider playerCollider;          // Cached collider, used to keep the whole body inside the bounds
    private GameManager gameManager;          // Notified when the vehicle hits a rock
    private Vector2 movementInput;            // Latest input value read each frame
    private bool isExiting;
    private bool exitViaBottom;
    private float exitStartTime;
    private Vector3 exitDirection;
    private float knockbackUntil = float.NegativeInfinity;  // While Time.time is below this, the crash shove owns the velocity
    public Vector3 CurrentMovementDirection { get; private set; }
    public float SteerInput { get; private set; }    // Raw -1..1 horizontal axis, ungated/unnormalized, for wheel-steer visuals
    private float wallMinZ = float.NegativeInfinity;  // Inner face of the low-Z wall (unbounded until found)
    private float wallMaxZ = float.PositiveInfinity;  // Inner face of the high-Z wall

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();                    // Used to keep the whole vehicle body inside the bounds
        playerRb.useGravity = false;                                 // Top-down game: no gravity, movement is script-driven
        playerRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        playerRb.constraints = RigidbodyConstraints.FreezeRotation    // Keep the player upright
                             | RigidbodyConstraints.FreezePositionY;  // Lock to the ground plane (prevents jitter)
        baseSpeed = speed;
        movementAction.Enable();                                     // Begin listening for input

        if (gameCamera == null)
            gameCamera = Camera.main;                                // Fall back to the main camera if none assigned

        gameManager = FindAnyObjectByType<GameManager>();          // Reports rock hits so the timer takes a penalty

        FindWallBounds();                                            // Work out the Z limits from the side walls
        UpdateBounds();                                              // Calculate the initial playable area
    }
    // Update is called once per frame
    void Update()
    {
        movementInput = movementAction.ReadValue<Vector2>();  // Capture this frame's input
        UpdateBounds();                                       // Recompute bounds in case the camera moved
    }

    void FixedUpdate()
    {
        if (isExiting)
        {
            ExitDriveTick();
            return;
        }

        // Hold still on the start screen; movement only begins once the Start button is pressed.
        if (gameManager != null && !gameManager.IsGameActive)
        {
            CurrentMovementDirection = Vector3.zero;
            SteerInput = 0f;
            return;
        }

        if (gameCamera == null)
            ResolveGameCamera();
        if (gameCamera == null)
            return;

        // Just after a vehicle-vehicle crash, let physics (and the Bouncy material)
        // carry the vehicle so the impact reads as a real bounce. Input resumes once
        // the window closes; until then only keep the body inside the playable area.
        if (Time.time < knockbackUntil)
        {
            Vector3 pos = playerRb.position;
            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedZ = Mathf.Clamp(pos.z, minZ, maxZ);
            if (!Mathf.Approximately(pos.x, clampedX) || !Mathf.Approximately(pos.z, clampedZ))
            {
                playerRb.position = new Vector3(clampedX, pos.y, clampedZ);
                Vector3 velocity = playerRb.linearVelocity;
                if (!Mathf.Approximately(pos.x, clampedX)) velocity.x = 0f;
                if (!Mathf.Approximately(pos.z, clampedZ)) velocity.z = 0f;
                playerRb.linearVelocity = velocity;
            }
            return;
        }

        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;
        SteerInput = horizontalInput;

        Vector3 screenRight = gameCamera.transform.right;
        screenRight.y = 0f;
        screenRight.Normalize();
        Vector3 screenForward = ComputeScreenForward();

        // Build a normalized direction so diagonal movement isn't faster
        Vector3 movementDirection = (screenRight * horizontalInput + screenForward * verticalInput).normalized;
        CurrentMovementDirection = movementDirection.magnitude > movementDeadzone ? movementDirection : Vector3.zero;
        Vector3 movement = movementDirection * speed * Time.fixedDeltaTime;

        // Apply movement, then clamp the result inside the playable area
        Vector3 targetPosition = playerRb.position + movement;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);

        // Clear any velocity from rock impacts so collisions can't shove the vehicle out
        // of bounds; the script's MovePosition is the only thing that moves it.
        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.MovePosition(targetPosition);     // Move via physics so collisions are still respected
    }

    public void BeginExitDrive(bool exitViaBottom = false)
    {
        isExiting = true;
        SteerInput = 0f;   // Straighten the wheels; FixedUpdate no longer refreshes this once exiting
        this.exitViaBottom = exitViaBottom;
        exitStartTime = Time.time;
        ResolveGameCamera();
        exitDirection = exitViaBottom ? -ComputeScreenForward() : ComputeScreenForward();
    }

    // Rebinds movement to the given key set. Called by GameManager when the
    // player-count mode changes; replaces the serialized action so the prefab's
    // authored bindings (both key sets) stay the single-player default.
    public void ApplyControlScheme(ControlScheme scheme)
    {
        movementAction?.Disable();
        movementAction = BuildMovementAction(scheme);
        movementAction.Enable();
    }

    static InputAction BuildMovementAction(ControlScheme scheme)
    {
        var action = new InputAction("Movement", InputActionType.Value);
        if (scheme != ControlScheme.ArrowsOnly && scheme != ControlScheme.ArrowsAndGamepad)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
        }
        if (scheme != ControlScheme.WasdOnly)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
        }
        // Single player gets the gamepad too (Steam Deck / controller / on-screen
        // stick); in two-player mode the pad belongs to P2 only.
        if (scheme == ControlScheme.ArrowsAndGamepad || scheme == ControlScheme.WasdAndArrows)
        {
            action.AddBinding("<Gamepad>/leftStick");
            action.AddBinding("<Gamepad>/dpad");
        }
        return action;
    }

    // Called by GameManager when two vehicles crash: a brief physics-driven shove
    // away from the other vehicle. The Bouncy physics material on the collider
    // handles the contact itself; this gives the separation some visible energy.
    public void ApplyCrashKnockback(Vector3 direction)
    {
        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();
        if (playerRb == null)
            return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        knockbackUntil = Time.time + crashKnockbackDuration;
        playerRb.linearVelocity = direction.normalized * crashKnockbackSpeed;
    }

    void ResolveGameCamera()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    Vector3 ComputeScreenForward()
    {
        if (gameCamera == null)
            return Vector3.forward;

        Vector3 screenForward = gameCamera.transform.forward;
        screenForward.y = 0f;
        if (screenForward.sqrMagnitude < 0.001f)                   // Top-down camera looks straight down
            screenForward = gameCamera.transform.up;               // ...so "screen up" lives on the camera's up axis
        screenForward.y = 0f;
        screenForward.Normalize();
        return screenForward;
    }

    void ExitDriveTick()
    {
        ResolveGameCamera();
        if (gameCamera == null)
        {
            Debug.LogError("PlayerController exit drive is missing a game camera.", this);
            return;
        }

        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.MovePosition(playerRb.position + exitDirection * (speed * exitSpeedMultiplier) * Time.fixedDeltaTime);

        if (Time.time - exitStartTime < exitMinDuration)
            return;

        Bounds b = GetExitBounds();
        float halfAlong = Mathf.Abs(exitDirection.x) * b.extents.x + Mathf.Abs(exitDirection.z) * b.extents.z;
        float threshold = (exitViaBottom
            ? ScreenEdgeUtility.BottomAlongTravel(gameCamera, transform.position.y, exitDirection)
            : ScreenEdgeUtility.TopAlongTravel(gameCamera, transform.position.y, exitDirection))
            + exitOffScreenMargin;
        if (Vector3.Dot(b.center, exitDirection) - halfAlong > threshold)
        {
            isExiting = false;
            if (gameManager != null)
                gameManager.OnVehicleExitComplete(this);
            else
                enabled = false;
        }
    }

    Bounds GetExitBounds()
    {
        bool hasBounds = false;
        Bounds bounds = default;

        foreach (var renderer in GetComponentsInChildren<Renderer>(false))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                continue;
            if (!renderer.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        if (hasBounds)
            return bounds;

        if (playerCollider != null)
            return playerCollider.bounds;

        const float minExtent = 0.5f;
        Vector3 center = playerRb != null ? playerRb.position : transform.position;
        return new Bounds(center, new Vector3(minExtent * 2f, minExtent, minExtent * 2f));
    }

    // Recalculate the world-space limits that keep the player contrained on screen
    void UpdateBounds()
    {
        if (gameCamera == null)
            return;

        // Distance from the player's plane to the camera, used to project viewport points
        float planeDistance = Mathf.Abs(transform.position.y - gameCamera.transform.position.y);

        // Convert the four viewport corners into world positions. Using corners (not edge
        // midpoints) keeps the bounds correct no matter how the camera is rotated.
        Vector3 c00 = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0f, planeDistance));
        Vector3 c10 = gameCamera.ViewportToWorldPoint(new Vector3(1f, 0f, planeDistance));
        Vector3 c01 = gameCamera.ViewportToWorldPoint(new Vector3(0f, 1f, planeDistance));
        Vector3 c11 = gameCamera.ViewportToWorldPoint(new Vector3(1f, 1f, planeDistance));

        // Raw screen edges (where the vehicle's *centre* could go if it had no size).
        float regionMinX = Mathf.Min(Mathf.Min(c00.x, c10.x), Mathf.Min(c01.x, c11.x));
        float regionMaxX = Mathf.Max(Mathf.Max(c00.x, c10.x), Mathf.Max(c01.x, c11.x));
        float regionMinZ = Mathf.Min(Mathf.Min(c00.z, c10.z), Mathf.Min(c01.z, c11.z));
        float regionMaxZ = Mathf.Max(Mathf.Max(c00.z, c10.z), Mathf.Max(c01.z, c11.z));

        // The side walls are usually tighter than the screen edges, so honour them too.
        regionMinZ = Mathf.Max(regionMinZ, wallMinZ);
        regionMaxZ = Mathf.Min(regionMaxZ, wallMaxZ);

        // Inset by the vehicle's own half-size (from its collider) plus a little padding,
        // so the WHOLE body stays inside the walls/screen, not just its pivot point. The
        // collider can be offset from the pivot, so measure each face separately.
        Bounds b = playerCollider != null ? playerCollider.bounds : new Bounds(playerRb.position, Vector3.zero);
        float halfLeftX = playerRb.position.x - b.min.x;   // pivot → −X face
        float halfRightX = b.max.x - playerRb.position.x;  // pivot → +X face
        float halfLeftZ = playerRb.position.z - b.min.z;   // pivot → −Z face
        float halfRightZ = b.max.z - playerRb.position.z;  // pivot → +Z face

        minX = regionMinX + halfLeftX + boundaryPadding;
        maxX = regionMaxX - halfRightX - boundaryPadding;
        minZ = regionMinZ + halfLeftZ + wallPadding;
        maxZ = regionMaxZ - halfRightZ - wallPadding;

        // If the vehicle is wider than the space, park it in the middle rather than flip the bounds.
        if (minX > maxX) minX = maxX = 0.5f * (regionMinX + regionMaxX);
        if (minZ > maxZ) minZ = maxZ = 0.5f * (regionMinZ + regionMaxZ);
    }

    void FindWallBounds()
    {
        var faces = WallBoundsUtility.GetInnerFaces(wallsParentName, transform.position);
        if (!faces.Found)
            return;

        wallMinZ = faces.LowZ;
        wallMaxZ = faces.HighZ;
    }

    public float GetApproachSpeed(Vector3 towardOther)
    {
        towardOther.y = 0f;
        if (towardOther.sqrMagnitude < 0.001f)
            return 0f;
        return Mathf.Max(0f, Vector3.Dot(CurrentMovementDirection * speed, towardOther.normalized));
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        if (baseSpeed <= 0.001f)
            baseSpeed = speed;
        speed = baseSpeed * multiplier;
    }

    public void ResetSpeedToBase()
    {
        speed = baseSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Two-player mode: vehicles can crash into each other. GameManager dedupes
        // (both vehicles' OnCollisionEnter fire for the same contact) and applies
        // the timer penalty plus the bounce knockback to both.
        var otherVehicle = collision.gameObject.GetComponentInParent<PlayerController>();
        if (otherVehicle != null && otherVehicle != this)
        {
            // While either vehicle is still being shoved apart, contacts can break and
            // re-form every physics step; don't report those as fresh crashes.
            if (Time.time < knockbackUntil || Time.time < otherVehicle.knockbackUntil)
                return;

            Vector3 vehicleHitPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : playerRb.position;
            if (gameManager != null)
                gameManager.OnVehicleCollision(vehicleHitPoint, this, otherVehicle);
            return;
        }

        // MoveDown owns the full hit response (timer penalty, knock-aside, destroy FX).
        if (!collision.gameObject.CompareTag("Obstacle"))
            return;

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : playerRb.position;
        collision.gameObject.GetComponent<MoveDown>()?.RegisterPlayerHit(hitPoint, transform);
    }
}
