using System.Collections.Generic;
using UnityEngine;

// Endless belt of decorative stone-wall tiles on the grass strips beside the road.
// Each lane holds exactly two long tiles (a "two-tile leapfrog", like a classic
// endless-runner background): when a tile fully exits the screen it jumps directly
// behind its partner tile in the same lane, using that tile's authored TileLength.
// There is no runtime bounds measurement and no scanning across many segments --
// each lane only ever has its own pair to consider.
public class WallSpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject wallTilePrefab;
    [SerializeField] private GroundScroller grassScroller;
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private float leftLaneZ = 11.5f;
    [SerializeField] private float rightLaneZ = -7.9f;
    [SerializeField] private float segmentY = 0.05f;

    private Transform segmentRoot;
    private GameManager gameManager;
    private readonly List<(WallScroller a, WallScroller b)> lanePairs = new List<(WallScroller, WallScroller)>();

    void Awake()
    {
        if (grassScroller == null)
            grassScroller = GameObject.Find("Plane")?.GetComponent<GroundScroller>();
        segmentRoot = transform;
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        SpawnLane(leftLaneZ);
        SpawnLane(rightLaneZ);
    }

    void SpawnLane(float laneZ)
    {
        WallScroller tileA = SpawnTile(laneZ, spawnX);
        if (tileA == null)
            return;

        WallScroller tileB = SpawnTile(laneZ, spawnX + tileA.TileLength);
        if (tileB == null)
            return;

        lanePairs.Add((tileA, tileB));
    }

    WallScroller SpawnTile(float laneZ, float x)
    {
        if (wallTilePrefab == null)
            return null;

        Vector3 pos = new Vector3(x, segmentY, laneZ);
        GameObject tile = Instantiate(wallTilePrefab, pos, Quaternion.identity, segmentRoot);

        WallScroller scroller = tile.GetComponent<WallScroller>();
        if (scroller == null)
            scroller = tile.AddComponent<WallScroller>();
        scroller.LaneZ = laneZ;
        scroller.Configure(grassScroller);
        return scroller;
    }

    public void OnSegmentLeftScreen(WallScroller tile)
    {
        if (tile == null)
            return;

        if (gameManager != null && !gameManager.IsWorldAnimating)
            return;

        if (!TryGetPartner(tile, out WallScroller partner))
            return;

        Vector3 moveDir = grassScroller != null ? grassScroller.WorldMoveDirection : Vector3.left;
        Vector3 newPos = partner.transform.position - moveDir * tile.TileLength;
        tile.transform.position = new Vector3(newPos.x, tile.transform.position.y, tile.LaneZ);
    }

    bool TryGetPartner(WallScroller tile, out WallScroller partner)
    {
        for (int i = 0; i < lanePairs.Count; i++)
        {
            var (a, b) = lanePairs[i];
            if (a == tile)
            {
                partner = b;
                return true;
            }
            if (b == tile)
            {
                partner = a;
                return true;
            }
        }

        partner = null;
        return false;
    }
}
