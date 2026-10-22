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
    public InputProperty<double> Strength { get; }
    public InputProperty<double> Size { get; }
    public InputProperty<double> Opacity { get; }
    public InputProperty<AsciiMagicPalette> Palette { get; }
    public InputProperty<AsciiMagicChroma> Chroma { get; }
    public InputProperty<double> Brightness { get; }
    public InputProperty<double> Contrast { get; }
    public InputProperty<bool> InvertColors { get; }

    private readonly string shaderCode;
    private readonly Paint paint = new() { BlendMode = BlendMode.Src };
    private readonly double sizeAtNormalScale;
    private Shader? shader;
    private Shader? imageShader;

    protected override bool ExecuteOnlyOnCacheChange => true;
    protected override CacheTriggerFlags CacheTrigger => CacheTriggerFlags.All;

    protected AsciiMagicNode(string shaderFileName, TEffect defaultEffect, string sizeLabel,
        double defaultSize, double sizeAtNormalScale, double minimumSize, double maximumSize)
    {
        Background = CreateRenderInput("Background", "BACKGROUND");
        Effect = CreateInput("Effect", "ASCII_MAGIC_FILTER", defaultEffect);
        Strength = CreateInput("Amount", "ASCII_MAGIC_STRENGTH", 100d).WithRules(v => v.Min(0).Max(100));
        Size = CreateInput("Scale", sizeLabel, defaultSize).WithRules(v => v.Min(minimumSize).Max(maximumSize));
        Opacity = CreateInput("Opacity", "ASCII_MAGIC_OPACITY", 100d).WithRules(v => v.Min(0).Max(100));
        Palette = CreateInput("Palette", "ASCII_MAGIC_PALETTE", AsciiMagicPalette.OriginalColors);
        Chroma = CreateInput("Chroma", "ASCII_MAGIC_CHROMA", AsciiMagicChroma.Standard);
        Brightness = CreateInput("Brightness", "ASCII_MAGIC_BRIGHTNESS", 0d).WithRules(v => v.Min(-100).Max(100));
        Contrast = CreateInput("Contrast", "ASCII_MAGIC_CONTRAST", 0d).WithRules(v => v.Min(-100).Max(100));
        InvertColors = CreateInput("InvertColors", "ASCII_MAGIC_INVERT_COLORS", false);
        this.sizeAtNormalScale = sizeAtNormalScale;

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
        // The shaders use compact technical ranges internally. The node deliberately exposes
        // familiar percentages and pixel sizes instead, and keeps procedural variation stable.
        uniforms.Add("Amount", new Uniform("Amount", (float)(Strength.Value / 100d - 1d)));
        uniforms.Add("Scale", new Uniform("Scale", (float)Math.Log2(Size.Value / sizeAtNormalScale)));
        uniforms.Add("Seed", new Uniform("Seed", 0f));
        uniforms.Add("Opacity", new Uniform("Opacity", (float)(Opacity.Value / 100d)));
        uniforms.Add("Palette", new Uniform("Palette", Convert.ToInt32(Palette.Value)));
        uniforms.Add("Chroma", new Uniform("Chroma", Convert.ToInt32(Chroma.Value)));
        uniforms.Add("Brightness", new Uniform("Brightness", (float)(Brightness.Value / 100d)));
        uniforms.Add("Contrast", new Uniform("Contrast", (float)(Contrast.Value / 100d)));
        uniforms.Add("InvertColors", new Uniform("InvertColors", InvertColors.Value ? 1 : 0));
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
public sealed class AsciiMagicAsciiTextNode() : AsciiMagicNode<AsciiTextEffect>("ascii-text.sksl", AsciiTextEffect.Characters,
    "ASCII_MAGIC_CHARACTER_SIZE", 11, 9, 2, 72)
{
    public override Node CreateCopy() => new AsciiMagicAsciiTextNode();
}

[NodeInfo("AsciiMagicPixelBlocks")]
public sealed class AsciiMagicPixelBlocksNode() : AsciiMagicNode<PixelBlocksEffect>("pixel-blocks.sksl", PixelBlocksEffect.PixelArt,
    "ASCII_MAGIC_BLOCK_SIZE", 9, 9, 2, 96)
{
    public override Node CreateCopy() => new AsciiMagicPixelBlocksNode();
}

[NodeInfo("AsciiMagicPrintPaper")]
public sealed class AsciiMagicPrintPaperNode() : AsciiMagicNode<PrintPaperEffect>("print-paper.sksl", PrintPaperEffect.Dither,
    "ASCII_MAGIC_DOT_SIZE", 6, 6, 2, 64)
{
    public override Node CreateCopy() => new AsciiMagicPrintPaperNode();
}

[NodeInfo("AsciiMagicGeometric")]
public sealed class AsciiMagicGeometricNode() : AsciiMagicNode<GeometricEffect>("geometric.sksl", GeometricEffect.Cross,
    "ASCII_MAGIC_PATTERN_SIZE", 18, 18, 4, 144)
{
    public override Node CreateCopy() => new AsciiMagicGeometricNode();
}

[NodeInfo("AsciiMagicDistort")]
public sealed class AsciiMagicDistortNode() : AsciiMagicNode<DistortEffect>("distort.sksl", DistortEffect.Smudge,
    "ASCII_MAGIC_DISTORTION_SCALE", 100, 100, 25, 400)
{
    public override Node CreateCopy() => new AsciiMagicDistortNode();
}

[NodeInfo("AsciiMagicBlurFocus")]
public sealed class AsciiMagicBlurFocusNode() : AsciiMagicNode<BlurFocusEffect>("blur-focus.sksl", BlurFocusEffect.MotionBlur,
    "ASCII_MAGIC_BLUR_RADIUS", 10, 10, 1, 80)
{
    public override Node CreateCopy() => new AsciiMagicBlurFocusNode();
}

[NodeInfo("AsciiMagicGlitchSignal")]
public sealed class AsciiMagicGlitchSignalNode() : AsciiMagicNode<GlitchSignalEffect>("glitch-signal.sksl", GlitchSignalEffect.CRTScreen,
    "ASCII_MAGIC_GLITCH_SIZE", 100, 100, 25, 400)
{
    public override Node CreateCopy() => new AsciiMagicGlitchSignalNode();
}

[NodeInfo("AsciiMagicLightColor")]
public sealed class AsciiMagicLightColorNode() : AsciiMagicNode<LightColorEffect>("light-color.sksl", LightColorEffect.Disco,
    "ASCII_MAGIC_PATTERN_SCALE", 100, 100, 25, 400)
{
    public override Node CreateCopy() => new AsciiMagicLightColorNode();
}

[NodeInfo("AsciiMagicGlass")]
public sealed class AsciiMagicGlassNode() : AsciiMagicNode<GlassEffect>("glass.sksl", GlassEffect.FrostedGlass,
    "ASCII_MAGIC_GLASS_PATTERN_SIZE", 100, 100, 25, 400)
{
    public override Node CreateCopy() => new AsciiMagicGlassNode();
}

[NodeInfo("AsciiMagicMaterialTexture")]
public sealed class AsciiMagicMaterialTextureNode() : AsciiMagicNode<MaterialTextureEffect>("material-texture.sksl", MaterialTextureEffect.Waves,
    "ASCII_MAGIC_TEXTURE_SIZE", 100, 100, 25, 400)
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

public enum AsciiMagicPalette
{
    OriginalColors,
    Mono1Bit,
    Gray4Levels,
    Gray8Levels,
    GameBoy,
    CGAPalette0,
    CGAPalette1,
    Pico8,
    Cyberpunk,
    Pastel,
    Risograph,
    Sepia
}

public enum AsciiMagicChroma
{
    Standard,
    LuminanceOnly,
    PerChannelRGB
}
