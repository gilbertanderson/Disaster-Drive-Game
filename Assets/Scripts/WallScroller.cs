using UnityEngine;

// Moves a fence segment down-screen at the same world speed as the grass scroll.
public class WallScroller : MonoBehaviour
{
    public Camera gameCamera;
    public float wrapMargin = 2f;

    [SerializeField] private float wallSpeedMultiplier = 2.25f;
    [SerializeField] private float recycleSpawnX = 12f;
    [SerializeField] private float recycleSpawnMargin = 1f;

    private GroundScroller grassScroller;
    private WallSpawnManager spawnManager;
    private GameManager gameManager;
    private Vector3 moveDirection = Vector3.left;
    private float bottomThreshold;
    private float speed = 2.5f;

    public float LaneZ { get; set; }

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
