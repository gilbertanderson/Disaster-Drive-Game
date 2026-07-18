using NUnit.Framework;
using UnityEngine;

public class HillBackdropMirrorTests
{
    private GameObject backdrop;

    [SetUp]
    public void SetUp()
    {
        backdrop = new GameObject("IntroBackdrop");
        CreateSource("Hill_F1", new Vector3(-38f, -1.5f, -42f));
        CreateSource("Hill_B1", new Vector3(-52f, -2f, -55f));
        CreateSource("FarGround", new Vector3(-45f, -0.55f, 0f));
        CreateSource("UnrelatedDecoration", new Vector3(-10f, 0f, 0f));
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(backdrop);
    }

    [Test]
    public void MirrorBackdrop_CopiesHillsAndGroundAcrossX()
    {
        Transform group = HillBackdropMirror.MirrorBackdrop(backdrop.transform);

        Assert.That(group, Is.Not.Null);
        Assert.That(group.childCount, Is.EqualTo(3));
        AssertMirrored(group, "Hill_F1_Top", new Vector3(38f, -1.5f, -42f));
        AssertMirrored(group, "Hill_B1_Top", new Vector3(52f, -2f, -55f));
        AssertMirrored(group, "FarGround_Top", new Vector3(45f, -0.55f, 0f));
        Assert.That(group.Find("UnrelatedDecoration_Top"), Is.Null);
    }

    [Test]
    public void MirrorBackdrop_IsIdempotent()
    {
        Transform first = HillBackdropMirror.MirrorBackdrop(backdrop.transform);
        Transform second = HillBackdropMirror.MirrorBackdrop(backdrop.transform);

        Assert.That(second, Is.SameAs(first));
        Assert.That(backdrop.transform.childCount, Is.EqualTo(5));
    }

    private void CreateSource(string objectName, Vector3 localPosition)
    {
        var source = new GameObject(objectName);
        source.transform.SetParent(backdrop.transform, false);
        source.transform.localPosition = localPosition;
    }

    private static void AssertMirrored(Transform group, string childName, Vector3 expected)
    {
        Transform child = group.Find(childName);
        Assert.That(child, Is.Not.Null);
        Assert.That(child.localPosition, Is.EqualTo(expected).Using(Vector3EqualityComparer.Instance));
    }
}
