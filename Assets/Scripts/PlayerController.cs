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

    private Rigidbody playerRb;               // Cached Rigidbody for physics-based movement
    private Vector2 movementInput;            // Latest input value read each frame

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerRb = GetComponent<Rigidbody>();
        playerRb.constraints = RigidbodyConstraints.FreezeRotation;  // Keep the player upright
        movementAction.Enable();                                     // Begin listening for input

        if (gameCamera == null)
            gameCamera = Camera.main;                                // Fall back to the main camera if none assigned

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

        // Build a normalized direction so diagonal movement isn't faster
        Vector3 movementDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
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

        // Convert the four viewport edge midpoints into world positions
        Vector3 bottomWorld = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, planeDistance));
        Vector3 topWorld = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 1f, planeDistance));
        Vector3 leftWorld = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0.5f, planeDistance));
        Vector3 rightWorld = gameCamera.ViewportToWorldPoint(new Vector3(1f, 0.5f, planeDistance));

        // Store padded min/max so the player stays just inside the visible edges
        minZ = Mathf.Min(bottomWorld.z, topWorld.z) + boundaryPadding;
        maxZ = Mathf.Max(bottomWorld.z, topWorld.z) - boundaryPadding;
        minX = Mathf.Min(leftWorld.x, rightWorld.x) + boundaryPadding;
        maxX = Mathf.Max(leftWorld.x, rightWorld.x) - boundaryPadding;
    }
}
