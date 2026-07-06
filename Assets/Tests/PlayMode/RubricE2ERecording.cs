using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

/// <summary>
/// Captures Game View frames during Play Mode rubric E2E tests and encodes them to video.webm.
/// Output layout mirrors Playwright: TestResults/RubricE2E/&lt;scenario&gt;/video.webm
/// </summary>
public static class RubricE2ERecording
{
    static string activeScenario;
    static string framesDirectory;
    static int frameIndex;

    static bool VideoEnabled => RubricE2EVideoSettings.VideoEnabled;

    public static IEnumerator Begin(string scenarioName)
    {
        if (!VideoEnabled)
            yield break;

        activeScenario = scenarioName;
        framesDirectory = Path.Combine(RubricE2EVideoSettings.OutputRoot, scenarioName, "frames");
        frameIndex = 0;

        if (Directory.Exists(framesDirectory))
            Directory.Delete(framesDirectory, true);
        Directory.CreateDirectory(framesDirectory);

        yield return CaptureFrame();
    }

    public static IEnumerator CaptureForSeconds(float seconds)
    {
        if (!VideoEnabled)
            yield break;

        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end)
            yield return CaptureFrame();
    }

    public static IEnumerator CaptureFrame()
    {
        if (!VideoEnabled || string.IsNullOrEmpty(framesDirectory))
            yield break;

        yield return new WaitForEndOfFrame();

        string path = Path.Combine(framesDirectory, $"frame_{frameIndex:D5}.png");
        ScreenCapture.CaptureScreenshot(path);
        frameIndex++;
        yield return null;
    }

    public static IEnumerator End()
    {
        if (!VideoEnabled || string.IsNullOrEmpty(activeScenario))
            yield break;

        yield return CaptureFrame();

        string scenarioDir = Path.Combine(RubricE2EVideoSettings.OutputRoot, activeScenario);
        string videoPath = Path.Combine(scenarioDir, "video.webm");

#if UNITY_EDITOR
        bool encoded = TryEncodeVideo(framesDirectory, videoPath, 24);
        if (encoded)
            UnityEngine.Debug.Log($"Rubric E2E video saved: {videoPath}");
        else
            UnityEngine.Debug.LogWarning($"Rubric E2E frames saved (install ffmpeg for video): {framesDirectory}");
#else
        UnityEngine.Debug.Log($"Rubric E2E frames saved: {framesDirectory}");
#endif

        activeScenario = null;
        framesDirectory = null;
        frameIndex = 0;
    }

#if UNITY_EDITOR
    static bool TryEncodeVideo(string framesDirectory, string outputWebmPath, int frameRate)
    {
        if (!Directory.Exists(framesDirectory))
            return false;

        string ffmpeg = FindFfmpeg();
        if (ffmpeg == null)
            return false;

        string outputDir = Path.GetDirectoryName(outputWebmPath);
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        string inputPattern = Path.Combine(framesDirectory, "frame_%05d.png");
        string arguments =
            $"-y -framerate {frameRate} -i \"{inputPattern}\" -pix_fmt yuv420p -c:v libvpx-vp9 -b:v 1M \"{outputWebmPath}\"";

        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            return false;

        process.WaitForExit(120_000);
        return process.ExitCode == 0 && File.Exists(outputWebmPath);
    }

    static string FindFfmpeg()
    {
        string[] candidates =
        {
            "/opt/homebrew/bin/ffmpeg",
            "/usr/local/bin/ffmpeg",
            "/usr/bin/ffmpeg"
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        try
        {
            var which = new ProcessStartInfo
            {
                FileName = "/usr/bin/which",
                Arguments = "ffmpeg",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var process = Process.Start(which);
            if (process == null)
                return null;

            string path = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 && !string.IsNullOrEmpty(path) ? path : null;
        }
        catch
        {
            return null;
        }
    }
#endif
}
