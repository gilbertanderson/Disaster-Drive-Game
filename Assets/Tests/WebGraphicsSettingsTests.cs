using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
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

    // The UnityEditor API above reads only the global ProjectSettings.asset.
    // Each Web build profile embeds its own full PlayerSettings copy, and a
    // profile-driven build (Unity Build Automation, the deployment path) uses
    // that copy — so the profiles must carry the same graphics/template config
    // or a deploy silently drops WebGPU and the custom template while CI stays
    // green. The overrides are serialized as quoted "- line:" YAML, which no
    // editor API exposes, so assert on the file text directly.
    private static readonly string[] WebBuildProfiles =
    {
        "Web",
        "Web - Desktop - Development",
        "Web - Desktop - Release",
        "Web - Mobile - Development",
        "Web - Mobile - Release",
    };

    [Test]
    public void WebBuildProfiles_CarryWebGpuApis_AndDisasterDriveTemplate(
        [ValueSource(nameof(WebBuildProfiles))] string profileName)
    {
        string path = Path.Combine(Application.dataPath, "Settings", "Build Profiles", profileName + ".asset");
        Assert.That(File.Exists(path), Is.True, $"Build profile not found: {path}");
        // Normalize line endings so a CRLF checkout can't break the
        // multi-line containment assertion below.
        string yaml = File.ReadAllText(path).Replace("\r\n", "\n");

        Assert.That(yaml, Does.Contain(
            "    - line: '|   - m_BuildTarget: WebGLSupport'\n" +
            "    - line: '|     m_APIs: 1c0000000b000000'\n" +
            "    - line: '|     m_Automatic: 0'"),
            $"{profileName}: profile's PlayerSettings copy must pin the Web graphics APIs to WebGPU (0x1c) + WebGL2 (0x0b).");
        Assert.That(yaml, Does.Contain("    - line: '|   webGLTemplate: PROJECT:DisasterDrive'"),
            $"{profileName}: profile's PlayerSettings copy must select the DisasterDrive template.");
    }
}
