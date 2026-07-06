using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpawnManager : MonoBehaviour
{
    public GameObject[] obstacles;                // Gameplay shell prefab(s); rockVisuals swaps the mesh at spawn

    public float spawnX = 7.0f;                   // Travel-axis spawn line near the top of the field
    public string wallsParentName = "Walls";      // Parent whose children mark the lateral (Z) bounds
    public float lateralPadding = 0.5f;           // Keep spawns a little inside the walls so rocks don't clip them
    public int maxObstacles = 5;                  // Cap on how many rocks may exist at once

    [SerializeField] private float obstacleSpawnInterval = 2.2f;  // Seconds between spawns at the start
    [SerializeField] private float startDelay = 2.0f;             // Delay before the first spawn
    [SerializeField] private float minSpawnInterval = 0.65f;      // Floor so the difficulty ramp can't spawn absurdly fast
    [SerializeField] private int maxObstaclesCap = 14;            // Ceiling the ramp can raise maxObstacles to
    [SerializeField] private int obstaclesPerWaveIncrease = 2;    // Extra on-screen rock slots added each difficulty wave

    [Header("Rock Variety")]
    [SerializeField] private GameObject[] rockVisuals;  // Mesh prefabs swapped onto the shell (PolyOne, JC, etc.)
    [SerializeField] private Vector2 rockScaleRange = new Vector2(1.3f, 1.4f);  // Random size multiplier per rock
    [SerializeField] private float rockSpeedMultiplier = 2.25f;                  // Match decorative trees so rocks travel at the same world speed
    [SerializeField] private float minRockVisualFootprint = 1.2f;                // Drop prefab variants smaller than this at authored scale
    [SerializeField] private float minRockFootprint;  // 0 = auto-detect from convertible at runtime
    [SerializeField] private float minRockHeight = 1.8f;                         // Minimum world-space visual height after scaling
    [SerializeField] private float rockGroundBurialFraction = 0.2f;              // Fraction of rock height below the road surface
    [SerializeField] private float groundLevelY;                                 // 0 = auto-detect from Ground / GroundScroller at runtime

    private static readonly string[] KnownRockVisualPaths =
    {
        "Assets/Prefabs/RockVisuals/RockVisual_Boulder.prefab",
        "Assets/Prefabs/RockVisuals/RockVisual_Rocks_02.prefab",
        "Assets/Prefabs/RockVisuals/RockVisual_Rocks_03.prefab",
        "Assets/Prefabs/RockVisuals/RockVisual_Rocks_04.prefab",
        "Assets/Prefabs/RockVisuals/RockVisual_Rocks_05.prefab",
        "Assets/Prefabs/RockVisuals/RockVisual_Rocks_09.prefab",
    };

    private float currentInterval;                // Live spawn interval; shrinks as difficulty ramps
    private bool isSpawning = true;
    private float minZ = -5.0f;                   // Lateral spawn range between the walls (recomputed from the walls in Start)
    private float maxZ = 10.0f;
    private GameManager gameManager;              // Rocks only flow while the game is active
    private GroundScroller groundScroller;        // Used to sync rock movement with the decorative trees
    private GameObject[] validRockVisuals;        // Cached, spawn-safe prefab list
    private float cachedGroundLevelY;             // Road surface Y used for burial alignment

    void OnValidate()
    {
#if UNITY_EDITOR
        RepairRockVisualReferences();
#endif
    }

    void Start()
    {
#if UNITY_EDITOR
        RepairRockVisualReferences();
#endif
        validRockVisuals = BuildValidRockVisualList();

        currentInterval = obstacleSpawnInterval;
        gameManager = FindAnyObjectByType<GameManager>();
        groundScroller = FindAnyObjectByType<GroundScroller>();
        CacheGroundLevelY();
        FindWallRange();
        CacheMinRockFootprint();
        StartCoroutine(SpawnLoop());
    }

    void CacheGroundLevelY()
    {
        if (groundLevelY > 0.001f)
        {
            cachedGroundLevelY = groundLevelY;
            return;
        }

        var ground = GameObject.Find("Ground");
        if (ground != null)
        {
            cachedGroundLevelY = ground.transform.position.y;
            return;
        }

        if (groundScroller != null)
        {
            cachedGroundLevelY = groundScroller.transform.position.y;
            return;
        }

        cachedGroundLevelY = 0.06f;
    }

    void CacheMinRockFootprint()
    {
        if (minRockFootprint > 0.001f)
            return;

        var selector = FindAnyObjectByType<VehicleSelector>();
        if (selector != null)
            minRockFootprint = selector.GetVehicleFootprint("SM_Veh_Convertable_01");
    }

    // Coroutine instead of InvokeRepeating so the interval can shrink over time
    // and spawning can be stopped cleanly at game over.
    IEnumerator SpawnLoop()
    {
        // Wait on the start screen until the player presses Start (skipped if no GameManager exists).
        yield return new WaitUntil(() => gameManager == null || gameManager.IsGameActive);
        yield return new WaitForSeconds(startDelay);
        while (isSpawning)
        {
            SpawnObstacle();
            yield return new WaitForSeconds(currentInterval);
        }
    }

    // Called by GameManager on each difficulty bump: rocks spawn sooner and
    // slightly more of them are allowed on the field at once.
    public void IncreaseDifficulty(float intervalMultiplier)
    {
        currentInterval = Mathf.Max(currentInterval * intervalMultiplier, minSpawnInterval);
        maxObstacles = Mathf.Min(maxObstacles + obstaclesPerWaveIncrease, maxObstaclesCap);
    }

    // Called by GameManager when the timer runs out.
    public void StopSpawning()
    {
        isSpawning = false;
    }

    void SpawnObstacle()
    {
        if (obstacles == null || obstacles.Length == 0)
            return;

        if (MoveDown.ActiveRockCount >= maxObstacles)
            return;

        try
        {
            SpawnObstacleInternal();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SpawnManager failed to spawn a rock: {ex.Message}", this);
        }
    }

    void SpawnObstacleInternal()
    {
        float sizeMultiplier = Random.Range(rockScaleRange.x, rockScaleRange.y);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPos = new Vector3(spawnX, cachedGroundLevelY, randomZ);

        GameObject rock;
        GameObject visual = null;
        if (HasUsableRockVisuals())
        {
            if (obstacles[0] == null)
                return;
            rock = Instantiate(obstacles[0], spawnPos, obstacles[0].transform.rotation);
            visual = ApplyRandomVisual(rock);
        }
        else
        {
            int obstacleIndex = Random.Range(0, obstacles.Length);
            if (obstacles[obstacleIndex] == null)
                return;
            rock = Instantiate(obstacles[obstacleIndex], spawnPos, obstacles[obstacleIndex].transform.rotation);
            visual = GetRockVisual(rock);
        }

        rock.transform.localScale *= sizeMultiplier;
        EnforceMinimumRockFootprint(rock);
        EnforceMinimumRockHeight(rock);

        if (visual != null)
        {
            AlignVisualToGround(visual, rock);
            FitSphereColliderToVisual(rock, visual);
        }

        ConfigureRockShadows(rock);

        MoveDown mover = rock.GetComponentInChildren<MoveDown>();
        if (mover != null)
        {
            float scrollSpeed = groundScroller != null
                ? groundScroller.PropScrollSpeed(rockSpeedMultiplier)
                : mover.speed;
            mover.speed = Mathf.Max(1.5f, scrollSpeed);
        }
    }

    GameObject ApplyRandomVisual(GameObject rock)
    {
        GameObject visualPrefab = PickRandomRockVisual();
        if (visualPrefab == null)
            return GetRockVisual(rock);

        GameObject visual = TryInstantiateVisual(visualPrefab, rock.transform);
        if (visual == null)
            return GetRockVisual(rock);

        for (int i = rock.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = rock.transform.GetChild(i).gameObject;
            if (child != visual)
                Destroy(child);
        }

        Vector3 prefabLocalPos = visual.transform.localPosition;
        visual.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        visual.transform.localPosition = prefabLocalPos;

        foreach (var childCollider in visual.GetComponentsInChildren<Collider>())
            childCollider.enabled = false;

        return visual;
    }

    GameObject TryInstantiateVisual(GameObject visualPrefab, Transform parent)
    {
        try
        {
            return Instantiate(visualPrefab, parent);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"SpawnManager skipped invalid rock visual '{visualPrefab.name}': {ex.Message}", this);
            return null;
        }
    }

    static GameObject GetRockVisual(GameObject rock)
    {
        if (rock.transform.childCount > 0)
            return rock.transform.GetChild(0).gameObject;
        return rock;
    }

    static float MeasureRockFootprint(GameObject rock)
    {
        var renderers = rock.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    void EnforceMinimumRockFootprint(GameObject rock)
    {
        if (minRockFootprint <= 0.001f)
            return;

        float footprint = MeasureRockFootprint(rock);
        if (footprint > 0.001f && footprint < minRockFootprint)
            rock.transform.localScale *= minRockFootprint / footprint;
    }

    static float MeasureRockHeight(GameObject rock)
    {
        var renderers = rock.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds.size.y;
    }

    void EnforceMinimumRockHeight(GameObject rock)
    {
        if (minRockHeight <= 0.001f)
            return;

        float height = MeasureRockHeight(rock);
        if (height > 0.001f && height < minRockHeight)
            rock.transform.localScale *= minRockHeight / height;
    }

    bool HasUsableRockVisuals()
    {
        if (validRockVisuals == null)
            validRockVisuals = BuildValidRockVisualList();
        return validRockVisuals.Length > 0;
    }

    GameObject PickRandomRockVisual()
    {
        if (validRockVisuals == null || validRockVisuals.Length == 0)
            validRockVisuals = BuildValidRockVisualList();
        if (validRockVisuals.Length == 0)
            return null;
        return validRockVisuals[Random.Range(0, validRockVisuals.Length)];
    }

    GameObject[] BuildValidRockVisualList()
    {
        var candidates = new List<GameObject>();
        if (rockVisuals != null)
        {
            foreach (var entry in rockVisuals)
            {
                if (TryGetRockVisualPrefab(entry, out GameObject prefab) && !candidates.Contains(prefab))
                    candidates.Add(prefab);
            }
        }

        if (candidates.Count == 0)
        {
            foreach (var path in KnownRockVisualPaths)
            {
                var loaded = LoadRockVisualAtPath(path);
                if (loaded != null && !candidates.Contains(loaded))
                    candidates.Add(loaded);
            }
        }

        return FilterRockVisualsByFootprint(candidates);
    }

    GameObject[] FilterRockVisualsByFootprint(List<GameObject> candidates)
    {
        if (minRockVisualFootprint <= 0.001f)
            return candidates.ToArray();

        var valid = new List<GameObject>();
        var excluded = new List<string>();
        foreach (var prefab in candidates)
        {
            if (prefab == null)
                continue;

            float footprint = MeasurePrefabFootprint(prefab);
            if (footprint >= minRockVisualFootprint)
                valid.Add(prefab);
            else
                excluded.Add($"{prefab.name} ({footprint:F2})");
        }

        if (excluded.Count > 0)
            Debug.Log($"SpawnManager excluded small rock visuals (min footprint {minRockVisualFootprint:F2}): {string.Join(", ", excluded)}", this);

        if (valid.Count == 0 && candidates.Count > 0)
        {
            Debug.LogWarning("SpawnManager: all rock visuals were below minRockVisualFootprint; keeping largest candidate.", this);
            GameObject largest = candidates[0];
            float largestFootprint = MeasurePrefabFootprint(largest);
            for (int i = 1; i < candidates.Count; i++)
            {
                float footprint = MeasurePrefabFootprint(candidates[i]);
                if (footprint > largestFootprint)
                {
                    largest = candidates[i];
                    largestFootprint = footprint;
                }
            }
            valid.Add(largest);
        }

        return valid.ToArray();
    }

    static float MeasurePrefabFootprint(GameObject prefab)
    {
        if (prefab == null)
            return 0f;

        float maxFootprint = 0f;
        foreach (var meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            Vector3 scaledSize = Vector3.Scale(meshFilter.sharedMesh.bounds.size, GetHierarchyScale(meshFilter.transform));
            maxFootprint = Mathf.Max(maxFootprint, Mathf.Max(scaledSize.x, scaledSize.z));
        }

        if (maxFootprint > 0.001f)
            return maxFootprint;

        if (!Application.isPlaying)
            return 0f;

        GameObject instance = Instantiate(prefab);
        try
        {
            return MeasureRockFootprint(instance);
        }
        finally
        {
            Destroy(instance);
        }
    }

    static Vector3 GetHierarchyScale(Transform transform)
    {
        Vector3 scale = transform.localScale;
        Transform parent = transform.parent;
        while (parent != null)
        {
            scale = Vector3.Scale(scale, parent.localScale);
            parent = parent.parent;
        }

        return scale;
    }

    static bool TryGetRockVisualPrefab(Object entry, out GameObject prefab)
    {
        prefab = null;
        if (entry == null)
            return false;

        if (entry is GameObject gameObject)
        {
            prefab = gameObject;
            return true;
        }

#if UNITY_EDITOR
        string path = AssetDatabase.GetAssetPath(entry);
        if (!string.IsNullOrEmpty(path))
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null;
        }
#endif
        return false;
    }

    static GameObject LoadRockVisualAtPath(string path)
    {
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    void RepairRockVisualReferences()
    {
        if (rockVisuals == null || rockVisuals.Length == 0)
        {
            rockVisuals = LoadAllKnownRockVisuals();
            EditorUtility.SetDirty(this);
            return;
        }
        var serializedObject = new SerializedObject(this);
        SerializedProperty arrayProperty = serializedObject.FindProperty("rockVisuals");
        if (arrayProperty == null || !arrayProperty.isArray)
            return;

        var repaired = new List<GameObject>();
        bool changed = false;
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
            Object current = element.objectReferenceValue;
            if (TryGetRockVisualPrefab(current, out GameObject prefab))
            {
                if (!repaired.Contains(prefab))
                    repaired.Add(prefab);
                if (current != prefab)
                {
                    element.objectReferenceValue = prefab;
                    changed = true;
                }
                continue;
            }

            changed = true;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(current, out string guid, out long _))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (loaded != null && !repaired.Contains(loaded))
                    {
                        repaired.Add(loaded);
                        element.objectReferenceValue = loaded;
                        continue;
                    }
                }
            }
        }

        if (repaired.Count == 0)
            repaired.AddRange(LoadAllKnownRockVisuals());

        if (changed || repaired.Count != rockVisuals.Length)
        {
            rockVisuals = repaired.ToArray();
            serializedObject.Update();
            arrayProperty.arraySize = rockVisuals.Length;
            for (int i = 0; i < rockVisuals.Length; i++)
                arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue = rockVisuals[i];
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(this);
        }
        else
        {
            rockVisuals = repaired.ToArray();
        }
    }

    GameObject[] LoadAllKnownRockVisuals()
    {
        var loaded = new List<GameObject>();
        foreach (var path in KnownRockVisualPaths)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && !loaded.Contains(prefab))
                loaded.Add(prefab);
        }

        return loaded.ToArray();
    }
#endif

    void AlignVisualToGround(GameObject visual, GameObject rock)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float targetBottomY = cachedGroundLevelY - rockGroundBurialFraction * bounds.size.y;
        rock.transform.position += Vector3.up * (targetBottomY - bounds.min.y);
    }

    static void ConfigureRockShadows(GameObject rock)
    {
        foreach (var renderer in rock.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    void FitSphereColliderToVisual(GameObject rock, GameObject visual)
    {
        var collider = rock.GetComponent<SphereCollider>();
        if (collider == null)
            return;

        var renderers = visual.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        collider.center = rock.transform.InverseTransformPoint(bounds.center);
        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        float uniformScale = Mathf.Max(rock.transform.lossyScale.x, 0.001f);
        collider.radius = maxExtent / uniformScale;
    }

    void FindWallRange()
    {
        if (WallBoundsUtility.TryGetPaddedRange(wallsParentName, transform.position, lateralPadding, out minZ, out maxZ))
            return;

        minZ = -5.0f;
        maxZ = 10.0f;
    }
}
