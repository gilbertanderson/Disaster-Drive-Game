using UnityEngine;

// Moves a decorative tree down-screen at the same world speed as the grass scroll.
public class TreeScroller : MonoBehaviour
{
    public float speed = 2.5f;
    public Camera gameCamera;
    public float wrapMargin = 2f;

    private Vector3 moveDirection = Vector3.left;
    private float bottomThreshold;
    private GameManager gameManager;
    private TreeSpawnManager spawnManager;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        spawnManager = FindAnyObjectByType<TreeSpawnManager>();

        if (gameCamera == null)
            gameCamera = Camera.main;

        float planeDistance = gameCamera != null
            ? Mathf.Abs(transform.position.y - gameCamera.transform.position.y)
            : 10f;
        Vector3 bottomWorld = gameCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, planeDistance));
        bottomThreshold = bottomWorld.x - wrapMargin;
    }

    void Update()
    {
        if (gameManager != null && (!gameManager.IsGameActive || gameManager.IsPaused))
            return;

        transform.position += speed * Time.deltaTime * moveDirection;

        if (transform.position.x < bottomThreshold)
        {
            if (spawnManager != null)
                spawnManager.OnTreeLeftScreen(this);
            else
                Destroy(gameObject);
        }
    }
}
