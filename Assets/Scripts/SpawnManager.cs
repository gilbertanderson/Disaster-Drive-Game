using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] obtacles;

    private float zObstacleSpawn = 12.0f;
    private float xObstacleSpawnRange = 16.0f;
    private float obsstacleSpawnInterval = 1.5f;
    private float startDelay = 2.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating("SpawnObstacle", startDelay, obsstacleSpawnInterval);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnObstacle()
    {
        float randomX = Random.Range(-xObstacleSpawnRange, xObstacleSpawnRange);
        int obstacleIndex = Random.Range(0, obtacles.Length);
        Vector3 spawnPos = new Vector3(randomX, 0, zObstacleSpawn);
        Instantiate(obtacles[obstacleIndex], spawnPos, obtacles[obstacleIndex].transform.rotation);
    }
}
