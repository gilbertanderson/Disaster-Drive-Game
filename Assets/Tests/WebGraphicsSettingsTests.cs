using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;

// Locks in the Web player's graphics and template configuration. These settings
// live in hand-edited YAML (ProjectSettings plus a full copy in every Web build
// profile), so a wrong enum encoding or an accidental revert would otherwise
// fail silently at build time — Unity would just ship a WebGL2-only build with
// the stock template. Asserting through the UnityEditor API makes CI the
// authoritative check of what a build would actually use.
public class WebGraphicsSettingsTests
{
    [Test]
    public void WebBuild_UsesExplicitGraphicsApis()
    {
        Assert.That(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL), Is.False,
            "Web graphics APIs should be set explicitly, not left on Automatic.");
    }

    [Test]
    public void WebBuild_TargetsWebGpu_WithWebGl2Fallback()
    {
        var apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);

        Assert.That(apis, Is.EqualTo(new[]
        {
            GraphicsDeviceType.WebGPU,
            GraphicsDeviceType.OpenGLES3,   // WebGL2 on the Web target
        }), "Web builds should try WebGPU first and fall back to WebGL2 on browsers without it.");
    }

    [Test]
    public void WebBuild_UsesDisasterDriveTemplate()
    {
        Assert.That(PlayerSettings.WebGL.template, Is.EqualTo("PROJECT:DisasterDrive"),
            "Web builds must use the DisasterDrive template (mobile orientation lock + rotate overlay).");
    }
}
