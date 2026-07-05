using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] obstacles;                // Obstacle prefab(s) to spawn

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

        // Don't add more rocks once we're at the cap, so the field can't fill up endlessly.
        if (FindObjectsByType<MoveDown>(FindObjectsInactive.Exclude).Length >= maxObstacles)
            return;

        // Spawn on the fixed X line, spread randomly across the field between
        // the two side walls (Z). The rock then travels down the screen.
        int obstacleIndex = Random.Range(0, obstacles.Length);
        if (obstacles[obstacleIndex] == null)
            return;

        // Vary each rock: random size (bigger rocks sit higher so they don't clip the
        // ground) and a random speed offset on top of the difficulty ramp's bonus.
        float sizeMultiplier = Random.Range(rockScaleRange.x, rockScaleRange.y);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(spawnX, spawnY * sizeMultiplier, randomZ);
        GameObject rock = Instantiate(obstacles[obstacleIndex], spawnPos, obstacles[obstacleIndex].transform.rotation);
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
