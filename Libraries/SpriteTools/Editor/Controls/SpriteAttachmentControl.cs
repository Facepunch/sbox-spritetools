using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools;

[CustomEditor(typeof(SpriteAttachment))]
public class SpriteAttachmentControlWidget : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    static int attachmentsMade = 0;

    public SpriteAttachmentControlWidget(SerializedProperty property) : base(property)
    {
        Layout = Layout.Row();
        Layout.Spacing = 2;

        if (property.IsNull)
        {
            property.SetValue(new SpriteAttachment($"new attachment {attachmentsMade++}"));
        }

        var serializedObject = property.GetValue<SpriteAttachment>()?.GetSerialized();
        if (serializedObject is null)
            return;

        serializedObject.TryGetProperty(nameof(SpriteAttachment.Color), out var color);
        serializedObject.TryGetProperty(nameof(SpriteAttachment.Name), out var name);
        // serializedObject.TryGetProperty(nameof(SpriteAttachment.Points), out var attachPoints);
        // if (!attachPoints.TryGetAsObject(out var so) || so is not SerializedCollection sc)
        //     return;

        Layout.Add(new ColorSwatchWidget(color) { FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
        Layout.Add(new StringControlWidget(name) { MinimumWidth = 100, HorizontalSizeMode = SizeMode.Default });
    }

    protected override void OnPaint()
    {
        // nothing
    }

}