using UnityEngine;

// Moves one long stone-wall tile down-screen at the same world speed as the grass
// scroll. The tile's own length along the travel axis is authored once (baked into
// the prefab as `tileLength`) rather than measured at runtime, so the two-tile
// leapfrog recycling math in WallSpawnManager is exact and seam-free.
//
// Pivot convention: transform.position is the tile's LEADING edge (the edge nearest
// the spawn/top side of the screen). The tile's geometry extends backward from there
// (in the +moveDirection sense) by `tileLength` to reach its trailing edge. Because the
// leading edge is also the last part of the tile to clear the bottom of the screen, the
// existing bottom-threshold check (based on the pivot alone) is already correct for a
// tile of any length -- no per-frame bounds measurement needed.
public class WallScroller : MonoBehaviour
{
    public Camera gameCamera;
    public float wrapMargin = 2f;

    [SerializeField] private float wallSpeedMultiplier = 2.25f;
    [SerializeField] private float tileLength = 30f; // authored: total span of this tile along the travel axis
    [SerializeField] private float recycleSpawnX = 12f;
    [SerializeField] private float recycleSpawnMargin = 1f;

    private GroundScroller grassScroller;
    private WallSpawnManager spawnManager;
    private GameManager gameManager;
    private Vector3 moveDirection = Vector3.left;
    private float bottomThreshold;
    private float speed = 2.5f;

    public float LaneZ { get; set; }
    public float TileLength => tileLength;

    public void Configure(GroundScroller scroller)
    {
        grassScroller = scroller;
        RefreshMotion();
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        spawnManager = FindAnyObjectByType<WallSpawnManager>(FindObjectsInactive.Include);

        if (grassScroller == null)
            grassScroller = FindAnyObjectByType<GroundScroller>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        RefreshMotion();
        UpdateBottomThreshold();
    }

    void Update()
    {
        if (gameManager != null && !gameManager.IsWorldAnimating)
            return;

        if (grassScroller != null)
            RefreshMotion();

        transform.position += speed * Time.deltaTime * moveDirection;

        if (Vector3.Dot(transform.position, moveDirection) > bottomThreshold)
        {
            if (spawnManager != null)
                spawnManager.OnSegmentLeftScreen(this);
            else
                transform.position = new Vector3(GetFallbackRespawnX(), transform.position.y, LaneZ);
        }
    }

    void RefreshMotion()
    {
        if (grassScroller == null)
            return;

        moveDirection = grassScroller.WorldMoveDirection;
        speed = grassScroller.PropScrollSpeed(wallSpeedMultiplier);
    }

    void UpdateBottomThreshold()
    {
        if (gameCamera == null)
            return;

        bottomThreshold = ScreenEdgeUtility.BottomAlongTravel(gameCamera, transform.position.y, moveDirection) + wrapMargin;
    }

    float GetFallbackRespawnX()
    {
        if (gameCamera == null)
            return recycleSpawnX;

        float topAlongTravel = ScreenEdgeUtility.TopAlongTravel(gameCamera, transform.position.y, moveDirection);
        Vector3 spawnWorld = new Vector3(recycleSpawnX, transform.position.y, 0f)
            + moveDirection * (topAlongTravel - recycleSpawnMargin - Vector3.Dot(new Vector3(recycleSpawnX, transform.position.y, 0f), moveDirection));
        return spawnWorld.x;
    }
}
