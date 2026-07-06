using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Lets the player pick which vehicle model to drive (start screen arrows).
// Each entry in vehicleVisuals is a child model of the PlayerVehicle; exactly
// one is active at a time. The choice persists between sessions via PlayerPrefs.
public class VehicleSelector : MonoBehaviour
{
    private const string VehicleIndexKey = "VehicleIndex";

    [SerializeField] private GameObject[] vehicleVisuals;
    [SerializeField] private string[] vehicleNames;           // Optional display names; falls back to visual object names
    [SerializeField] private TMP_Text vehicleNameText;        // Shown on the start screen while cycling vehicles
    [SerializeField] private ParticleSystem[] dirtEmitters;   // Rear-tire dirt spray; repositioned to fit each vehicle
    [SerializeField] private bool showEmitterOrientationGizmos = true;
    [SerializeField] private float targetFootprint;  // 0 = auto median horizontal extent across all visuals

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

        CompactVehicleVisuals();
        NormalizeVisualScales();
        index = Mathf.Clamp(PlayerPrefs.GetInt(VehicleIndexKey, 0), 0, vehicleVisuals.Length - 1);
        NormalizeVisualPositions();
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

        bool driving = gameManager != null && gameManager.IsWorldAnimating;
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
        UpdateVehicleNameLabel();
    }

    void UpdateVehicleNameLabel()
    {
        if (vehicleNameText == null || vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        vehicleNameText.text = GetVehicleDisplayName(index);
    }

    string GetVehicleDisplayName(int vehicleIndex)
    {
        if (vehicleNames != null && vehicleIndex >= 0 && vehicleIndex < vehicleNames.Length
            && !string.IsNullOrWhiteSpace(vehicleNames[vehicleIndex]))
            return LimitToWordCount(vehicleNames[vehicleIndex].Trim(), 2);

        if (vehicleIndex < 0 || vehicleIndex >= vehicleVisuals.Length || vehicleVisuals[vehicleIndex] == null)
            return "Vehicle";

        return FormatVehicleName(vehicleVisuals[vehicleIndex].name);
    }

    public static string FormatVehicleName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return "Vehicle";

        if (TryGetCustomDisplayName(rawName, out string customName))
            return customName;

        string name = rawName;
        if (name.StartsWith("Veh_"))
            name = name.Substring(4);
        else if (name.StartsWith("SM_Veh_"))
            name = name.Substring(7);
        else if (name.StartsWith("Prefab_"))
            name = name.Substring(7);

        name = name.Replace('_', ' ');
        if (name.Contains("Convertable"))
            name = name.Replace("Convertable", "Convertible");

        if (name.EndsWith(" Z"))
            name = name.Substring(0, name.Length - 2).TrimEnd();

        if (name.Length > 3 && name.EndsWith(" 01"))
            name = name.Substring(0, name.Length - 3);

        name = TrimTrailingColorToken(name.Trim());
        return LimitToWordCount(name, 2);
    }

    static bool TryGetCustomDisplayName(string rawName, out string displayName)
    {
        switch (rawName)
        {
            case "Off-road vehicle":
                displayName = "Humvee";
                return true;
            case "Prefab_K-131":
                displayName = "Jeep";
                return true;
            case "Veh_Armor_Car_01":
                displayName = "Tank";
                return true;
            default:
                displayName = null;
                return false;
        }
    }

    static float GetVehicleScaleMultiplier(string rawName)
    {
        switch (rawName)
        {
            case "Veh_Armor_Car_01":
                return 1.5f;
            default:
                return 1f;
        }
    }

    static string TrimTrailingColorToken(string name)
    {
        string[] colors = { "Red", "Green", "Blue", "Black", "White", "Yellow", "Orange", "Grey", "Gray" };
        foreach (string color in colors)
        {
            if (name.EndsWith(" " + color, System.StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - color.Length - 1).TrimEnd();
        }
        return name;
    }

    static string LimitToWordCount(string name, int maxWords)
    {
        string[] parts = name.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= maxWords)
            return name.Trim();

        return string.Join(" ", parts, 0, maxWords);
    }

    // Drop null/missing entries left when a vehicle prefab is deleted but the array size is not compacted.
    void CompactVehicleVisuals()
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        var compacted = new List<GameObject>(vehicleVisuals.Length);
        foreach (var visual in vehicleVisuals)
        {
            if (visual != null)
                compacted.Add(visual);
        }

        if (compacted.Count == 0)
        {
            vehicleVisuals = System.Array.Empty<GameObject>();
            return;
        }

        if (compacted.Count != vehicleVisuals.Length)
            vehicleVisuals = compacted.ToArray();
    }

    // Scale each visual so its top-down footprint matches the fleet median (or targetFootprint).
    void NormalizeVisualScales()
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        var footprints = new List<float>(vehicleVisuals.Length);
        foreach (var visual in vehicleVisuals)
        {
            if (visual == null)
                continue;

            bool wasActive = visual.activeSelf;
            visual.SetActive(true);
            float footprint = MeasureFootprint(visual);
            if (footprint > 0.001f)
                footprints.Add(footprint);
            visual.SetActive(wasActive);
        }

        if (footprints.Count == 0)
            return;

        footprints.Sort();
        float target = targetFootprint > 0.001f
            ? targetFootprint
            : footprints[footprints.Count / 2];

        foreach (var visual in vehicleVisuals)
        {
            if (visual == null)
                continue;

            bool wasActive = visual.activeSelf;
            visual.SetActive(true);
            float footprint = MeasureFootprint(visual);
            if (footprint > 0.001f)
            {
                float scaleFactor = target / footprint;
                visual.transform.localScale *= scaleFactor * GetVehicleScaleMultiplier(visual.name);
            }
            visual.SetActive(wasActive);
        }
    }

    static float MeasureFootprint(GameObject visual)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return 0f;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return Mathf.Max(bounds.size.x, bounds.size.z);
    }

    public float GetVehicleFootprint(string visualObjectName)
    {
        if (vehicleVisuals == null || string.IsNullOrEmpty(visualObjectName))
            return 0f;

        foreach (var visual in vehicleVisuals)
        {
            if (visual == null || visual.name != visualObjectName)
                continue;

            bool wasActive = visual.activeSelf;
            visual.SetActive(true);
            float footprint = MeasureFootprint(visual);
            visual.SetActive(wasActive);
            return footprint;
        }

        return 0f;
    }

    // Imported vehicle prefabs ship with different pivots; align each model's ground center
    // to the player anchor so cycling vehicles does not shift the car on screen.
    void NormalizeVisualPositions()
    {
        foreach (var visual in vehicleVisuals)
        {
            if (visual == null)
                continue;

            bool wasActive = visual.activeSelf;
            visual.SetActive(true);
            AlignVisualToOrigin(visual);
            visual.SetActive(wasActive);
        }
    }

    void AlignVisualToOrigin(GameObject visual)
    {
        var renderers = visual.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 bottomCenterWorld = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 bottomCenterLocal = transform.InverseTransformPoint(bottomCenterWorld);
        visual.transform.localPosition -= bottomCenterLocal;
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

        // Emitters spray along local +X; align that axis with road travel. Left emitter rolls 180°
        // to mirror the authored cone on the right (confirmed visually in play mode).
        Vector3 moveDirection = groundScroller != null ? groundScroller.WorldMoveDirection : Vector3.left;
        Quaternion baseRotation = Quaternion.FromToRotation(Vector3.right, moveDirection);

        for (int i = 0; i < dirtEmitters.Length; i++)
        {
            if (dirtEmitters[i] == null) continue;
            float side = i == 0 ? -1f : 1f;
            dirtEmitters[i].transform.localPosition = new Vector3(rearX, groundY, box.center.z + side * halfTrack);
            dirtEmitters[i].transform.localRotation = side < 0f
                ? baseRotation * Quaternion.Euler(180f, 0f, 0f)
                : baseRotation;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showEmitterOrientationGizmos || dirtEmitters == null)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < dirtEmitters.Length; i++)
        {
            if (dirtEmitters[i] == null)
                continue;

            Vector3 origin = dirtEmitters[i].transform.position;
            Vector3 direction = dirtEmitters[i].transform.right;
            Gizmos.DrawLine(origin, origin + direction * 1.5f);
            Gizmos.DrawSphere(origin + direction * 1.5f, 0.05f);
        }
    }
}
