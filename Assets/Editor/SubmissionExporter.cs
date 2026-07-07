#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SubmissionExporter
{
    const string PackageRelativePath = "Exports/GilbertAnderson_Disaster_Project4.unitypackage";
    const string BuildRelativePath = "Exports/Disaster_macOS.app";

    [MenuItem("Disaster/Export Submission Package")]
    public static void ExportPackage()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string exportDirectory = Path.Combine(projectRoot, "Exports");
        Directory.CreateDirectory(exportDirectory);

        string packagePath = Path.Combine(projectRoot, PackageRelativePath);
        string[] assetPaths =
        {
            "Assets",
            "Packages/manifest.json",
            "ProjectSettings"
        };

        AssetDatabase.ExportPackage(assetPaths, packagePath, ExportPackageOptions.Recurse);
        Debug.Log("Exported submission package to " + packagePath, AssetDatabase.LoadMainAssetAtPath("Assets"));
    }

    [MenuItem("Disaster/Build Submission Player (macOS)")]
    public static void BuildPlayer()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string exportDirectory = Path.Combine(projectRoot, "Exports");
        Directory.CreateDirectory(exportDirectory);

        string buildPath = Path.Combine(exportDirectory, "Disaster_macOS.app");
        var scenes = new[] { "Assets/Scenes/My Game.unity" };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            Debug.Log("Built submission player at " + buildPath);
        else
            Debug.LogError("Build failed: " + report.summary.result);
    }
}
#endif
