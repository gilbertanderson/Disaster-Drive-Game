using System;
using Unity.Play.Publisher.Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless "Publish to play.unity.com" entry point, mirroring what the
/// interactive Publish/WebGL Project window does, without opening a window.
/// Requires the machine's Editor to already be logged into a Unity ID
/// (UnityConnectSession.instance.GetAccessToken() must be non-empty) — there
/// is no way to supply that token non-interactively, which is why this can
/// only run on a machine with a cached interactive login (e.g. a self-hosted
/// CI runner), not a fresh hosted GitHub Actions runner.
///
/// Invoke with (note: no -quit — this method returns immediately and lets
/// EditorApplication.update keep pumping the upload's async work; the method
/// itself calls EditorApplication.Exit() once publish finishes or times out):
///   Unity -batchmode -projectPath . -executeMethod DisasterPublishAutomation.Publish
/// after a WebGL build already exists at build/WebGL (see DisasterBuildAutomation.BuildWebGL).
/// </summary>
public static class DisasterPublishAutomation
{
    private const string BuildOutputPath = "build/WebGL";
    private const string GameTitle = "Disaster Drive";
    private const double TimeoutSeconds = 10 * 60;

    private static Store<AppState> store;
    private static double startTime;

    [MenuItem("Disaster/Publish/WebGL to play.unity.com (Headless)")]
    public static void Publish()
    {
        var token = UnityConnectSession.instance.GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError(
                "Not logged into a Unity ID in this Editor session. " +
                "Open this Editor interactively once (Preferences > Unity ID) " +
                "and stay signed in before running headless publish.");
            ExitBatchMode(1);
            return;
        }

        if (!PublisherUtils.BuildIsValid(BuildOutputPath))
        {
            Debug.LogError($"No valid WebGL build found at '{BuildOutputPath}'. Run DisasterBuildAutomation.BuildWebGL first.");
            ExitBatchMode(1);
            return;
        }

        store = new Store<AppState>(PublisherReducer.Reducer, new AppState(), PublisherMiddleware.Create());
        startTime = EditorApplication.timeSinceStartup;

        // The upload is fully async (EditorCoroutineUtility + UnityWebRequest
        // callbacks), all driven off EditorApplication.update. If we're not
        // in batch mode (e.g. -executeMethod during interactive testing) that
        // loop is already running; in batch mode with no window open we must
        // keep it alive ourselves by not returning/quitting until this
        // callback signals completion.
        EditorApplication.update += PollState;
        store.Dispatch(new PublishStartAction { title = GameTitle, buildPath = BuildOutputPath });
    }

    private static void PollState()
    {
        var state = store.state;

        if (!string.IsNullOrEmpty(state.errorMsg))
        {
            Debug.LogError($"Publish failed: {state.errorMsg}");
            EditorApplication.update -= PollState;
            ExitBatchMode(1);
            return;
        }

        if (!string.IsNullOrEmpty(state.url))
        {
            Debug.Log($"Published successfully: {state.url}");
            EditorApplication.update -= PollState;
            ExitBatchMode(0);
            return;
        }

        if (EditorApplication.timeSinceStartup - startTime > TimeoutSeconds)
        {
            Debug.LogError("Publish timed out waiting for play.unity.com to finish processing the upload.");
            EditorApplication.update -= PollState;
            ExitBatchMode(1);
        }
    }

    private static void ExitBatchMode(int code)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(code);
        }
    }
}
