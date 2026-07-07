#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class RubricE2EVideoMenu
{
    const string MenuPath = "Disaster/Run Rubric E2E with Video";

    [MenuItem(MenuPath)]
    public static void RunRubricE2EWithVideo()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Rubric E2E", "Stop Play Mode before running rubric E2E tests.", "OK");
            return;
        }

        RubricE2EVideoSettings.VideoEnabled = true;

        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var filter = new Filter
        {
            testMode = TestMode.PlayMode,
            assemblyNames = new[] { "DisasterPlayModeTests" }
        };

        api.RegisterCallbacks(new RunCallbacks());
        api.Execute(new ExecutionSettings(filter));
        Debug.Log("Rubric E2E with video started. Videos land in TestResults/RubricE2E/<scenario>/video.webm");
    }

    sealed class RunCallbacks : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun) { }

        public void RunFinished(ITestResultAdaptor result)
        {
            RubricE2EVideoSettings.VideoEnabled = false;

            int passed = result.PassCount;
            int failed = result.FailCount;
            int total = passed + failed + result.SkipCount;
            string outputRoot = RubricE2EVideoSettings.OutputRoot;

            Debug.Log($"Rubric E2E finished: {passed}/{total} passed, {failed} failed. Review videos in {outputRoot}");

            if (failed > 0)
                EditorUtility.DisplayDialog(
                    "Rubric E2E",
                    $"{failed} test(s) failed. Check the Console and Test Runner for details.\n\nVideos: {outputRoot}",
                    "OK");
            else
                EditorUtility.DisplayDialog(
                    "Rubric E2E",
                    $"All {passed} rubric E2E test(s) passed.\n\nOpen videos:\n{outputRoot}",
                    "OK");
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (!result.HasChildren && result.TestStatus == TestStatus.Failed)
                Debug.LogError($"Rubric E2E failed: {result.FullName}\n{result.Message}");
        }
    }
}
#endif
