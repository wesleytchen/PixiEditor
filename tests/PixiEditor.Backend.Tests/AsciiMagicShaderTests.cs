using System.Reflection;
using PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Effects;
using SkiaSharp;
using Xunit;

namespace PixiEditor.Backend.Tests;

public class AsciiMagicShaderTests
{
    [Fact]
    public void EveryEmbeddedAsciiMagicShaderCompiles()
    {
        Assembly assembly = typeof(AsciiMagicAsciiTextNode).Assembly;
        string[] shaders = assembly.GetManifestResourceNames()
            .Where(name => name.Contains(".Shaders.AsciiMagic.") && name.EndsWith(".sksl"))
            .ToArray();

        Assert.Equal(10, shaders.Length);

        foreach (string shaderName in shaders)
        {
            using Stream stream = assembly.GetManifestResourceStream(shaderName)!;
            using StreamReader reader = new(stream);
            using SKRuntimeEffect effect = SKRuntimeEffect.CreateShader(reader.ReadToEnd(), out string errors);

            Assert.True(string.IsNullOrEmpty(errors), $"{shaderName} failed to compile: {errors}");
            Assert.NotNull(effect);
        }
    }
}
