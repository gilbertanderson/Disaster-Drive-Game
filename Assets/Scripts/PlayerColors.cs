using UnityEngine;

public static class PlayerColors
{
    public static readonly Color P1 = new Color(0.35f, 0.65f, 1f);
    public static readonly Color P2 = new Color(1f, 0.55f, 0.15f);
    public static readonly Color PenaltyRed = new Color(0.95f, 0.25f, 0.2f);
    public static readonly Color BonusGreen = new Color(0.3f, 0.95f, 0.4f);

    public static Color GetColor(int playerIndex) => playerIndex == 1 ? P2 : P1;

    public static string GetLabel(int playerIndex) => playerIndex == 1 ? "P2" : "P1";

    public static string GetFullLabel(int playerIndex) => playerIndex == 1 ? "PLAYER 2" : "PLAYER 1";
}
