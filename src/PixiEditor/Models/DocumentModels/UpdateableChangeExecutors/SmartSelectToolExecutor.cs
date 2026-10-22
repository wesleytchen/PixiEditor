using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Actions.Generated;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Models.Controllers.InputDevice;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Models.Tools;
using PixiEditor.ViewModels.SubViewModels;

namespace PixiEditor.Models.DocumentModels.UpdateableChangeExecutors;

internal sealed class SmartSelectToolExecutor : UpdateableChangeExecutor
{
    private List<Guid>? memberGuids;
    private SelectionMode mode;
    private float tolerance;
    private float edgeSensitivity;
    private string renderOutput = string.Empty;

    public override ExecutionState Start()
    {
        var tool = GetHandler<ISmartSelectToolHandler>();
        renderOutput = GetHandler<WindowViewModel>().LastActiveViewport?.RenderOutputName ?? string.Empty;
        var members = document!.ExtractSelectedLayers(true).ToList();

        if (tool is null || members.Count == 0)
            return ExecutionState.Error;

        mode = tool.ResultingSelectionMode;
        memberGuids = tool.DocumentScope == DocumentScope.Canvas ? null : members;
        tolerance = tool.Tolerance;
        edgeSensitivity = tool.EdgeSensitivity;

        AddUpdateAction(controller!.LastPixelPosition);
        return ExecutionState.Success;
    }

    public override void OnPixelPositionChange(VecI pos, MouseOnCanvasEventArgs args) => AddUpdateAction(pos);

    public override void OnLeftMouseButtonUp(VecD argsPositionOnCanvas)
    {
        AddFinishAction();
        onEnded!(this);
    }

    public override void ForceStop() => AddFinishAction();

    private void AddUpdateAction(VecI pos)
    {
        internals!.ActionAccumulator.AddActions(new SmartSelect_Action(memberGuids, pos, mode, tolerance,
            edgeSensitivity, document!.AnimationHandler.ActiveFrameBindable, renderOutput));
    }

    private void AddFinishAction()
    {
        internals!.ActionAccumulator.AddFinishedActions(new EndSmartSelect_Action());
    }
}
