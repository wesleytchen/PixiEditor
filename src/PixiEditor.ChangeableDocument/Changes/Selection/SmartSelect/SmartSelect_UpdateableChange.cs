using Drawie.Backend.Core.Vector;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Enums;

namespace PixiEditor.ChangeableDocument.Changes.Selection.SmartSelect;

internal sealed class SmartSelect_UpdateableChange : UpdateableChange
{
    private VectorPath? originalPath;
    private VectorPath path = new() { FillType = PathFillType.EvenOdd };
    private VecI point;
    private readonly List<Guid>? memberGuids;
    private readonly SelectionMode mode;
    private readonly double tolerance;
    private readonly double edgeSensitivity;
    private readonly int frame;
    private readonly string renderOutput;

    [GenerateUpdateableChangeActions]
    public SmartSelect_UpdateableChange(List<Guid>? memberGuids, VecI point, SelectionMode mode, double tolerance,
        double edgeSensitivity, int frame, string renderOutput)
    {
        this.memberGuids = memberGuids;
        this.point = point;
        this.mode = mode;
        this.tolerance = tolerance;
        this.edgeSensitivity = edgeSensitivity;
        this.frame = frame;
        this.renderOutput = renderOutput;
    }

    public override bool InitializeAndValidate(Document target)
    {
        originalPath = new VectorPath(target.Selection.SelectionPath);
        return true;
    }

    [UpdateChangeMethod]
    public void Update(VecI point) => this.point = point;

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Apply(Document target, bool firstApply,
        out bool ignoreInUndo)
    {
        ignoreInUndo = false;
        return CommonApply(target);
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> ApplyTemporarily(Document target) => CommonApply(target);

    private Selection_ChangeInfo CommonApply(Document target)
    {
        HashSet<Guid>? membersToReference = null;
        if (memberGuids is not null)
        {
            membersToReference = [];
            target.ForEveryReadonlyMember(member =>
            {
                if (memberGuids.Contains(member.Id))
                    membersToReference.Add(member.Id);
            });
        }

        path.Dispose();
        path = SmartSelectHelper.SelectSegment(point, membersToReference, tolerance, edgeSensitivity, renderOutput,
            target, frame);

        var oldSelection = target.Selection.SelectionPath;
        if (mode == SelectionMode.New)
        {
            var copy = new VectorPath(path);
            copy.Close();
            target.Selection.SelectionPath = copy;
        }
        else
        {
            target.Selection.SelectionPath = target.Selection.SelectionPath!.Op(path, mode.ToVectorPathOp());
        }

        oldSelection.Dispose();
        return new Selection_ChangeInfo(new VectorPath(target.Selection.SelectionPath));
    }

    public override OneOf<None, IChangeInfo, List<IChangeInfo>> Revert(Document target)
    {
        var oldSelection = target.Selection.SelectionPath;
        target.Selection.SelectionPath = new VectorPath(originalPath!);
        oldSelection.Dispose();
        return new Selection_ChangeInfo(new VectorPath(target.Selection.SelectionPath));
    }

    public override void Dispose()
    {
        path.Dispose();
        originalPath?.Dispose();
    }
}
