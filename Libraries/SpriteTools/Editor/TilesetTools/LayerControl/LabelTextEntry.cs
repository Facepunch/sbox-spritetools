using Editor;
using Sandbox;
using System.Linq;

namespace SpriteTools.TilesetTool;

internal class LabelTextEntry : Widget
{
    SerializedProperty Property;
    string lastValue = "";
    string lastSafeValue = "";

    bool editing = false;
    RealTimeSince timeSinceLastEdit = 0;
    StringControlWidget stringControl;

    public LabelTextEntry(SerializedProperty property) : base(null)
    {
        Layout = Layout.Row();
        Property = property;

        RebuildUI();
    }

    void RebuildUI()
    {
        Layout.Clear(true);

        if (editing)
        {
            stringControl = Layout.Add(new StringControlWidget(Property));
            stringControl.HorizontalSizeMode = SizeMode.CanShrink;
            stringControl.MaximumWidth = 250;
            Layout.AddStretchCell();
        }
        else
        {
            Layout.Add(new Label(Property.GetValue("N/A")));
        }
    }

    protected override void OnDoubleClick(MouseEvent e)
    {
        timeSinceLastEdit = 0;

        if (editing)
        {
            editing = false;
            RebuildUI();
        }
        else
        {
            Edit();
        }
    }

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);

        if (e.Key == KeyCode.Enter || e.Key == KeyCode.Return)
        {
            StopEditing();
        }
    }

    public void Edit()
    {
        lastSafeValue = Property.GetValue("N/A");
        editing = true;
        timeSinceLastEdit = 0f;
        RebuildUI();
        stringControl?.StartEditing();
    }

    public void StopEditing()
    {
        if (!editing) return;

        editing = false;
        var value = Property.GetValue("");
        Property.SetValue(value);
        RebuildUI();
    }

    [EditorEvent.Frame]
    void Frame()
    {
        if (editing)
        {
            var val = Property.GetValue("");
            if (lastValue != val)
            {
                lastValue = val;
                timeSinceLastEdit = 0;
            }

            if (timeSinceLastEdit > 5f)
            {
                StopEditing();
            }
        }
    }
}