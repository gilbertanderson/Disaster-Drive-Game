using System.IO;
using UnityEngine;

/// <summary>
/// Shared rubric E2E video toggle and output path (Game assembly so Editor menu can reference it).
/// </summary>
public static class RubricE2EVideoSettings
{
    public static bool VideoEnabled { get; set; }

    public static string OutputRoot =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", "TestResults", "RubricE2E"));
}
