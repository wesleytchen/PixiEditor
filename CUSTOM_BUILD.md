# PixiEditor ASCII Magic custom build

This fork embeds the ASCII Magic-inspired effects directly in PixiEditor and adds an edge-aware Smart Select tool. It does not require the old extension or loose shader files.

## Run the macOS build

Quit the installed PixiEditor first so macOS does not group both processes under the same application identity, then run:

```sh
./run-custom.sh
```

The current compiled executable targets Apple Silicon (`osx-arm64`) and uses .NET 10.

## Use the effects

1. Open **Graph View** for a document.
2. Add a node and search for **ASCII Magic**, or browse the **ASCII Magic** category.
3. Choose one of the ten family nodes: ASCII & Text, Pixel & Blocks, Print & Paper, Geometric, Distort, Blur & Focus, Glitch & Signal, Light & Color, Glass, or Material & Texture.
4. Connect an image or layer output to the node's **Background** input, then connect the node's output to the document **Output** node.
5. Pick an effect from the family node's **Effect** dropdown. Use **Amount**, **Scale**, and **Seed** to tune it.

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
