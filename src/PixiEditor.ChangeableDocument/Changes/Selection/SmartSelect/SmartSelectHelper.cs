using System.Collections;
using ChunkyImageLib.Operations;
using Drawie.Backend.Core.ColorsImpl;
using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Changeables.Interfaces;
using PixiEditor.ChangeableDocument.Changes.Drawing.FloodFill;
using PixiEditor.ChangeableDocument.Changes.Selection.MagicWand;

namespace PixiEditor.ChangeableDocument.Changes.Selection.SmartSelect;

/// <summary>
/// Edge-aware region growing. Unlike Magic Wand, this follows gradual color changes inside a segment while
/// refusing abrupt neighboring-pixel transitions, which makes shaded objects much easier to select.
/// </summary>
internal static class SmartSelectHelper
{
    private static readonly VecI[] Neighbors = [new(-1, 0), new(1, 0), new(0, -1), new(0, 1)];

    public static VectorPath SelectSegment(VecI start, HashSet<Guid>? membersToRender, double tolerance,
        double edgeSensitivity, string renderOutput, IReadOnlyDocument document, int frame)
    {
        VecI size = document.GetRenderOutputSize(renderOutput);
        if (!IsInside(start, size))
            return new VectorPath();

        long pixelCount = (long)size.X * size.Y;
        if (pixelCount <= 0 || pixelCount > int.MaxValue)
            return new VectorPath();

        tolerance = Math.Clamp(tolerance, 0, 1);
        edgeSensitivity = Math.Clamp(edgeSensitivity, 0, 1);

        using FloodFillChunkCache cache = FloodFillHelper.CreateCache(membersToRender, document, frame, renderOutput);
        ColorF seedColor = GetPixel(cache, start);

        // Overall similarity anchors the selection to the clicked object. The local threshold allows smooth
        // lighting gradients, while Edge Sensitivity tightens the boundary at high-contrast transitions.
        double seedLimit = 0.035 + tolerance * 1.25;
        double localLimit = Lerp(0.22, 0.025, edgeSensitivity) + tolerance * 0.22;

        BitArray selected = new((int)pixelCount);
        Queue<VecI> frontier = new();
        List<int> selectedIndices = [];

        int startIndex = ToIndex(start, size.X);
        selected[startIndex] = true;
        selectedIndices.Add(startIndex);
        frontier.Enqueue(start);

        while (frontier.Count > 0)
        {
            VecI current = frontier.Dequeue();
            ColorF currentColor = GetPixel(cache, current);

            foreach (VecI offset in Neighbors)
            {
                VecI next = current + offset;
                if (!IsInside(next, size))
                    continue;

                int index = ToIndex(next, size.X);
                if (selected[index])
                    continue;

                ColorF candidate = GetPixel(cache, next);
                double seedDistance = ColorDistance(seedColor, candidate);
                double edgeDistance = ColorDistance(currentColor, candidate);
                if (seedDistance > seedLimit || edgeDistance > localLimit)
                    continue;

                selected[index] = true;
                selectedIndices.Add(index);
                frontier.Enqueue(next);
            }
        }

        MagicWandHelper.Lines contour = new();
        foreach (int index in selectedIndices)
        {
            int x = index % size.X;
            int y = index / size.X;

            if (x == 0 || !selected[index - 1])
                contour.AddLine(new MagicWandHelper.Line(new VecI(x, y + 1), new VecI(x, y)));
            if (x == size.X - 1 || !selected[index + 1])
                contour.AddLine(new MagicWandHelper.Line(new VecI(x + 1, y), new VecI(x + 1, y + 1)));
            if (y == 0 || !selected[index - size.X])
                contour.AddLine(new MagicWandHelper.Line(new VecI(x, y), new VecI(x + 1, y)));
            if (y == size.Y - 1 || !selected[index + size.X])
                contour.AddLine(new MagicWandHelper.Line(new VecI(x + 1, y + 1), new VecI(x, y + 1)));
        }

        return contour.Count == 0 ? new VectorPath() : MagicWandHelper.BuildContour(contour);
    }

    private static ColorF GetPixel(FloodFillChunkCache cache, VecI position)
    {
        int chunkSize = ChunkResolution.Full.PixelSize();
        VecI chunkPosition = OperationHelper.GetChunkPos(position, chunkSize);
        VecI positionInChunk = position - chunkPosition * chunkSize;
        return cache.GetChunk(chunkPosition).Match(
            chunk => chunk.Surface.GetRawPixelPrecise(positionInChunk),
            _ => Colors.Transparent);
    }

    private static double ColorDistance(ColorF first, ColorF second)
    {
        double firstAlpha = first.A;
        double secondAlpha = second.A;
        double red = first.R * firstAlpha - second.R * secondAlpha;
        double green = first.G * firstAlpha - second.G * secondAlpha;
        double blue = first.B * firstAlpha - second.B * secondAlpha;
        double alpha = firstAlpha - secondAlpha;

        return Math.Sqrt(red * red * 0.2126 + green * green * 0.7152 + blue * blue * 0.0722 +
                         alpha * alpha * 0.5);
    }

    private static bool IsInside(VecI position, VecI size) =>
        position.X >= 0 && position.Y >= 0 && position.X < size.X && position.Y < size.Y;

    private static int ToIndex(VecI position, int width) => position.Y * width + position.X;
    private static double Lerp(double from, double to, double amount) => from + (to - from) * amount;
}
