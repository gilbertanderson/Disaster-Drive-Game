using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
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
        string yaml = File.ReadAllText(path).Replace("\r\n", "\n");

        Assert.That(yaml, Does.Contain("webGLTemplate: PROJECT:DisasterDrive"),
            $"{profileName}: profile's PlayerSettings copy must select the DisasterDrive template.");

        Assert.That(TryParseWebGlApiList(yaml, out var apis), Is.True,
            $"{profileName}: could not find an explicit WebGLSupport m_APIs list in the profile YAML.");
        Assert.That(apis, Is.EqualTo(new[]
        {
            (int)GraphicsDeviceType.WebGPU,       // 0x1c
            (int)GraphicsDeviceType.OpenGLES3,    // 0x0b — WebGL2
        }), $"{profileName}: profile must pin WebGPU then WebGL2 (got [{string.Join(", ", apis)}]).");
    }

    // Pulls the little-endian GraphicsDeviceType ints from the WebGLSupport
    // block's m_APIs hex blob, ignoring surrounding YAML quoting/indentation.
    static bool TryParseWebGlApiList(string yaml, out int[] apis)
    {
        apis = System.Array.Empty<int>();
        // Match WebGLSupport then the next m_APIs hex on a nearby line.
        var match = Regex.Match(
            yaml,
            @"m_BuildTarget:\s*WebGLSupport[\s\S]{0,400}?m_APIs:\s*([0-9a-fA-F]+)",
            RegexOptions.CultureInvariant);
        if (!match.Success)
            return false;

        string hex = match.Groups[1].Value;
        if (hex.Length < 8 || hex.Length % 8 != 0)
            return false;

        apis = new int[hex.Length / 8];
        for (int i = 0; i < apis.Length; i++)
        {
            string chunk = hex.Substring(i * 8, 8);
            // Serialized as little-endian bytes: "1c000000" → 0x1c
            string le = chunk.Substring(6, 2) + chunk.Substring(4, 2)
                + chunk.Substring(2, 2) + chunk.Substring(0, 2);
            if (!int.TryParse(le, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out apis[i]))
                return false;
        }

        return true;
    }
}
