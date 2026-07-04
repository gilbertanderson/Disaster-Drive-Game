using UnityEngine;

// Scrolls the ground texture so the road appears to move under the vehicle,
// matching the direction the rocks travel. Only scrolls while a run is active
// (and not paused), so the road is still on the start/game over screens.
[RequireComponent(typeof(Renderer))]
public class GroundScroller : MonoBehaviour
{
    [SerializeField] private Vector2 scrollDirection = new Vector2(0f, -1f); // UV-space direction that reads as "moving down the screen"
    [SerializeField] private float scrollSpeed = 5f;                         // UV units per second; higher = faster road; synchronized with worldScrollSpeed
    [SerializeField] private float worldScrollSpeed = 5f;                      // World units/sec for props (trees, rocks via MoveDown)

    public float WorldSpeed { get; private set; }
    public Vector3 WorldMoveDirection { get; private set; } = Vector3.left;

    private Material groundMaterial;   // Runtime instance — the asset on disk is not modified
    private Vector2 offset;
    private GameManager gameManager;
    private bool useBaseMap;           // URP Lit uses _BaseMap instead of _MainTex

    void Awake()
    {
        Vector2 uvDir = scrollDirection.sqrMagnitude > 0.0001f
            ? scrollDirection.normalized
            : Vector2.left;
        WorldMoveDirection = new Vector3(uvDir.x, 0f, uvDir.y).normalized;
        WorldSpeed = worldScrollSpeed;
    }

    void Start()
    {
        groundMaterial = GetComponent<Renderer>().material;
        gameManager = FindAnyObjectByType<GameManager>();
        useBaseMap = groundMaterial.HasProperty("_BaseMap");
    }

    void Update()
    {
        if (gameManager != null && (!gameManager.IsGameActive || gameManager.IsPaused))
            return;

        offset += scrollSpeed * Time.deltaTime * scrollDirection.normalized;
        if (useBaseMap)
            groundMaterial.SetTextureOffset("_BaseMap", offset);
        else
            groundMaterial.mainTextureOffset = offset;
    }
}
