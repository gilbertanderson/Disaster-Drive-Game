using UnityEngine;

// Moves a decorative tree down-screen at the same world speed as the grass scroll.
public class TreeScroller : MonoBehaviour
{
    public Camera gameCamera;
    public float wrapMargin = 2f;

    [SerializeField] private float treeSpeedMultiplier = 2.25f;

    private GroundScroller grassScroller;
    private TreeSpawnManager spawnManager;
    private GameManager gameManager;
    private Vector3 moveDirection = Vector3.left;
    private float bottomThreshold;
    private float speed = 2.5f;

    public void Configure(GroundScroller scroller)
    {
        grassScroller = scroller;
        RefreshMotion();
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        spawnManager = FindAnyObjectByType<TreeSpawnManager>();

        if (grassScroller == null)
            grassScroller = FindAnyObjectByType<GroundScroller>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        RefreshMotion();
        UpdateBottomThreshold();
    }

    void Update()
    {
        if (gameManager != null && (!gameManager.IsGameActive || gameManager.IsPaused))
            return;

        if (grassScroller != null)
            RefreshMotion();

        transform.position += speed * Time.deltaTime * moveDirection;

        if (Vector3.Dot(transform.position, moveDirection) > bottomThreshold)
        {
            if (spawnManager != null)
                spawnManager.OnTreeLeftScreen(this);
            else
                Destroy(gameObject);
        }
    }

    void RefreshMotion()
    {
        if (grassScroller == null)
            return;

        moveDirection = grassScroller.WorldMoveDirection;
        speed = grassScroller.WorldSpeed * treeSpeedMultiplier;
    }

    void UpdateBottomThreshold()
    {
        if (gameCamera == null)
            return;

        bottomThreshold = ScreenEdgeUtility.BottomAlongTravel(gameCamera, transform.position.y, moveDirection) + wrapMargin;
    }
}
