# PixiEditor ASCII Magic custom build

This fork embeds the ASCII Magic-inspired effects directly in PixiEditor and adds an edge-aware Smart Select tool. It does not require the old extension or loose shader files.

## Run the macOS build

Create the local app bundle, then open it from Finder:

```sh
./build-macos-app.sh
open "dist/PixiEditor Custom.app"
```

The bundle has its own `PixiEditor Custom` identity, so it can coexist with the installed app. The current build targets Apple Silicon (`osx-arm64`) and uses the Homebrew .NET 10 runtime. For a quick terminal launch, use `./run-custom.sh`.

## Use the effects

1. Open **Graph View** for a document.
2. Add a node and search for **ASCII Magic**, or browse the **ASCII Magic** category.
3. Choose one of the ten family nodes: ASCII & Text, Pixel & Blocks, Print & Paper, Geometric, Distort, Blur & Focus, Glitch & Signal, Light & Color, Glass, or Material & Texture.
4. Connect an image or layer output to the node's **Background** input, then connect the node's output to the document **Output** node.
5. Pick an effect from the family node's **Filter** dropdown. Tune it with **Effect strength**, the plainly named size control—such as **Character size**, **Pixel size**, **Blur radius**, or **Texture size**—and the shared finishing controls.

Each family node also includes **Output opacity**, **Palette**, **Chroma**, **Brightness**, **Contrast**, and **Invert colors**. Palette choices mirror the useful presets from ASCII Magic, including monochrome, limited grayscale, Game Boy, CGA, PICO-8, Cyberpunk, Pastel, Risograph, and Sepia. Chroma uses understandable modes: Standard, Luminance only, or Per-channel RGB.

The nodes intentionally hide shader terminology and procedural seeds. Strength and opacity are shown as 0–100%, sizes use pixels where that is meaningful, brightness and contrast use -100 to 100, and procedural patterns stay stable between renders.

The family nodes contain all 106 effects represented by the shader pack.

## Use Smart Select

1. Choose **Smart Select** from the selection tools.
2. Click inside the image segment you want.
3. Adjust **Tolerance** to admit more or less overall color variation.
4. Raise **Edge sensitivity** to stop more aggressively at local contrast boundaries.
5. Hold **Shift** to add a segment or **Ctrl** to subtract one.

Smart Select differs from Magic Wand by comparing both the clicked color and each neighboring pixel. This lets it follow gradual shading within an object while stopping at sharper edges.

## Rebuild

```sh
dotnet build src/PixiEditor.Desktop/PixiEditor.Desktop.csproj -c Debug -r osx-arm64
```
