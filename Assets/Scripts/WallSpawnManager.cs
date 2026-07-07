using UnityEngine;

// Endless belt of decorative fence segments on the grass strips beside the road.
public class WallSpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject fencePrefab;
    [SerializeField] private GroundScroller grassScroller;
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private int segmentsPerSide = 7;
    [SerializeField] private float leftLaneZ = 11.5f;
    [SerializeField] private float rightLaneZ = -7.9f;
    [SerializeField] private float segmentY = 0.05f;
    [SerializeField] private float segmentScale = 2f;
    [SerializeField] private float spawnBackMargin = 2f;
    [SerializeField] private float segmentOverlap = 0.25f;

    private Transform segmentRoot;
    private GameManager gameManager;
    private Camera gameCamera;
    private float segmentSpacing = 4f;
    private bool spacingMeasured;

    public float SegmentSpacing
    {
        get
        {
            MeasureSegmentSpacing();
            return segmentSpacing;
        }
    }

    public float SpawnX => spawnX;

    void Awake()
    {
        if (grassScroller == null)
            grassScroller = GameObject.Find("Plane")?.GetComponent<GroundScroller>();
        segmentRoot = transform;
        MeasureSegmentSpacing();
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        gameCamera = Camera.main;
        PopulateSide(leftLaneZ);
        PopulateSide(rightLaneZ);
    }

    public void OnSegmentLeftScreen(WallScroller segment)
    {
        if (segment == null)
            return;

        if (gameManager != null && !gameManager.IsWorldAnimating)
            return;

        float laneZ = segment.LaneZ;
        Vector3 moveDir = grassScroller != null ? grassScroller.WorldMoveDirection : Vector3.left;

        if (TryGetLeadSegment(laneZ, segment, moveDir, out WallScroller lead))
        {
            Vector3 newPos = lead.transform.position - moveDir * segmentSpacing;
            segment.transform.position = new Vector3(newPos.x, segmentY, laneZ);
            return;
        }

        float respawnX = GetRespawnX(segment.transform.position.y);
        segment.transform.position = new Vector3(respawnX, segmentY, laneZ);
    }

    public float GetRespawnX(float segmentY)
    {
        if (gameCamera == null)
            gameCamera = Camera.main;

        if (gameCamera == null)
            return spawnX;

        Vector3 moveDir = grassScroller != null ? grassScroller.WorldMoveDirection : Vector3.left;
        float topAlongTravel = ScreenEdgeUtility.TopAlongTravel(gameCamera, segmentY, moveDir);
        Vector3 spawnWorld = new Vector3(spawnX, segmentY, 0f)
            + moveDir * (topAlongTravel - Vector3.Dot(new Vector3(spawnX, segmentY, 0f), moveDir));
        return spawnWorld.x;
    }

    void PopulateSide(float laneZ)
    {
        float backX = GetMinSpawnX();
        float minX = Mathf.Min(backX, spawnX);
        float maxX = Mathf.Max(backX, spawnX);
        minX = Mathf.Min(minX, spawnX - segmentSpacing * segmentsPerSide);

        for (float x = maxX; x >= minX; x -= segmentSpacing)
            SpawnSegment(laneZ, x);
    }

    void SpawnSegment(float laneZ, float x)
    {
        if (fencePrefab == null)
            return;

        Vector3 pos = new Vector3(x, segmentY, laneZ);
        GameObject segment = Instantiate(fencePrefab, pos, Quaternion.identity, segmentRoot);
        segment.transform.localScale = Vector3.one * segmentScale;

        WallScroller scroller = segment.GetComponent<WallScroller>();
        if (scroller == null)
            scroller = segment.AddComponent<WallScroller>();
        scroller.LaneZ = laneZ;
        scroller.Configure(grassScroller);
    }

    void MeasureSegmentSpacing()
    {
        if (fencePrefab == null || spacingMeasured)
            return;

        GameObject probe = Instantiate(fencePrefab, Vector3.down * 1000f, Quaternion.identity);
        probe.transform.localScale = Vector3.one * segmentScale;

        var renderers = probe.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);

            segmentSpacing = Mathf.Max(b.size.x - segmentOverlap, 0.5f);
            spacingMeasured = true;
        }

        Destroy(probe);
    }

    bool TryGetLeadSegment(float laneZ, WallScroller exclude, Vector3 moveDir, out WallScroller lead)
    {
        lead = null;
        float leadAlong = float.PositiveInfinity;
        const float laneEpsilon = 0.1f;

        foreach (var scroller in FindObjectsByType<WallScroller>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (scroller == exclude || scroller == null)
                continue;
            if (Mathf.Abs(scroller.LaneZ - laneZ) > laneEpsilon)
                continue;

            float along = Vector3.Dot(scroller.transform.position, moveDir);
            if (along < leadAlong)
            {
                leadAlong = along;
                lead = scroller;
            }
        }

        return lead != null;
    }

    float GetMinSpawnX()
    {
        if (gameCamera == null)
            return spawnX - segmentSpacing * segmentsPerSide;

        Vector3 moveDir = grassScroller != null ? grassScroller.WorldMoveDirection : Vector3.left;
        float bottomAlongTravel = ScreenEdgeUtility.BottomAlongTravel(gameCamera, segmentY, moveDir);
        float spawnAlongTravel = Vector3.Dot(new Vector3(spawnX, segmentY, 0f), moveDir);

        // Furthest back along the travel axis (screen bottom), not the minimum dot value.
        float backAlongTravel = Mathf.Max(bottomAlongTravel, spawnAlongTravel) + spawnBackMargin;
        Vector3 backWorld = new Vector3(spawnX, segmentY, 0f) + moveDir * (backAlongTravel - spawnAlongTravel);
        return backWorld.x;
    }
}
