using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] obstacles;                // Gameplay shell prefab(s); rockVisuals swaps the mesh at spawn

    public float spawnX = 7.0f;                   // Travel-axis spawn line near the top of the field
    public float spawnY = 0.6f;                   // Height the rocks sit at above the ground
    public string wallsParentName = "Walls";      // Parent whose children mark the lateral (Z) bounds
    public float lateralPadding = 0.5f;           // Keep spawns a little inside the walls so rocks don't clip them
    public int maxObstacles = 4;                  // Cap on how many rocks may exist at once

    [SerializeField] private float obstacleSpawnInterval = 2.5f;  // Seconds between spawns at the start
    [SerializeField] private float startDelay = 2.0f;             // Delay before the first spawn
    [SerializeField] private float minSpawnInterval = 0.8f;       // Floor so the difficulty ramp can't spawn absurdly fast
    [SerializeField] private int maxObstaclesCap = 8;             // Ceiling the ramp can raise maxObstacles to

    [Header("Rock Variety")]
    [SerializeField] private GameObject[] rockVisuals;  // Mesh prefabs swapped onto the shell (PolyOne, JC, etc.)
    [SerializeField] private Vector2 rockScaleRange = new Vector2(0.7f, 1.4f);  // Random size multiplier per rock
    [SerializeField] private Vector2 rockSpeedJitter = new Vector2(-1f, 2f);    // Random speed offset per rock
    [SerializeField] private float rockSpeedMultiplier = 2.25f;                  // Match decorative trees so rocks travel at the same world speed

    private float currentInterval;                // Live spawn interval; shrinks as difficulty ramps
    private float rockSpeedBonus;                 // Added to each newly spawned rock's speed
    private bool isSpawning = true;
    private float minZ = -5.0f;                   // Lateral spawn range between the walls (recomputed from the walls in Start)
    private float maxZ = 10.0f;
    private GameManager gameManager;              // Rocks only flow while the game is active
    private GroundScroller groundScroller;        // Used to sync rock movement with the decorative trees

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentInterval = obstacleSpawnInterval;
        gameManager = FindAnyObjectByType<GameManager>();
        groundScroller = FindAnyObjectByType<GroundScroller>();
        FindWallRange();                          // Work out the lateral spawn range from the side walls
        StartCoroutine(SpawnLoop());
    }

    // Coroutine instead of InvokeRepeating so the interval can shrink over time
    // and spawning can be stopped cleanly at game over.
    IEnumerator SpawnLoop()
    {
        // Wait on the start screen until the player presses Start (skipped if no GameManager exists).
        yield return new WaitUntil(() => gameManager == null || gameManager.IsGameActive);
        yield return new WaitForSeconds(startDelay);
        while (isSpawning)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(currentInterval);
        }
    }

    // Called by GameManager on each difficulty bump: rocks travel faster, spawn
    // sooner, and slightly more of them are allowed on the field at once.
    public void IncreaseDifficulty(float rockSpeedIncrease, float intervalMultiplier)
    {
        rockSpeedBonus += rockSpeedIncrease;
        currentInterval = Mathf.Max(currentInterval * intervalMultiplier, minSpawnInterval);
        maxObstacles = Mathf.Min(maxObstacles + 1, maxObstaclesCap);
    }

    // Called by GameManager when the timer runs out.
    public void StopSpawning()
    {
        isSpawning = false;
    }

    void SpawnObstacle()
    {
        if (obstacles.Length == 0)
            return;

        if (FindObjectsByType<MoveDown>(FindObjectsInactive.Exclude).Length >= maxObstacles)
            return;

        float sizeMultiplier = Random.Range(rockScaleRange.x, rockScaleRange.y);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(spawnX, spawnY * sizeMultiplier, randomZ);

        GameObject rock;
        if (rockVisuals != null && rockVisuals.Length > 0)
        {
            if (obstacles[0] == null)
                return;
            rock = Instantiate(obstacles[0], spawnPos, obstacles[0].transform.rotation);
            ApplyRandomVisual(rock);
        }
        else
        {
            int obstacleIndex = Random.Range(0, obstacles.Length);
            if (obstacles[obstacleIndex] == null)
                return;
            rock = Instantiate(obstacles[obstacleIndex], spawnPos, obstacles[obstacleIndex].transform.rotation);
        }

        rock.transform.localScale *= sizeMultiplier;

        MoveDown mover = rock.GetComponentInChildren<MoveDown>();
        if (mover != null)
        {
            float baseRockSpeed = groundScroller != null
                ? groundScroller.WorldSpeed * rockSpeedMultiplier
                : mover.speed;
            mover.speed = Mathf.Max(1.5f, baseRockSpeed + rockSpeedBonus + Random.Range(rockSpeedJitter.x, rockSpeedJitter.y));
        }
    }

    void ApplyRandomVisual(GameObject rock)
    {
        for (int i = rock.transform.childCount - 1; i >= 0; i--)
            Destroy(rock.transform.GetChild(i).gameObject);

        GameObject visualPrefab = rockVisuals[Random.Range(0, rockVisuals.Length)];
        if (visualPrefab == null)
            return;

        GameObject visual = Instantiate(visualPrefab, rock.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        foreach (var childCollider in visual.GetComponentsInChildren<Collider>())
            childCollider.enabled = false;

        AlignVisualToGround(visual, rock);
        FitSphereColliderToVisual(rock, visual);
    }

    void AlignVisualToGround(GameObject visual, GameObject rock)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        visual.transform.position += Vector3.up * (rock.transform.position.y - bounds.min.y);
    }

    void FitSphereColliderToVisual(GameObject rock, GameObject visual)
    {
        var collider = rock.GetComponent<SphereCollider>();
        if (collider == null)
            return;

        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        collider.center = rock.transform.InverseTransformPoint(bounds.center);
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float uniformScale = Mathf.Max(rock.transform.lossyScale.x, 0.001f);
        collider.radius = maxExtent / uniformScale;
    }

    // Read the side walls so spawns land between them (matches the clamp the rocks use).
    void FindWallRange()
    {
        GameObject wallsParent = GameObject.Find(wallsParentName);
        if (wallsParent == null || wallsParent.transform.childCount < 2)
            return;                               // Keep the default range if the walls can't be found

        float lowZ = float.NegativeInfinity;      // Highest inner face on the low-Z side
        float highZ = float.PositiveInfinity;     // Lowest inner face on the high-Z side

        foreach (Transform wall in wallsParent.transform)
        {
            float halfDepth = wall.localScale.z * 0.5f;
            if (wall.position.z < transform.position.z)        // Wall on the low-Z side of the field
                lowZ = Mathf.Max(lowZ, wall.position.z + halfDepth);
            else                                               // Wall on the high-Z side
                highZ = Mathf.Min(highZ, wall.position.z - halfDepth);
        }

        minZ = lowZ + lateralPadding;
        maxZ = highZ - lateralPadding;
    }
}
