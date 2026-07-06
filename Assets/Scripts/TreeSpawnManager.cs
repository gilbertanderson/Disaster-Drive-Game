using System.Collections;
using UnityEngine;

// Endless belt of decorative trees on the grass strips beside the road.
public class TreeSpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject[] treePrefabs;
    [SerializeField] private GroundScroller grassScroller;
    [SerializeField] private string wallsParentName = "Walls";
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private int targetTreeCount = 16;
    [SerializeField] private Vector2 leftStripZ = new Vector2(-16f, -11f);
    [SerializeField] private Vector2 rightStripZ = new Vector2(11f, 16f);
    [SerializeField] private Vector2 xJitter = new Vector2(-3f, 3f);
    [SerializeField] private float rockReferenceHeight = 2.7f;
    [SerializeField] private Vector2 treeHeightMultiplier = new Vector2(1.3f, 1.8f);
    [SerializeField] private float wallPadding = 0.6f;
    [SerializeField] private float spawnBackMargin = 2f;

    private Transform treeRoot;
    private GameManager gameManager;
    private Camera gameCamera;
    private float wallInnerLowZ = -10f;
    private float wallInnerHighZ = 10f;

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        gameCamera = Camera.main;
        if (grassScroller == null)
            grassScroller = GameObject.Find("Plane")?.GetComponent<GroundScroller>();
        FindWallRange();

        treeRoot = transform;
        StartCoroutine(PopulateWhenActive());
    }

    IEnumerator PopulateWhenActive()
    {
        yield return new WaitUntil(() => gameManager == null || gameManager.IsGameActive);
        float minX = GetMinSpawnX();
        while (treeRoot.childCount < targetTreeCount)
        {
            float z = RandomStripZ();
            float x = Random.Range(minX, spawnX + xJitter.y);
            SpawnTree(z, x);
        }
    }

    public void OnTreeLeftScreen(TreeScroller tree)
    {
        if (tree == null)
            return;

        float z = tree.transform.position.z;
        Destroy(tree.gameObject);

        if (gameManager != null && gameManager.IsWorldAnimating)
            SpawnTree(z, spawnX + Random.Range(xJitter.x, xJitter.y));
    }

    void SpawnTree(float z, float x)
    {
        if (treePrefabs == null || treePrefabs.Length == 0)
            return;

        GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
        if (prefab == null)
            return;

        float height = rockReferenceHeight * Random.Range(treeHeightMultiplier.x, treeHeightMultiplier.y);
        Vector3 pos = new Vector3(x, 0f, z);
        GameObject tree = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), treeRoot);
        ScaleTreeToHeight(tree, height);
        ConstrainTreeOutsideWalls(tree);

        TreeScroller scroller = tree.GetComponent<TreeScroller>();
        if (scroller == null)
            scroller = tree.AddComponent<TreeScroller>();
        scroller.Configure(grassScroller);
    }

    float GetMinSpawnX()
    {
        if (gameCamera == null)
            return spawnX + xJitter.x - spawnBackMargin * 8f;

        Vector3 moveDir = grassScroller != null ? grassScroller.WorldMoveDirection : Vector3.left;
        float bottomAlongTravel = ScreenEdgeUtility.BottomAlongTravel(gameCamera, treeRoot.position.y, moveDir);
        float spawnAlongTravel = Vector3.Dot(new Vector3(spawnX, 0f, 0f), moveDir);

        // Convert travel-axis bounds back to world X for this game's mostly -X travel.
        float minAlongTravel = Mathf.Min(bottomAlongTravel, spawnAlongTravel) - spawnBackMargin;
        Vector3 minWorld = new Vector3(spawnX, 0f, 0f) + moveDir * (minAlongTravel - spawnAlongTravel);
        return minWorld.x;
    }

    float RandomStripZ()
    {
        return Random.value < 0.5f
            ? Random.Range(leftStripZ.x, leftStripZ.y)
            : Random.Range(rightStripZ.x, rightStripZ.y);
    }

    void ScaleTreeToHeight(GameObject tree, float targetHeight)
    {
        var renderers = tree.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        if (b.size.y < 0.01f)
            return;

        float scale = targetHeight / b.size.y;
        tree.transform.localScale *= scale;
    }

    void FindWallRange()
    {
        GameObject wallsParent = GameObject.Find(wallsParentName);
        if (wallsParent == null || wallsParent.transform.childCount < 2)
            return;

        float lowZ = float.NegativeInfinity;
        float highZ = float.PositiveInfinity;
        foreach (Transform wall in wallsParent.transform)
        {
            float halfDepth = wall.localScale.z * 0.5f;
            if (wall.position.z < transform.position.z)
                lowZ = Mathf.Max(lowZ, wall.position.z + halfDepth);
            else
                highZ = Mathf.Min(highZ, wall.position.z - halfDepth);
        }

        wallInnerLowZ = lowZ;
        wallInnerHighZ = highZ;
    }

    void ConstrainTreeOutsideWalls(GameObject tree)
    {
        if (tree == null)
            return;

        var renderers = tree.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        float halfDepth = b.extents.z;
        Vector3 p = tree.transform.position;
        if (p.z < 0f)
        {
            // Mirror the right strip: keep trees on the outer grass band, away from the road edge.
            float maxZ = Mathf.Min(
                wallInnerLowZ - wallPadding - halfDepth,
                leftStripZ.y - wallPadding - halfDepth);
            p.z = Mathf.Min(p.z, maxZ);
            p.z = Mathf.Clamp(p.z, leftStripZ.x, leftStripZ.y);
        }
        else
        {
            float minZ = Mathf.Max(
                wallInnerHighZ + wallPadding + halfDepth,
                rightStripZ.x + wallPadding + halfDepth);
            p.z = Mathf.Max(p.z, minZ);
            p.z = Mathf.Clamp(p.z, rightStripZ.x, rightStripZ.y);
        }
        tree.transform.position = p;
    }
}
