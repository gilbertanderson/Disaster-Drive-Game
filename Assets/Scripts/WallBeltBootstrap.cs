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

        foreach (Transform child in transform)
        {
            WallScroller scroller = child.GetComponent<WallScroller>();
            if (scroller == null)
                scroller = child.gameObject.AddComponent<WallScroller>();

            scroller.LaneZ = child.position.z;
            scroller.Configure(grassScroller);
        }
    }
}
