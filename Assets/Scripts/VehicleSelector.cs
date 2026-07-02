using UnityEngine;

// Lets the player pick which vehicle model to drive (start screen arrows).
// Each entry in vehicleVisuals is a child model of the PlayerVehicle; exactly
// one is active at a time. The choice persists between sessions via PlayerPrefs.
public class VehicleSelector : MonoBehaviour
{
    private const string VehicleIndexKey = "VehicleIndex";

    [SerializeField] private GameObject[] vehicleVisuals;
    [SerializeField] private ParticleSystem[] dirtEmitters;   // Rear-tire dirt spray; repositioned to fit each vehicle

    private int index;
    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();

        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        index = Mathf.Clamp(PlayerPrefs.GetInt(VehicleIndexKey, 0), 0, vehicleVisuals.Length - 1);
        Apply();
    }

    void Update()
    {
        // The dirt spray only runs while actually "driving" (mid-run, not paused).
        if (dirtEmitters == null)
            return;

        bool driving = gameManager != null && gameManager.IsGameActive && !gameManager.IsPaused;
        foreach (var emitter in dirtEmitters)
        {
            if (emitter == null) continue;
            if (driving && !emitter.isPlaying) emitter.Play();
            else if (!driving && emitter.isPlaying) emitter.Stop();
        }
    }

    // Wired to the start screen's ">" button.
    public void NextVehicle()
    {
        Cycle(1);
    }

    // Wired to the start screen's "<" button.
    public void PreviousVehicle()
    {
        Cycle(-1);
    }

    void Cycle(int direction)
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        index = (index + direction + vehicleVisuals.Length) % vehicleVisuals.Length;
        PlayerPrefs.SetInt(VehicleIndexKey, index);
        PlayerPrefs.Save();
        Apply();
    }

    void Apply()
    {
        for (int i = 0; i < vehicleVisuals.Length; i++)
            if (vehicleVisuals[i] != null)
                vehicleVisuals[i].SetActive(i == index);

        FitColliderToVisual();
    }

    // Size the hitbox to the visible car so rocks touch the model before they collide,
    // instead of bouncing off the old cube-sized box ("invisible wall").
    void FitColliderToVisual()
    {
        var box = GetComponent<BoxCollider>();
        var visual = vehicleVisuals[index];
        if (box == null || visual == null)
            return;

        var renderers = visual.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);

        // World-space bounds -> this object's local space (the vehicle never rotates)
        Vector3 lossy = transform.lossyScale;
        box.center = transform.InverseTransformPoint(b.center);
        box.size = new Vector3(b.size.x / lossy.x, b.size.y / lossy.y, b.size.z / lossy.z) * 0.95f;

        PositionDirtEmitters(box);
    }

    // Park the two dirt emitters just behind the rear corners of the fitted hitbox,
    // near ground level, so the spray lines up with whichever vehicle is selected.
    void PositionDirtEmitters(BoxCollider box)
    {
        if (dirtEmitters == null)
            return;

        float rearX = box.center.x - box.size.x * 0.5f - 0.2f;              // just behind the -X (down-screen) face
        float groundY = box.center.y - box.size.y * 0.5f + 0.15f;
        float halfTrack = box.size.z * 0.3f;                                // roughly where the rear tires sit

        for (int i = 0; i < dirtEmitters.Length; i++)
        {
            if (dirtEmitters[i] == null) continue;
            float side = i == 0 ? -1f : 1f;
            dirtEmitters[i].transform.localPosition = new Vector3(rearX, groundY, box.center.z + side * halfTrack);
        }
    }
}
