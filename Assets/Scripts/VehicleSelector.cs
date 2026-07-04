using UnityEngine;
using UnityEngine.InputSystem;

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
    private GroundScroller groundScroller;

    void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        groundScroller = FindAnyObjectByType<GroundScroller>();

        // Particle velocity must follow emitter rotation, which requires local simulation space.
        if (dirtEmitters != null)
        {
            foreach (var emitter in dirtEmitters)
            {
                if (emitter == null) continue;
                var main = emitter.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        index = Mathf.Clamp(PlayerPrefs.GetInt(VehicleIndexKey, 0), 0, vehicleVisuals.Length - 1);
        Apply();
    }

    void Update()
    {
        // On the start screen, A/D and the arrow keys cycle vehicles like the < > buttons.
        // Gated to the start screen so leftover steering presses can't change the vehicle.
        if (gameManager != null && gameManager.IsOnStartScreen && Keyboard.current != null)
        {
            if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame)
                PreviousVehicle();
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame)
                NextVehicle();
        }

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

        // Get the actual road direction from GroundScroller, fall back to down-screen if not found
        Vector3 moveDirection = groundScroller != null ? groundScroller.WorldMoveDirection : Vector3.left;

        for (int i = 0; i < dirtEmitters.Length; i++)
        {
            if (dirtEmitters[i] == null) continue;
            float side = i == 0 ? -1f : 1f;
            dirtEmitters[i].transform.localPosition = new Vector3(rearX, groundY, box.center.z + side * halfTrack);
            // Spray opposite the road movement so dirt trails behind the vehicle
            dirtEmitters[i].transform.localRotation = Quaternion.LookRotation(-moveDirection, Vector3.up);
        }
    }
}
