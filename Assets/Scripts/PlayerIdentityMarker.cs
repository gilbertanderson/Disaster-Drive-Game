using TMPro;
using UnityEngine;

// Billboard label + colored ring above each vehicle so players can tell cars apart in 2P.
public class PlayerIdentityMarker : MonoBehaviour
{
    [SerializeField] private float heightOffset = 1.6f;
    [SerializeField] private float ringRadius = 0.55f;
    [SerializeField] private float ringThickness = 0.08f;

    private GameManager gameManager;
    private Transform markerRoot;
    private int playerIndex;

    void Awake()
    {
        var owner = GetComponentInParent<PlayerController>();
        if (owner != null)
            playerIndex = owner.playerIndex;

        BuildVisuals();
    }

    void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        RefreshVisibility();
    }

    void BuildVisuals()
    {
        markerRoot = new GameObject("IdentityMarker").transform;
        markerRoot.SetParent(transform, false);
        markerRoot.localPosition = Vector3.up * heightOffset;

        var ringGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ringGo.name = "ColorRing";
        ringGo.transform.SetParent(markerRoot, false);
        ringGo.transform.localScale = new Vector3(ringRadius * 2f, ringThickness, ringRadius * 2f);
        ringGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var ringCollider = ringGo.GetComponent<Collider>();
        if (ringCollider != null)
            Destroy(ringCollider);

        var ringRenderer = ringGo.GetComponent<Renderer>();
        if (ringRenderer != null)
        {
            ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            ringRenderer.material.color = PlayerColors.GetColor(playerIndex);
        }

        var labelGo = new GameObject("PlayerLabel");
        labelGo.transform.SetParent(markerRoot, false);
        labelGo.transform.localPosition = Vector3.up * 0.35f;
        var label = labelGo.AddComponent<TextMeshPro>();
        label.text = PlayerColors.GetLabel(playerIndex);
        label.fontSize = 4f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = PlayerColors.GetColor(playerIndex);
        label.fontStyle = FontStyles.Bold;
        label.rectTransform.sizeDelta = new Vector2(2f, 1f);
    }

    void LateUpdate()
    {
        RefreshVisibility();
        if (markerRoot == null || !markerRoot.gameObject.activeSelf)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        markerRoot.rotation = Quaternion.LookRotation(cam.transform.forward, cam.transform.up);
    }

    void RefreshVisibility()
    {
        if (markerRoot == null)
            return;

        bool show = gameManager != null && gameManager.IsTwoPlayerMode && gameManager.IsGameActive;
        markerRoot.gameObject.SetActive(show);
    }
}
