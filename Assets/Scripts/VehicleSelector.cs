using UnityEngine;

// Lets the player pick which vehicle model to drive (start screen arrows).
// Each entry in vehicleVisuals is a child model of the PlayerVehicle; exactly
// one is active at a time. The choice persists between sessions via PlayerPrefs.
public class VehicleSelector : MonoBehaviour
{
    private const string VehicleIndexKey = "VehicleIndex";

    [SerializeField] private GameObject[] vehicleVisuals;

    private int index;

    void Awake()
    {
        if (vehicleVisuals == null || vehicleVisuals.Length == 0)
            return;

        index = Mathf.Clamp(PlayerPrefs.GetInt(VehicleIndexKey, 0), 0, vehicleVisuals.Length - 1);
        Apply();
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
    }
}
