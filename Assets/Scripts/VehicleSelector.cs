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

    // Fallback only when no PlayerController is present (edit-mode tests).
    // Runtime player identity always comes from PlayerController.playerIndex.
    [SerializeField] private int playerIndex;
    [SerializeField] private VehicleSelector otherSelector;    // The other player's selector; the two may never match
    [SerializeField] private GameObject[] vehicleVisuals;
    [SerializeField] private string[] vehicleNames;           // Optional display names; falls back to visual object names
    [SerializeField] private TMP_Text vehicleNameText;        // Shown on the start screen while cycling vehicles
    [SerializeField] private ParticleSystem[] dirtEmitters;   // Rear-tire dirt spray; repositioned to fit each vehicle
    [SerializeField] private bool showEmitterOrientationGizmos = true;
    [SerializeField] private float targetFootprint;  // 0 = auto median horizontal extent across all visuals
    [SerializeField] private float[] speedMultipliers;
    [SerializeField] private float[] hitboxScaleMultipliers;
    [SerializeField] private float maxSteerAngleDegrees = 28f;     // Visual front-wheel lock angle
    [SerializeField] private float steerResponseDegPerSec = 220f;  // How fast the visual steer angle catches up to input


    private int index;
    private GameManager gameManager;
    private GroundScroller groundScroller;
    private PlayerController playerController;
    private Transform[] wheelTransforms = System.Array.Empty<Transform>();
    private float[] wheelRadii = System.Array.Empty<float>();
    private bool[] wheelIsFront = System.Array.Empty<bool>();
    private float[] wheelRollAngles = System.Array.Empty<float>();
    private Quaternion[] wheelBaseRotations = System.Array.Empty<Quaternion>();
    private float currentSteerAngle;

    public int CurrentIndex => index;

    // Single source of truth: PlayerController when present, else serialized fallback.
    int ResolvedPlayerIndex => playerController != null ? playerController.playerIndex : playerIndex;

    // P1 keeps the original key so an existing save still restores its vehicle.
    string PrefsKey => ResolvedPlayerIndex <= 0 ? VehicleIndexKey : VehicleIndexKey + (ResolvedPlayerIndex + 1);

    void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        if (groundScroller == null)
            groundScroller = FindAnyObjectByType<GroundScroller>();
        playerController = GetComponent<PlayerController>();

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
        DisableEmbeddedCameraRigs();
        EnsureStatTables();
        NormalizeVisualScales();
        index = Mathf.Clamp(PlayerPrefs.GetInt(PrefsKey, 0), 0, vehicleVisuals.Length - 1);
        NormalizeVisualPositions();
        Apply();
    }

    void Update()
    {
        // On the start screen, A/D and the arrow keys cycle vehicles like the < > buttons.
        // Gated to the start screen so leftover steering presses can't change the vehicle.
        // In two-player mode the key sets split to mirror the driving controls:
        // A/D cycles Player 1's vehicle, the arrows cycle Player 2's.
        if (gameManager != null && gameManager.IsOnStartScreen && Keyboard.current != null)
        {
            bool twoPlayer = gameManager.IsTwoPlayerMode;
            int who = ResolvedPlayerIndex;
            bool listenWasd = who == 0;
            bool listenArrows = twoPlayer ? who == 1 : who == 0;

            bool prevPressed = (listenWasd && Keyboard.current.aKey.wasPressedThisFrame)
                || (listenArrows && Keyboard.current.leftArrowKey.wasPressedThisFrame);
            bool nextPressed = (listenWasd && Keyboard.current.dKey.wasPressedThisFrame)
                || (listenArrows && Keyboard.current.rightArrowKey.wasPressedThisFrame);

            if (prevPressed)
                PreviousVehicle();
            else if (nextPressed)
                NextVehicle();
        }

        // Wheel roll/steer and the dirt spray both only run while actually "driving"
        // (mid-run or the post-game-over exit drive, not paused).
        bool driving = gameManager != null && gameManager.IsWorldAnimating;
        TickWheelSpin(Time.deltaTime, driving);

        if (dirtEmitters == null)
            return;

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

    void Cycle(int direction, bool persist = true)
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        // Step past any index the other player already holds so the two
        // selectors can never land on the same vehicle.
        int candidate = index;
        for (int step = 0; step < vehicleVisuals.Length; step++)
        {
            candidate = (candidate + direction + vehicleVisuals.Length) % vehicleVisuals.Length;
            if (!IsIndexTakenByOther(candidate))
                break;
        }

        if (candidate == index || IsIndexTakenByOther(candidate))
            return;   // Only one legal vehicle left; stay put

        index = candidate;
        // Auto-dedupe when enabling 2P must not rewrite the player's saved pick.
        if (persist)
        {
            PlayerPrefs.SetInt(PrefsKey, index);
            PlayerPrefs.Save();
        }
        Apply();
    }

    bool IsIndexTakenByOther(int candidate)
    {
        // An inactive other selector means single-player mode (Player 2 is disabled),
        // so every vehicle is available.
        if (otherSelector == null || !otherSelector.gameObject.activeInHierarchy)
            return false;

        return candidate == otherSelector.CurrentIndex;
    }

    // Called when two-player mode turns on: if this selector loaded the same
    // vehicle as the other player, advance to the next free one.
    public void EnsureDistinctFrom(VehicleSelector other)
    {
        if (other == null)
            return;

        otherSelector = other;
        if (other.otherSelector == null)
            other.otherSelector = this;

        if (vehicleVisuals == null || vehicleVisuals.Length < 2)
            return;

        if (index == other.CurrentIndex)
            Cycle(1, persist: false);
    }

    void Apply()
    {
        for (int i = 0; i < vehicleVisuals.Length; i++)
            if (vehicleVisuals[i] != null)
                vehicleVisuals[i].SetActive(i == index);

        // Re-center the active visual on the player pivot each switch so imported
        // meshes don't sit off the start-position center line.
        if (vehicleVisuals != null && index >= 0 && index < vehicleVisuals.Length
            && vehicleVisuals[index] != null)
            AlignVisualToOrigin(vehicleVisuals[index]);

        CacheWheels();
        FitColliderToVisual();
        ApplyVehicleStats();
        UpdateVehicleNameLabel();
    }

    public void ReapplyStats() => Apply();

    void ApplyVehicleStats()
    {
        float speedMul = GetStatMultiplier(speedMultipliers, 1f);
        var controller = GetComponent<PlayerController>();
        if (controller != null)
            controller.SetSpeedMultiplier(speedMul);
    }

    float GetStatMultiplier(float[] table, float fallback)
    {
        if (table == null || table.Length == 0)
            return fallback;
        int i = Mathf.Clamp(index, 0, table.Length - 1);
        return table[i] > 0.001f ? table[i] : fallback;
    }

    public void RefreshLabel() => UpdateVehicleNameLabel();

    public string CurrentVehicleDisplayName => GetVehicleDisplayName(index);

    void UpdateVehicleNameLabel()
    {
        if (vehicleNameText == null || vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        bool twoPlayer = gameManager != null && gameManager.IsTwoPlayerMode;
        int who = ResolvedPlayerIndex;
        string prefix = twoPlayer ? PlayerColors.GetLabel(who) + ": " : string.Empty;
        vehicleNameText.text = prefix + GetVehicleDisplayName(index);
        if (twoPlayer)
            vehicleNameText.color = PlayerColors.GetColor(who);
        else
            vehicleNameText.color = Color.white;
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
            case "SURVIVAL ARMORED TRUCK 1":
                displayName = "Armored Car";
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
                return 1.35f;
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

    // Some imported vehicle prefabs ship with their own camera rig (e.g. the armored
    // truck's "Main Camera" child with a Camera and AudioListener). Left enabled, that
    // camera takes over rendering the moment its vehicle is activated, replacing the
    // game's top-down view with a behind-the-vehicle one.
    void DisableEmbeddedCameraRigs()
    {
        foreach (var visual in vehicleVisuals)
        {
            if (visual == null)
                continue;

            foreach (var camera in visual.GetComponentsInChildren<Camera>(true))
                camera.enabled = false;
            foreach (var listener in visual.GetComponentsInChildren<AudioListener>(true))
                listener.enabled = false;
        }
    }

    void EnsureStatTables()
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        if (speedMultipliers == null || speedMultipliers.Length != vehicleVisuals.Length)
            speedMultipliers = BuildDefaultSpeedTable(vehicleVisuals.Length);
        if (hitboxScaleMultipliers == null || hitboxScaleMultipliers.Length != vehicleVisuals.Length)
            hitboxScaleMultipliers = BuildDefaultHitboxTable(vehicleVisuals.Length);
    }

    static float[] BuildDefaultSpeedTable(int count)
    {
        float[] defaults = { 0.92f, 1f, 1.08f, 0.88f, 1.05f };
        var table = new float[count];
        for (int i = 0; i < count; i++)
            table[i] = i < defaults.Length ? defaults[i] : 1f;
        return table;
    }

    static float[] BuildDefaultHitboxTable(int count)
    {
        float[] defaults = { 1.08f, 1f, 0.95f, 1.12f, 0.92f };
        var table = new float[count];
        for (int i = 0; i < count; i++)
            table[i] = i < defaults.Length ? defaults[i] : 1f;
        return table;
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
        // Prefer mesh geometry in the visual's local space so ParticleSystem /
        // Trail renderers (and skewed world AABBs) can't pull the car off-center.
        if (TryGetLocalMeshBounds(visual.transform, out Bounds localBounds))
        {
            Vector3 bottomCenterInVisual = new Vector3(
                localBounds.center.x, localBounds.min.y, localBounds.center.z);
            Vector3 bottomCenterWorld = visual.transform.TransformPoint(bottomCenterInVisual);
            Vector3 bottomCenterLocal = transform.InverseTransformPoint(bottomCenterWorld);
            visual.transform.localPosition -= bottomCenterLocal;
            return;
        }

        Bounds? bounds = null;
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>(false))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || !renderer.enabled)
                continue;
            if (bounds == null)
                bounds = renderer.bounds;
            else
            {
                Bounds b = bounds.Value;
                b.Encapsulate(renderer.bounds);
                bounds = b;
            }
        }

        if (bounds == null)
            return;

        Vector3 bottomCenterWorldFallback = new Vector3(
            bounds.Value.center.x, bounds.Value.min.y, bounds.Value.center.z);
        Vector3 bottomCenterLocalFallback = transform.InverseTransformPoint(bottomCenterWorldFallback);
        visual.transform.localPosition -= bottomCenterLocalFallback;
    }

    // Same corner-walk as SpawnManager: mesh bounds expressed in `root` local space.
    static bool TryGetLocalMeshBounds(Transform root, out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        foreach (var meshFilter in root.GetComponentsInChildren<MeshFilter>(false))
        {
            if (meshFilter.sharedMesh == null)
                continue;

            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            Matrix4x4 meshToRoot = root.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 extentSigns = new Vector3(
                    (corner & 1) == 0 ? -1f : 1f,
                    (corner & 2) == 0 ? -1f : 1f,
                    (corner & 4) == 0 ? -1f : 1f);
                Vector3 localCorner = meshToRoot.MultiplyPoint3x4(
                    meshBounds.center + Vector3.Scale(meshBounds.extents, extentSigns));

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
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
        float hitboxMul = GetStatMultiplier(hitboxScaleMultipliers, 1f);
        box.size = new Vector3(b.size.x / lossy.x, b.size.y / lossy.y, b.size.z / lossy.z) * 0.95f * hitboxMul;

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

    // Finds this visual's roll-able wheel meshes and caches each one's rolling radius and
    // front/rear classification, so per-frame spin (TickWheelSpin) never has to search the
    // hierarchy or touch bounds. Wheels are identified by name (contains "wheel") plus having
    // a MeshRenderer, which naturally excludes grouping-only nodes (e.g. the Humvee's "wheels"
    // transform) and non-mesh helpers; WheelCollider objects (e.g. the armored truck's unused
    // physics colliders) are excluded explicitly.
    void CacheWheels()
    {
        wheelTransforms = System.Array.Empty<Transform>();
        wheelRadii = System.Array.Empty<float>();
        wheelIsFront = System.Array.Empty<bool>();
        wheelRollAngles = System.Array.Empty<float>();
        wheelBaseRotations = System.Array.Empty<Quaternion>();
        currentSteerAngle = 0f;

        var visual = (vehicleVisuals != null && index >= 0 && index < vehicleVisuals.Length) ? vehicleVisuals[index] : null;
        if (visual == null)
            return;

        // The Jeep's wheel meshes don't roll convincingly with this generic spin logic
        // (confirmed visually), so it's excluded and left with static wheels.
        if (visual.name == "Prefab_K-131")
            return;

        var renderers = visual.GetComponentsInChildren<MeshRenderer>(false);
        var wheels = new List<Transform>(4);
        var radii = new List<float>(4);
        var zLocal = new List<float>(4);
        var baseRotations = new List<Quaternion>(4);

        foreach (var mr in renderers)
        {
            if (!mr.gameObject.name.ToLowerInvariant().Contains("wheel"))
                continue;
            if (mr.GetComponent<WheelCollider>() != null)
                continue;

            wheels.Add(mr.transform);
            radii.Add(EstimateWheelRadius(mr.bounds));
            baseRotations.Add(mr.transform.localRotation);
            // Some imported models (e.g. FBX packs authored in a Z-up tool) keep every
            // sub-mesh's Transform at the same local position and bake the real per-part
            // offset into the mesh's own vertex data instead. Bounds.center reflects the
            // actual geometric position either way; transform.position would not.
            zLocal.Add(visual.transform.InverseTransformPoint(mr.bounds.center).z);
        }

        if (wheels.Count == 0)
            return;

        float minZ = zLocal[0];
        float maxZ = zLocal[0];
        for (int i = 1; i < zLocal.Count; i++)
        {
            minZ = Mathf.Min(minZ, zLocal[i]);
            maxZ = Mathf.Max(maxZ, zLocal[i]);
        }
        float frontThreshold = (minZ + maxZ) * 0.5f;

        wheelTransforms = wheels.ToArray();
        wheelRadii = radii.ToArray();
        wheelBaseRotations = baseRotations.ToArray();
        wheelIsFront = new bool[wheels.Count];
        wheelRollAngles = new float[wheels.Count];
        for (int i = 0; i < wheels.Count; i++)
            wheelIsFront[i] = zLocal[i] > frontThreshold;
    }

    // Wheels stand vertically regardless of which way a given car model happens to face, so a
    // wheel mesh's world-space vertical extent is an orientation-agnostic stand-in for its
    // rolling diameter.
    static float EstimateWheelRadius(Bounds worldBounds)
    {
        float diameter = worldBounds.size.y;
        return diameter > 0.001f ? diameter * 0.5f : 0.001f;
    }

    // Pure math: how fast (degrees/sec) a wheel of the given radius must spin to roll along
    // the ground at worldSpeed without slipping. internal so it's directly unit-testable.
    internal static float WheelSpinDegreesPerSecond(float worldSpeed, float wheelRadius)
    {
        if (wheelRadius <= 0.0001f)
            return 0f;

        float circumference = 2f * Mathf.PI * wheelRadius;
        return (worldSpeed / circumference) * 360f;
    }

    // The world/props scroll toward the vehicle to simulate it driving forward, so the
    // vehicle's effective travel direction is the *opposite* of WorldMoveDirection. Projecting
    // that onto the visual root's local Z (the nose/tail axis, perpendicular to the local-X
    // axle) tells us whether the vehicle is effectively moving nose-first or not, which is what
    // determines which way the wheels must spin about their local-X axle to roll without
    // slipping. Deriving this from world scroll direction (instead of hardcoding a sign) keeps
    // it correct regardless of how any individual imported model is authored/oriented.
    float ComputeSpinSign(Transform visualRoot)
    {
        if (groundScroller == null || visualRoot == null)
            return 1f;

        float localZ = visualRoot.InverseTransformDirection(groundScroller.WorldMoveDirection).z;
        return localZ < 0f ? -1f : 1f;
    }

    // Advances wheel roll by deltaTime and steers the front wheels toward the player's current
    // input. internal + explicit deltaTime so edit-mode tests can call this directly and
    // deterministically without waiting on real frames.
    internal void TickWheelSpin(float deltaTime, bool driving)
    {
        if (!driving)
            return;
        if (wheelTransforms == null || wheelTransforms.Length == 0)
            return;
        if (groundScroller == null)
            return;

        var visual = (vehicleVisuals != null && index >= 0 && index < vehicleVisuals.Length) ? vehicleVisuals[index] : null;
        if (visual == null)
            return;

        float speed = groundScroller.WorldSpeed;
        float sign = ComputeSpinSign(visual.transform);

        float targetSteer = playerController != null
            ? Mathf.Clamp(playerController.SteerInput, -1f, 1f) * maxSteerAngleDegrees
            : 0f;
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteer, steerResponseDegPerSec * deltaTime);

        for (int i = 0; i < wheelTransforms.Length; i++)
        {
            var wheel = wheelTransforms[i];
            if (wheel == null)
                continue;

            float degrees = WheelSpinDegreesPerSecond(speed, wheelRadii[i]) * sign * deltaTime;
            wheelRollAngles[i] = Mathf.Repeat(wheelRollAngles[i] + degrees, 360f);

            // Roll happens in the mesh's own object space (innermost), then the baked
            // import-correction rotation (e.g. a Z-up->Y-up FBX fixup) re-orients that into
            // the parent's frame, then steer yaws the already-corrected wheel (outermost).
            // With an identity base rotation this collapses to the original formula exactly.
            Quaternion roll = wheelBaseRotations[i] * Quaternion.AngleAxis(wheelRollAngles[i], Vector3.right);
            wheel.localRotation = wheelIsFront[i]
                ? Quaternion.AngleAxis(currentSteerAngle, Vector3.up) * roll
                : roll;
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
