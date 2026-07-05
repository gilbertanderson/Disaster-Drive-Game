using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 10f;                 // Movement speed in units per second
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

    private Rigidbody playerRb;               // Cached Rigidbody for physics-based movement
    private Collider playerCollider;          // Cached collider, used to keep the whole body inside the bounds
    private GameManager gameManager;          // Notified when the vehicle hits a rock
    private Vector2 movementInput;            // Latest input value read each frame
    private bool isExiting;
    private Vector3 exitDirection;
    private float wallMinZ = float.NegativeInfinity;  // Inner face of the low-Z wall (unbounded until found)
    private float wallMaxZ = float.PositiveInfinity;  // Inner face of the high-Z wall

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();                    // Used to keep the whole vehicle body inside the bounds
        playerRb.useGravity = false;                                 // Top-down game: no gravity, movement is script-driven
        playerRb.constraints = RigidbodyConstraints.FreezeRotation    // Keep the player upright
                             | RigidbodyConstraints.FreezePositionY;  // Lock to the ground plane (prevents jitter)
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
            return;

        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;

        Vector3 screenRight = gameCamera.transform.right;
        screenRight.y = 0f;
        screenRight.Normalize();
        Vector3 screenForward = ComputeScreenForward();

        // Build a normalized direction so diagonal movement isn't faster
        Vector3 movementDirection = (screenRight * horizontalInput + screenForward * verticalInput).normalized;
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

    public void BeginExitDrive()
    {
        isExiting = true;
        exitDirection = ComputeScreenForward();
    }

    Vector3 ComputeScreenForward()
    {
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
        if (gameCamera == null)
        {
            isExiting = false;
            if (gameManager != null)
                gameManager.OnVehicleExitComplete();
            else
                enabled = false;
            return;
        }

        playerRb.linearVelocity = Vector3.zero;
        playerRb.angularVelocity = Vector3.zero;
        playerRb.MovePosition(playerRb.position + exitDirection * (speed * exitSpeedMultiplier) * Time.fixedDeltaTime);

        Bounds b = playerCollider != null ? playerCollider.bounds : new Bounds(playerRb.position, Vector3.zero);
        float halfAlong = Mathf.Abs(exitDirection.x) * b.extents.x + Mathf.Abs(exitDirection.z) * b.extents.z;
        float threshold = ScreenEdgeUtility.TopAlongTravel(gameCamera, transform.position.y, exitDirection) + exitOffScreenMargin;
        if (Vector3.Dot(b.center, exitDirection) - halfAlong > threshold)
        {
            isExiting = false;
            if (gameManager != null)
                gameManager.OnVehicleExitComplete();
            else
                enabled = false;
        }
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

    // Scan the children of the "Walls" object and record the Z range between the two
    // walls' inner faces. Runs once at Start since the walls don't move.
    void FindWallBounds()
    {
        GameObject wallsParent = GameObject.Find(wallsParentName);
        if (wallsParent == null || wallsParent.transform.childCount < 2)
            return;                                                 // Leave the bounds unbounded if we can't find the walls

        float lowZ = float.NegativeInfinity;                        // Highest inner face on the low-Z side
        float highZ = float.PositiveInfinity;                       // Lowest inner face on the high-Z side
        Vector3 center = transform.position;

        foreach (Transform wall in wallsParent.transform)
        {
            float halfDepth = wall.localScale.z * 0.5f;
            if (wall.position.z < center.z)                          // Wall sits on the low-Z side of the field
                lowZ = Mathf.Max(lowZ, wall.position.z + halfDepth);
            else                                                    // Wall sits on the high-Z side
                highZ = Mathf.Min(highZ, wall.position.z - halfDepth);
        }

        // Store the raw inner faces; padding and the vehicle's half-size are applied in UpdateBounds.
        wallMinZ = lowZ;
        wallMaxZ = highZ;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Hitting a rock costs time: the GameManager docks the timer, plays the impact
        // sound, and kicks up dust at the point of contact.
        if (collision.gameObject.CompareTag("Obstacle") && gameManager != null)
            gameManager.OnPlayerHit(collision.GetContact(0).point);
    }
}
