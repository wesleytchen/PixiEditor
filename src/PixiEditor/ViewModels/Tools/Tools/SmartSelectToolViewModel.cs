using Avalonia.Input;
using Drawie.Numerics;
using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Models.Commands.Attributes.Commands;
using PixiEditor.Models.Handlers.Tools;
using PixiEditor.Models.Tools;
using PixiEditor.UI.Common.Localization;
using PixiEditor.ViewModels.Tools.ToolSettings.Toolbars;

namespace PixiEditor.ViewModels.Tools.Tools;

[Command.Tool]
internal sealed class SmartSelectToolViewModel : ToolViewModel, ISmartSelectToolHandler
{
    public override LocalizedString Tooltip => new("SMART_SELECT_TOOL_TOOLTIP", Shortcut);
    public override string ToolNameLocalizationKey => "SMART_SELECT_TOOL";
    public override Type[]? SupportedLayerTypes { get; } = null;
    public override Type? LayerTypeToCreateOnEmptyUse { get; } = null;
    public override string DefaultIcon => PixiPerfectIcons.Magic;

    private readonly LocalizedString defaultActionDisplay = new("SMART_SELECT_ACTION_DISPLAY");
    private SelectionMode keyModifierSelectionMode = SelectionMode.New;

    public SelectionMode ResultingSelectionMode =>
        keyModifierSelectionMode != SelectionMode.New ? keyModifierSelectionMode : SelectMode;

    [Settings.Enum("MODE_LABEL")]
    public SelectionMode SelectMode => GetValue<SelectionMode>();

    [Settings.Enum("SCOPE_LABEL", DocumentScope.Canvas)]
    public DocumentScope DocumentScope => GetValue<DocumentScope>();

    [Settings.Percent("TOLERANCE_LABEL", 0.18f, ExposedByDefault = true)]
    public float Tolerance => GetValue<float>();

    [Settings.Percent("EDGE_SENSITIVITY_LABEL", 0.65f, ExposedByDefault = true)]
    public float EdgeSensitivity => GetValue<float>();

    public SmartSelectToolViewModel()
    {
        Toolbar = ToolbarFactory.Create(this);
        ActionDisplay = defaultActionDisplay;
    }

    public override void UseTool(VecD pos)
    {
        ViewModelMain.Current?.DocumentManagerSubViewModel.ActiveDocument?.Tools.UseSmartSelectTool();
    }

    public override void KeyChanged(bool ctrlIsDown, bool shiftIsDown, bool altIsDown, Key argsKey)
    {
        if (shiftIsDown)
        {
            ActionDisplay = new LocalizedString("SMART_SELECT_ACTION_DISPLAY_SHIFT");
            keyModifierSelectionMode = SelectionMode.Add;
        }
        else if (ctrlIsDown)
        {
            ActionDisplay = new LocalizedString("SMART_SELECT_ACTION_DISPLAY_CTRL");
            keyModifierSelectionMode = SelectionMode.Subtract;
        }
        else
        {
            ActionDisplay = defaultActionDisplay;
            keyModifierSelectionMode = SelectionMode.New;
        }
    }
}
