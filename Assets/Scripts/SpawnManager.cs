using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] obtacles;                 // Obstacle prefab(s) to spawn (name kept to preserve the Inspector link)

    public float spawnX = 7.0f;                   // Travel-axis spawn line, just in front of the finish line (top of the field)
    public float spawnY = 0.6f;                   // Height the rocks sit at above the ground
    public string wallsParentName = "Walls";      // Parent whose children mark the lateral (Z) bounds
    public float lateralPadding = 0.5f;           // Keep spawns a little inside the walls so rocks don't clip them
    public int maxObstacles = 4;                  // Cap on how many rocks may exist at once

    private float obstacleSpawnInterval = 2.5f;   // Seconds between spawns
    private float startDelay = 2.0f;              // Delay before the first spawn
    private float minZ = -5.0f;                   // Lateral spawn range between the walls (recomputed from the walls in Start)
    private float maxZ = 10.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FindWallRange();                          // Work out the lateral spawn range from the side walls
        InvokeRepeating(nameof(SpawnObstacle), startDelay, obstacleSpawnInterval);
    }

    void SpawnObstacle()
    {
        if (obtacles.Length == 0)
            return;

        // Don't add more rocks once we're at the cap. Rocks wrap/loop rather than being
        // destroyed, so without this the field would fill up endlessly.
        if (FindObjectsByType<MoveDown>(FindObjectsInactive.Exclude).Length >= maxObstacles)
            return;

        // Spawn on the fixed X line in front of the finish line, spread randomly across
        // the field between the two side walls (Z). The rock then travels down the screen.
        int obstacleIndex = Random.Range(0, obtacles.Length);
        if (obtacles[obstacleIndex] == null)   // Reference was destroyed (e.g. a scene rock) — assign a prefab instead
            return;

        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, randomZ);
        Instantiate(obtacles[obstacleIndex], spawnPos, obtacles[obstacleIndex].transform.rotation);
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
