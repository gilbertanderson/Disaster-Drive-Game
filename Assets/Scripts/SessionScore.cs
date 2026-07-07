public static class SessionScore
{
    public static int WinsP1;
    public static int WinsP2;

    public static void RecordWin(int playerIndex)
    {
        if (playerIndex == 1)
            WinsP2++;
        else
            WinsP1++;
    }

    public static string FormatLine() => $"Wins this session: P1 {WinsP1} – {WinsP2} P2";
}
