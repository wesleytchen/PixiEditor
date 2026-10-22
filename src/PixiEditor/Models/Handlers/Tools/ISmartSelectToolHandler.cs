using PixiEditor.ChangeableDocument.Enums;
using PixiEditor.Models.Tools;

namespace PixiEditor.Models.Handlers.Tools;

internal interface ISmartSelectToolHandler : IToolHandler
{
    SelectionMode ResultingSelectionMode { get; }
    DocumentScope DocumentScope { get; }
    float Tolerance { get; }
    float EdgeSensitivity { get; }
}
