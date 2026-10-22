using System.Reflection;
using Drawie.Backend.Core;
using Drawie.Backend.Core.Numerics;
using Drawie.Backend.Core.Shaders;
using Drawie.Backend.Core.Surfaces;
using Drawie.Backend.Core.Surfaces.PaintImpl;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Graph.Interfaces;
using PixiEditor.ChangeableDocument.Rendering;

namespace PixiEditor.ChangeableDocument.Changeables.Graph.Nodes.Effects;

/// <summary>
/// Shared renderer for the native ASCII Magic effect-family nodes.
/// The SkSL programs are embedded so a saved document does not depend on loose files or an extension.
/// </summary>
public abstract class AsciiMagicNode<TEffect> : RenderNode, IRenderInput where TEffect : struct, Enum
{
    public RenderInputProperty Background { get; }
    public InputProperty<TEffect> Effect { get; }
    public InputProperty<double> Amount { get; }
    public InputProperty<double> Scale { get; }
    public InputProperty<double> Seed { get; }

    private readonly string shaderCode;
    private readonly Paint paint = new() { BlendMode = BlendMode.Src };
    private Shader? shader;
    private Shader? imageShader;

    protected override bool ExecuteOnlyOnCacheChange => true;
    protected override CacheTriggerFlags CacheTrigger => CacheTriggerFlags.All;

    protected AsciiMagicNode(string shaderFileName, TEffect defaultEffect)
    {
        Background = CreateRenderInput("Background", "BACKGROUND");
        Effect = CreateInput("Effect", "EFFECT", defaultEffect);
        Amount = CreateInput("Amount", "AMOUNT", 0d).WithRules(v => v.Min(-1).Max(1));
        Scale = CreateInput("Scale", "SCALE", 0d).WithRules(v => v.Min(-2).Max(3));
        Seed = CreateInput("Seed", "SEED", 0d).WithRules(v => v.Min(0).Max(1000));

        shaderCode = LoadShader(shaderFileName);
        Output.FirstInChain = null;
        RendersInAbsoluteCoordinates = true;
    }

    protected override void OnExecute(RenderContext context)
    {
        base.OnExecute(context);
        shader?.Dispose();
        shader = Shader.Create(shaderCode, CreateUniforms(context), out _);
        paint.Shader = shader;
    }

    protected override void OnPaint(RenderContext context, Canvas surface)
    {
        if (Background.Value is null || shader is null)
            return;

        using Texture texture = Texture.ForProcessing(surface, context.ProcessingColorSpace);
        Background.Value.Paint(context, texture.DrawingSurface.Canvas);
        using var snapshot = texture.DrawingSurface.Snapshot();

        imageShader?.Dispose();
        imageShader = snapshot.ToShader();
        shader = shader.WithUpdatedUniforms(CreateUniforms(context));
        paint.Shader = shader;

        int savedTexture = texture.DrawingSurface.Canvas.Save();
        texture.DrawingSurface.Canvas.SetMatrix(Matrix3X3.Identity);
        texture.DrawingSurface.Canvas.DrawRect(0, 0, context.RenderOutputSize.X, context.RenderOutputSize.Y, paint);
        texture.DrawingSurface.Canvas.RestoreToCount(savedTexture);

        int savedSurface = surface.Save();
        surface.SetMatrix(Matrix3X3.Identity);
        surface.DrawSurface(texture.DrawingSurface, 0, 0);
        surface.RestoreToCount(savedSurface);
    }

    private Uniforms CreateUniforms(RenderContext context)
    {
        Uniforms uniforms = new();
        uniforms.Add("iImage", new Uniform("iImage", imageShader));
        uniforms.Add("iResolution", new Uniform("iResolution", (VecD)context.RenderOutputSize));
        uniforms.Add("iNormalizedTime", new Uniform("iNormalizedTime", (float)context.FrameTime.NormalizedTime));
        uniforms.Add("Effect", new Uniform("Effect", Convert.ToInt32(Effect.Value)));
        uniforms.Add("Amount", new Uniform("Amount", (float)Amount.Value));
        uniforms.Add("Scale", new Uniform("Scale", (float)Scale.Value));
        uniforms.Add("Seed", new Uniform("Seed", (float)Seed.Value));
        return uniforms;
    }

    private static string LoadShader(string shaderFileName)
    {
        Assembly assembly = typeof(AsciiMagicNode<>).Assembly;
        string resourceName = $"PixiEditor.ChangeableDocument.Shaders.AsciiMagic.{shaderFileName}";
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded ASCII Magic shader '{shaderFileName}' was not found.");
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }
}

[NodeInfo("AsciiMagicAsciiText")]
public sealed class AsciiMagicAsciiTextNode() : AsciiMagicNode<AsciiTextEffect>("ascii-text.sksl", AsciiTextEffect.Characters)
{
    public override Node CreateCopy() => new AsciiMagicAsciiTextNode();
}

