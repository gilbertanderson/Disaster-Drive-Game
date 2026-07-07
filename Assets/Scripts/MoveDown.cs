using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MoveDown : MonoBehaviour
{
    public float speed = 5.0f;        // Constant speed the obstacle travels at (units per second)
    public Camera gameCamera;         // Camera used to work out screen directions and edges
    public float wrapMargin = 1.5f;   // How far past the screen edge before wrapping back to the top
    public string wallsParentName = "Walls";  // Parent object whose children are the side walls
    public float wallPadding = 0.5f;  // Keeps the rock's body from clipping into a wall
    public float knockAsideSpeed = 6.0f;  // Sideways shove given to the rock when the vehicle hits it
    [SerializeField] private AudioClip crushClip;   // Crushing boom played where the rock is destroyed
    [SerializeField] private float crushVolume = 0.7f;
    [SerializeField] private GameObject destroyEffectPrefab;   // Rubble burst spawned where the rock is destroyed
    [SerializeField] private float destroyFxScaleMultiplier = 0.22f;
    [SerializeField] private float destroyFxCameraSafetyScale = 0.85f;
    [SerializeField] private float destroyFxDownOffset = 0.2f;
    [SerializeField] private float destroyFxCameraForwardOffset = 0.15f;
    [SerializeField] private float destroyFxSimulationSpeed = 2.75f;
    [SerializeField] private float destroyFxSpeedMultiplier = 2f;
    [SerializeField] private float destroyFxLifetimeMultiplier = 0.6f;

    private Rigidbody objectRb;       // Cached Rigidbody used for physics-based movement
    private SphereCollider rockCollider;
    private Vector3 moveDirection;    // World-space direction that reads as "down the screen"
    private Vector3 spawnPosition;    // Where this obstacle returns to after passing the bottom
    private Quaternion spawnRotation; // Original orientation, restored after a knock-aside
    private float bottomThreshold;    // Distance-along-travel value at which the obstacle has left the screen
    private float minZ = float.NegativeInfinity;  // Lower Z limit from the walls (unbounded if unassigned)
    private float maxZ = float.PositiveInfinity;  // Upper Z limit from the walls
    private bool isKnockedAside;      // While true, physics drives the rock instead of the scripted rail
    private bool hitPlayer;           // True once the vehicle collides with this rock
    private bool nearMissChecked;     // Near-miss bonus is evaluated at most once per rock
    private Transform playerTransform;          // Single-player fallback (also used by tests)
    private PlayerController[] players;         // All vehicles; near-miss goes to whichever is nearest
    private GameManager gameManager;  // Rocks travel while the world is animating (active run or exit drive)
    [SerializeField] private float nearMissDistance = 2f;  // Lateral (Z) gap that still counts as a close dodge

    public static int ActiveRockCount { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetActiveRockCount()
    {
        ActiveRockCount = 0;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectRb = GetComponent<Rigidbody>();
        rockCollider = GetComponent<SphereCollider>();
        objectRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        gameManager = FindAnyObjectByType<GameManager>();
        players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (players.Length > 0)
            playerTransform = players[0].transform;
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
        bottomThreshold = ScreenEdgeUtility.BottomAlongTravel(gameCamera, transform.position.y, moveDirection) + wrapMargin;

        // Cache the Z range between the side walls' inner faces so a sideways
        // collision can't shove the rock out of the play field.
        if (WallBoundsUtility.TryGetPaddedRange(wallsParentName, spawnPosition, wallPadding, out float lowZ, out float highZ))
        {
            minZ = lowZ;
            maxZ = highZ;
        }
    }

    void OnEnable()
    {
        ActiveRockCount++;
    }

    void OnDisable()
    {
        ActiveRockCount = Mathf.Max(0, ActiveRockCount - 1);
    }

    // FixedUpdate is the correct place for Rigidbody physics work
    void FixedUpdate()
    {
        // Hold still on the start screen; keep scrolling during the post-game-over exit drive.
        if (gameManager != null && !gameManager.IsWorldAnimating)
            return;

        TryAwardNearMiss();
        TryDetectPlayerOverlap();

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

        // Clear residual velocity from rock-on-rock bumps so only the scripted rail
        // moves the obstacle (same pattern as PlayerController).
        objectRb.linearVelocity = Vector3.zero;
        objectRb.angularVelocity = Vector3.zero;
        objectRb.MovePosition(targetPosition);

        Vector3 settled = objectRb.position;
        settled.z = Mathf.Clamp(settled.z, minZ, maxZ);
        if (!Mathf.Approximately(settled.z, objectRb.position.z))
            objectRb.position = settled;

        // Once the obstacle has travelled past the bottom edge (off-screen behind the
        // player), destroy it so the spawner keeps feeding a fresh flow of rocks.
        if (Vector3.Dot(objectRb.position, moveDirection) > bottomThreshold)
        {
            if (!hitPlayer && gameManager != null)
                gameManager.OnRockDodged();
            Destroy(gameObject);
        }
    }

    void TryAwardNearMiss()
    {
        if (nearMissChecked || hitPlayer || gameManager == null || !gameManager.IsGameActive)
            return;

        // In two-player mode the dodge credit goes to whichever vehicle the rock
        // passed closest to; with one player this is just that player.
        PlayerController nearest = NearestActivePlayer(out Transform nearestTransform);
        if (nearestTransform == null)
            return;

        float rockAlong = Vector3.Dot(objectRb.position, moveDirection);
        float playerAlong = Vector3.Dot(nearestTransform.position, moveDirection);
        if (rockAlong <= playerAlong)
            return;

        nearMissChecked = true;
        float lateralGap = Mathf.Abs(objectRb.position.z - nearestTransform.position.z);
        if (lateralGap <= nearMissDistance)
            gameManager.OnNearMiss(nearest);
    }

    PlayerController NearestActivePlayer(out Transform nearestTransform)
    {
        PlayerController nearest = null;
        nearestTransform = null;
        float bestSqrDistance = float.PositiveInfinity;

        if (players != null)
        {
            foreach (var candidate in players)
            {
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                    continue;

                float sqrDistance = (candidate.transform.position - objectRb.position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nearest = candidate;
                    nearestTransform = candidate.transform;
                }
            }
        }

        // Tests (and older setups) assign playerTransform directly without a controller.
        if (nearestTransform == null)
            nearestTransform = playerTransform;
        return nearest;
    }

    // When the player vehicle hits this rock, shove it aside for one tick so the impact
    // still reads as physical, then destroy it shortly after (rather than respawning).
    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        Vector3 hitPoint = collision.contactCount > 0
            ? collision.GetContact(0).point
            : objectRb.position;
        RegisterPlayerHit(hitPoint, collision.transform);
    }

    void TryDetectPlayerOverlap()
    {
        if (isKnockedAside || hitPlayer || rockCollider == null || playerTransform == null)
            return;

        Vector3 worldCenter = transform.TransformPoint(rockCollider.center);
        float worldRadius = rockCollider.radius * Mathf.Max(transform.lossyScale.x, 0.001f);
        var overlaps = Physics.OverlapSphere(worldCenter, worldRadius, ~0, QueryTriggerInteraction.Ignore);
        foreach (var overlap in overlaps)
        {
            if (!overlap.CompareTag("Player"))
                continue;

            Vector3 hitPoint = overlap.ClosestPoint(worldCenter);
            RegisterPlayerHit(hitPoint, overlap.transform);
            return;
        }
    }

    public void RegisterPlayerHit(Vector3 hitPoint, Transform player)
    {
        if (isKnockedAside || hitPlayer)
            return;

        hitPlayer = true;
        isKnockedAside = true;
        objectRb.constraints = RigidbodyConstraints.FreezeRotation
                             | RigidbodyConstraints.FreezePositionY;

        if (gameManager != null)
        {
            // Charge the penalty to whichever player's vehicle took the hit.
            var hitController = player != null ? player.GetComponentInParent<PlayerController>() : null;
            gameManager.OnPlayerHit(hitPoint, hitController);
        }

        // Push toward whichever side of the vehicle the rock is on, so it "falls" to
        // that side, while keeping its downward speed so it never simply stops.
        float side = objectRb.position.z >= player.position.z ? 1f : -1f;
        objectRb.linearVelocity = moveDirection * speed + Vector3.forward * (side * knockAsideSpeed);

        Invoke(nameof(DestroyRock), 0.01f);
    }

    private void DestroyRock()
    {
        SpawnDestroyFx();
        Destroy(gameObject);
    }

    // Rubble burst + crush sound on player-hit destruction, scaled to this rock's footprint.
    private void SpawnDestroyFx()
    {
        if (crushClip != null)
            AudioSource.PlayClipAtPoint(crushClip, objectRb.position, crushVolume);

        if (destroyEffectPrefab != null)
        {
            Vector3 camForward = gameCamera != null ? gameCamera.transform.forward : Vector3.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.001f)
                camForward.Normalize();

            Vector3 fxPos = objectRb.position
                + Vector3.down * destroyFxDownOffset
                + camForward * destroyFxCameraForwardOffset;
            GameObject fx = Instantiate(destroyEffectPrefab, fxPos, destroyEffectPrefab.transform.rotation);
            float rockScaleFactor = Mathf.Clamp(transform.localScale.x, 0.3f, 1f);
            float finalScale = rockScaleFactor * destroyFxScaleMultiplier * destroyFxCameraSafetyScale;
            fx.transform.localScale *= finalScale;
            BoostDestroyParticles(fx);
            Destroy(fx, 1.5f);
        }
    }

    static void BoostDestroyParticles(GameObject fx, float simulationSpeed, float speedMultiplier, float lifetimeMultiplier)
    {
        foreach (var ps in fx.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            main.simulationSpeed = simulationSpeed;
            main.startDelay = 0f;

            var lifetime = main.startLifetime;
            lifetime.constant *= lifetimeMultiplier;
            lifetime.constantMin *= lifetimeMultiplier;
            lifetime.constantMax *= lifetimeMultiplier;
            main.startLifetime = lifetime;

            var startSpeed = main.startSpeed;
            startSpeed.constant *= speedMultiplier;
            startSpeed.constantMin *= speedMultiplier;
            startSpeed.constantMax *= speedMultiplier;
            main.startSpeed = startSpeed;
        }
    }

    void BoostDestroyParticles(GameObject fx)
    {
        BoostDestroyParticles(fx, destroyFxSimulationSpeed, destroyFxSpeedMultiplier, destroyFxLifetimeMultiplier);
    }
}
