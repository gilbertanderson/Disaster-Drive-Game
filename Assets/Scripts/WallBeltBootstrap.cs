using System.Collections.Generic;
using UnityEngine;

// Attaches WallScroller to existing stonewall segments placed in the scene.
[DefaultExecutionOrder(-50)]
public class WallBeltBootstrap : MonoBehaviour
{
    [SerializeField] private GroundScroller grassScroller;

    void Awake()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf)
                child.gameObject.SetActive(true);
        }
    }

    void Start()
    {
        if (grassScroller == null)
            grassScroller = GameObject.Find("Plane")?.GetComponent<GroundScroller>();

        RedistributeSegments();

        foreach (Transform child in transform)
        {
            WallScroller scroller = child.GetComponent<WallScroller>();
            if (scroller == null)
                scroller = child.gameObject.AddComponent<WallScroller>();

            scroller.LaneZ = child.position.z;
            scroller.Configure(grassScroller);
        }
    }

    void RedistributeSegments()
    {
        if (grassScroller == null)
            return;

        var spawnManager = FindAnyObjectByType<WallSpawnManager>(FindObjectsInactive.Include);
        if (spawnManager == null)
            return;

        var segments = new List<Transform>();
        foreach (Transform child in transform)
            segments.Add(child);

        if (segments.Count == 0)
            return;

        float spacing = spawnManager.SegmentSpacing;
        float frontX = spawnManager.SpawnX;
        Vector3 moveDir = grassScroller.WorldMoveDirection;
        float laneZ = segments[0].position.z;
        float y = segments[0].position.y;
        Vector3 frontBase = new Vector3(frontX, y, laneZ);

        segments.Sort((a, b) =>
            Vector3.Dot(a.position, moveDir).CompareTo(Vector3.Dot(b.position, moveDir)));

        for (int i = 0; i < segments.Count; i++)
            segments[i].position = frontBase + moveDir * (i * spacing);
    }
}
