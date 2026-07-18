using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Mirrors the scene-authored lower/back hill cluster across the ground plane so
// the top/spawn edge has the same layered backdrop. The sources remain the
// single authoring point for positions, scales, meshes, and materials.
public static class HillBackdropMirror
{
    private const string BackdropName = "IntroBackdrop";
    private const string RuntimeGroupName = "HillsTopRuntime";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GameObject backdrop = GameObject.Find(BackdropName);
        if (backdrop != null)
            MirrorBackdrop(backdrop.transform);
    }

    internal static Transform MirrorBackdrop(Transform backdrop)
    {
        if (backdrop == null)
            return null;

        Transform existing = backdrop.Find(RuntimeGroupName);
        if (existing != null)
            return existing;

        var sources = new List<GameObject>();
        for (int i = 0; i < backdrop.childCount; i++)
        {
            GameObject child = backdrop.GetChild(i).gameObject;
            if (child.name.StartsWith("Hill_F")
                || child.name.StartsWith("Hill_B")
                || child.name == "FarGround")
            {
                sources.Add(child);
            }
        }

        var group = new GameObject(RuntimeGroupName);
        group.transform.SetParent(backdrop, false);

        foreach (GameObject source in sources)
        {
            GameObject copy = Object.Instantiate(source, group.transform, false);
            copy.name = source.name + "_Top";
            Vector3 mirroredPosition = copy.transform.localPosition;
            mirroredPosition.x = -mirroredPosition.x;
            copy.transform.localPosition = mirroredPosition;
        }

        return group.transform;
    }
}
