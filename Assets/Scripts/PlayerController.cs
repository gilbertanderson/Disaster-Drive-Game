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

    private Rigidbody playerRb;               // Cached Rigidbody for physics-based movement
    private Vector2 movementInput;            // Latest input value read each frame
    private float wallMinZ = float.NegativeInfinity;  // Inner face of the low-Z wall (unbounded until found)
    private float wallMaxZ = float.PositiveInfinity;  // Inner face of the high-Z wall

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerRb.useGravity = false;                                 // Top-down game: no gravity, movement is script-driven
        playerRb.constraints = RigidbodyConstraints.FreezeRotation    // Keep the player upright
                             | RigidbodyConstraints.FreezePositionY;  // Lock to the ground plane (prevents jitter)
        movementAction.Enable();                                     // Begin listening for input

        if (gameCamera == null)
            gameCamera = Camera.main;                                // Fall back to the main camera if none assigned

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
        float horizontalInput = movementInput.x;
        float verticalInput = movementInput.y;

        // Derive movement axes from the camera so controls always match the screen,
        // no matter how the camera is rotated.
        Vector3 screenRight = gameCamera.transform.right;          // Maps to left/right input
        Vector3 screenForward = gameCamera.transform.forward;      // Maps to up/down input
        screenForward.y = 0f;
        if (screenForward.sqrMagnitude < 0.001f)                   // Top-down camera looks straight down
            screenForward = gameCamera.transform.up;               // ...so "screen up" lives on the camera's up axis
        screenRight.y = 0f;
        screenForward.y = 0f;
        screenRight.Normalize();
        screenForward.Normalize();

        // Build a normalized direction so diagonal movement isn't faster
        Vector3 movementDirection = (screenRight * horizontalInput + screenForward * verticalInput).normalized;
        Vector3 movement = movementDirection * speed * Time.fixedDeltaTime;

        // Apply movement, then clamp the result inside the playable area
        Vector3 targetPosition = playerRb.position + movement;
        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);

        playerRb.MovePosition(targetPosition);     // Move via physics so collisions are respected
        playerRb.angularVelocity = Vector3.zero;   // Cancel any spin picked up from collisions
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

        // Store padded min/max so the player stays just inside the visible edges
        minX = Mathf.Min(Mathf.Min(c00.x, c10.x), Mathf.Min(c01.x, c11.x)) + boundaryPadding;
        maxX = Mathf.Max(Mathf.Max(c00.x, c10.x), Mathf.Max(c01.x, c11.x)) - boundaryPadding;
        minZ = Mathf.Min(Mathf.Min(c00.z, c10.z), Mathf.Min(c01.z, c11.z)) + boundaryPadding;
        maxZ = Mathf.Max(Mathf.Max(c00.z, c10.z), Mathf.Max(c01.z, c11.z)) - boundaryPadding;

        // Keep the player between the side walls' inner faces (usually tighter than
        // the screen edges) so it can't push through them.
        minZ = Mathf.Max(minZ, wallMinZ);
        maxZ = Mathf.Min(maxZ, wallMaxZ);
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

        wallMinZ = lowZ + wallPadding;
        wallMaxZ = highZ - wallPadding;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // If the player hits a rock, log a message. In a real game, this is where
        // you'd trigger a "game over" or "lose a life" event.
        if (collision.gameObject.CompareTag("Obstacle"))
            Debug.Log("Player hit an obstacle!");
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the player enters a trigger zone (like a goal area), log a message.
        // In a real game, this is where you'd trigger a "level complete" event.
        if (other.CompareTag("Goal"))
            Debug.Log("Player reached the goal!");
    }
}
