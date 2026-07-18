using NUnit.Framework;
using UnityEngine;

public class WallBoundsUtilityTests
{
    private GameObject wallsParent;

    [TearDown]
    public void TearDown()
    {
        if (wallsParent != null)
            Object.DestroyImmediate(wallsParent);
    }

    [Test]
    public void TryGetPaddedRange_ReturnsFalseWhenOnlyOneWallSideHasCollider()
    {
        wallsParent = new GameObject("OneSidedWalls");
        CreateWall("LowWall", new Vector3(0f, 0f, -5f), new Vector3(1f, 1f, 2f));

        bool found = WallBoundsUtility.TryGetPaddedRange(
            wallsParent.name, Vector3.zero, 0.5f, out float minZ, out float maxZ);

        Assert.That(found, Is.False,
            "A single wall must not replace callers' serialized fallback bounds.");
        Assert.That(minZ, Is.EqualTo(0f));
        Assert.That(maxZ, Is.EqualTo(0f));
    }

    [Test]
    public void TryGetPaddedRange_UsesInnerFacesWhenBothWallSidesHaveColliders()
    {
        wallsParent = new GameObject("CompleteWalls");
        CreateWall("LowWall", new Vector3(0f, 0f, -5f), new Vector3(1f, 1f, 2f));
        CreateWall("HighWall", new Vector3(0f, 0f, 7f), new Vector3(1f, 1f, 4f));

        bool found = WallBoundsUtility.TryGetPaddedRange(
            wallsParent.name, Vector3.zero, 0.5f, out float minZ, out float maxZ);

        Assert.That(found, Is.True);
        Assert.That(minZ, Is.EqualTo(-3.5f).Within(0.001f),
            "Low inner face is wall center plus half-depth, then padded inward.");
        Assert.That(maxZ, Is.EqualTo(4.5f).Within(0.001f),
            "High inner face is wall center minus half-depth, then padded inward.");
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(wallsParent.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.AddComponent<BoxCollider>();
    }
}