[NodeInfo("AsciiMagicPixelBlocks")]
public sealed class AsciiMagicPixelBlocksNode() : AsciiMagicNode<PixelBlocksEffect>("pixel-blocks.sksl", PixelBlocksEffect.PixelArt)
{
    public override Node CreateCopy() => new AsciiMagicPixelBlocksNode();
}

[NodeInfo("AsciiMagicPrintPaper")]
public sealed class AsciiMagicPrintPaperNode() : AsciiMagicNode<PrintPaperEffect>("print-paper.sksl", PrintPaperEffect.Dither)
{
    public override Node CreateCopy() => new AsciiMagicPrintPaperNode();
}

[NodeInfo("AsciiMagicGeometric")]
public sealed class AsciiMagicGeometricNode() : AsciiMagicNode<GeometricEffect>("geometric.sksl", GeometricEffect.Cross)
{
    public override Node CreateCopy() => new AsciiMagicGeometricNode();
}

[NodeInfo("AsciiMagicDistort")]
public sealed class AsciiMagicDistortNode() : AsciiMagicNode<DistortEffect>("distort.sksl", DistortEffect.Smudge)
{
    public override Node CreateCopy() => new AsciiMagicDistortNode();
}

[NodeInfo("AsciiMagicBlurFocus")]
public sealed class AsciiMagicBlurFocusNode() : AsciiMagicNode<BlurFocusEffect>("blur-focus.sksl", BlurFocusEffect.MotionBlur)
{
    public override Node CreateCopy() => new AsciiMagicBlurFocusNode();
}

[NodeInfo("AsciiMagicGlitchSignal")]
public sealed class AsciiMagicGlitchSignalNode() : AsciiMagicNode<GlitchSignalEffect>("glitch-signal.sksl", GlitchSignalEffect.CRTScreen)
{
    public override Node CreateCopy() => new AsciiMagicGlitchSignalNode();
}

[NodeInfo("AsciiMagicLightColor")]
public sealed class AsciiMagicLightColorNode() : AsciiMagicNode<LightColorEffect>("light-color.sksl", LightColorEffect.Disco)
{
    public override Node CreateCopy() => new AsciiMagicLightColorNode();
}

[NodeInfo("AsciiMagicGlass")]
public sealed class AsciiMagicGlassNode() : AsciiMagicNode<GlassEffect>("glass.sksl", GlassEffect.FrostedGlass)
{
    public override Node CreateCopy() => new AsciiMagicGlassNode();
}

[NodeInfo("AsciiMagicMaterialTexture")]
public sealed class AsciiMagicMaterialTextureNode() : AsciiMagicNode<MaterialTextureEffect>("material-texture.sksl", MaterialTextureEffect.Waves)
{
    public override Node CreateCopy() => new AsciiMagicMaterialTextureNode();
}

public enum AsciiTextEffect { Characters, ASCIIStudio, Block, Dots, Mixed, Braille }
public enum PixelBlocksEffect { PixelArt, Mosaic, LEGO, Voxel, HexMosaic, LEDMatrix }
public enum PrintPaperEffect { Dither, Stippling, Risograph, Comic, Sketch, Engraving, Warhol, Blueprint, Halftone, Woodblock, Posterize, Mezzotint, Pointillize, Crosshatch, CMYKDrops, Cyanotype, HoneycombScreen, CrumpledPrint }
public enum GeometricEffect { Cross, Diamond, Lines, Diagonal, Specimen, LowPoly, Droste, Truchet, IsoExtrude, FlowField, Lattice, Topography, Forms, Schematic, NeonGrid }
public enum DistortEffect { Smudge, Pinch, Spherize, Twirl, ZigZag, Ripple, Polar, Shear }
public enum BlurFocusEffect { MotionBlur, TiltShift, GaussianBlur, RadialBlur }
public enum GlitchSignalEffect { CRTScreen, PixelSort, Forensics, DigitalDistortion, Datamosh, Lofi, RGBSplit, Scanline, Thermal, Beacon }
public enum LightColorEffect { Disco, Anaglyph3D, GradientMap, Prism, Dreamdust, LightWind, Kaleidoscope, Warpbloom, Duotone, EdgeGlow, Aberration }
public enum GlassEffect { FrostedGlass, TintedGlass, Shatter, Vitrine, GlassOrb, FlutedGlass, CrossReeded, Hammered, DiamondGlass, Hexagon, RippleGlass, IceCrackle, ChevronPrism, SeedyBubbles, GlassPixel, Jello, Glass }
public enum MaterialTextureEffect { Waves, OilPaint, Watercolor, Chrome, ThinFilm, Marble, MoltenMetal, Crystallize, Emboss, PetrolSheen, HeatTint }
