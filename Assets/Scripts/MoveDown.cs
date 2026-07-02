using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveDown : MonoBehaviour
{
    public float speed = 5.0f;        // Constant speed the obstacle travels at (units per second)
    public Camera gameCamera;         // Camera used to work out screen directions and edges
    public float wrapMargin = 1.5f;   // How far past the screen edge before wrapping back to the top
    public string wallsParentName = "Walls";  // Parent object whose children are the side walls
    public float wallPadding = 0.5f;  // Keeps the rock's body from clipping into a wall
    public float respawnDelay = 2.0f; // Seconds to wait after a player hit before the rock respawns
    public float knockAsideSpeed = 6.0f;  // Sideways shove given to the rock when the vehicle hits it
    [SerializeField] private AudioClip crushClip;   // Crushing boom played where the rock is destroyed
    [SerializeField] private float crushVolume = 0.7f;
    [SerializeField] private GameObject destroyEffectPrefab;   // Rubble burst spawned where the rock is destroyed

    private Rigidbody objectRb;       // Cached Rigidbody used for physics-based movement
    private Vector3 moveDirection;    // World-space direction that reads as "down the screen"
    private Vector3 spawnPosition;    // Where this obstacle returns to after passing the bottom
    private Quaternion spawnRotation; // Original orientation, restored after a knock-aside
    private float bottomThreshold;    // Distance-along-travel value at which the obstacle has left the screen
    private float minZ = float.NegativeInfinity;  // Lower Z limit from the walls (unbounded if unassigned)
    private float maxZ = float.PositiveInfinity;  // Upper Z limit from the walls
    private bool isKnockedAside;      // While true, physics drives the rock instead of the scripted rail
    private GameManager gameManager;  // Rocks only travel while the game is active

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectRb = GetComponent<Rigidbody>();
        gameManager = FindAnyObjectByType<GameManager>();
        objectRb.useGravity = false;                          // Movement is script-driven, not gravity
        objectRb.constraints = RigidbodyConstraints.FreezeRotation    // Don't spin on impact
                             | RigidbodyConstraints.FreezePositionY;  // Stay on the ground; a hit only shoves it sideways

        if (gameCamera == null)
            gameCamera = Camera.main;                         // Fall back to the main camera if none assigned

        spawnPosition = objectRb.position;                    // Remember the starting spot near the top
        spawnRotation = objectRb.rotation;                    // ...and its upright orientation to restore later

        // Figure out which world direction points down the screen on the ground plane.
        Vector3 screenUp = gameCamera.transform.forward;      // For an angled camera, forward is the screen-up axis
        screenUp.y = 0f;
        if (screenUp.sqrMagnitude < 0.001f)                   // Top-down camera looks straight down...
            screenUp = gameCamera.transform.up;               // ...so "screen up" lives on the camera's up axis
        screenUp.y = 0f;
        screenUp.Normalize();

        moveDirection = -screenUp;                            // Down the screen is the opposite of screen up

        // Project the bottom edge of the screen onto the travel axis so we know when we've gone off-screen.
        float planeDistance = Mathf.Abs(transform.position.y - gameCamera.transform.position.y);
        Vector3 bottomWorld = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, planeDistance));
        bottomThreshold = Vector3.Dot(bottomWorld, moveDirection) + wrapMargin;

        // Cache the Z range between the side walls' inner faces so a sideways
        // collision can't shove the rock out of the play field.
        GameObject wallsParent = GameObject.Find(wallsParentName);
        if (wallsParent != null && wallsParent.transform.childCount >= 2)
        {
            float lowZ = float.NegativeInfinity;   // Highest inner face on the low-Z side
            float highZ = float.PositiveInfinity;  // Lowest inner face on the high-Z side

            foreach (Transform wall in wallsParent.transform)
            {
                float halfDepth = wall.localScale.z * 0.5f;
                if (wall.position.z < spawnPosition.z)              // Wall on the low-Z side of the field
                    lowZ = Mathf.Max(lowZ, wall.position.z + halfDepth);
                else                                               // Wall on the high-Z side
                    highZ = Mathf.Min(highZ, wall.position.z - halfDepth);
            }

            minZ = lowZ + wallPadding;
            maxZ = highZ - wallPadding;
        }
    }

    // FixedUpdate is the correct place for Rigidbody physics work
    void FixedUpdate()
    {
        // Hold still until the player presses DRIVE (and freeze again at game over).
        if (gameManager != null && !gameManager.IsGameActive)
            return;

        // While being knocked aside, let physics carry the rock freely so the vehicle
        // can shove it off to the side instead of it stopping dead on its scripted rail —
        // but still keep it inside the walls, the same lateral boundary the vehicle obeys.
        if (isKnockedAside)
        {
            Vector3 knockedPos = objectRb.position;
            float clampedZ = Mathf.Clamp(knockedPos.z, minZ, maxZ);
            if (!Mathf.Approximately(knockedPos.z, clampedZ))
            {
                knockedPos.z = clampedZ;
                objectRb.position = knockedPos;
                Vector3 velocity = objectRb.linearVelocity;
                velocity.z = 0f;                    // Stop pushing outward once the wall is reached
                objectRb.linearVelocity = velocity;
            }
            return;
        }

        // Move at a constant speed down the screen so collisions are respected,
        // then clamp Z so a sideways hit can't push the rock past the walls.
        Vector3 targetPosition = objectRb.position + speed * Time.fixedDeltaTime * moveDirection;
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
        objectRb.MovePosition(targetPosition);

        // Once the obstacle has travelled past the bottom edge (off-screen behind the
        // player), destroy it so the spawner keeps feeding a fresh flow of rocks.
        if (Vector3.Dot(objectRb.position, moveDirection) > bottomThreshold)
        {
            if (crushClip != null)
                AudioSource.PlayClipAtPoint(crushClip, objectRb.position, crushVolume);
            if (destroyEffectPrefab != null)
            {
                GameObject fx = Instantiate(destroyEffectPrefab, objectRb.position, destroyEffectPrefab.transform.rotation);
                Destroy(fx, 3f);
            }
            Destroy(gameObject);
        }
    }

    // When the player vehicle hits this rock, hand it over to physics: shove it to
    // whichever side of the vehicle it sits on (keeping its downward drift) and let it
    // tumble away rather than stopping. It respawns respawnDelay seconds later.
    private void OnCollisionEnter(Collision collision)
    {
        if (isKnockedAside || !collision.gameObject.CompareTag("Player"))
            return;

        isKnockedAside = true;
        // Keep rotation frozen so the rock slides off to the side like a shoved boulder
        // instead of tumbling/spinning; only its position is driven by the shove below.
        objectRb.constraints = RigidbodyConstraints.FreezeRotation
                             | RigidbodyConstraints.FreezePositionY;

        // Push toward whichever side of the vehicle the rock is on, so it "falls" to
        // that side, while keeping its downward speed so it never simply stops.
        float side = objectRb.position.z >= collision.transform.position.z ? 1f : -1f;
        objectRb.linearVelocity = moveDirection * speed + Vector3.forward * (side * knockAsideSpeed);

        Invoke(nameof(Respawn), respawnDelay);
    }

    // Return the rock to its starting spot (in front of the finish line), restore its
    // upright orientation and rail movement, and clear any momentum from the collision.
    private void Respawn()
    {
        isKnockedAside = false;
        objectRb.constraints = RigidbodyConstraints.FreezeRotation      // Back on rails: no spin...
                             | RigidbodyConstraints.FreezePositionY;     // ...and locked to the ground plane
        objectRb.position = spawnPosition;
        objectRb.rotation = spawnRotation;
        objectRb.linearVelocity = Vector3.zero;
        objectRb.angularVelocity = Vector3.zero;
    }
}
