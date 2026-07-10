using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Headless build entry points for CI (GitHub Actions / command line).
/// Invoke with: Unity -batchmode -quit -projectPath . -executeMethod DisasterBuildAutomation.BuildWebGL
/// </summary>
public static class DisasterBuildAutomation
{
    private const string MainScenePath = "Assets/Scenes/My Game.unity";
    private const string WebGLOutputPath = "build/WebGL";

    [MenuItem("Disaster/Build/WebGL (Headless)")]
    public static void BuildWebGL()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { MainScenePath },
            locationPathName = WebGLOutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"WebGL build succeeded: {summary.totalSize / (1024 * 1024)} MB at {WebGLOutputPath}");
        }
        else
        {
            Debug.LogError($"WebGL build failed: {summary.result} with {summary.totalErrors} errors");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
