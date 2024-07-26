using Editor;
using Sandbox;
using System;
using System.Linq;

namespace SpriteTools;

internal class LabelTextEntry : Widget
{
    internal SerializedProperty Property;
    string lastValue = "";
    internal string lastSafeValue = "";

    public string EmptyValue
    {
        get => _emptyValue;
        set
        {
            _emptyValue = value;
            RebuildUI();
        }
    }
    string _emptyValue = "N/A";

    bool editing = false;
    RealTimeSince timeSinceLastEdit = 0;
    StringControlWidget stringControl;

    internal Func<string, bool> OnStopEditing;

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
            var val = Property.GetValue("N/A");
            if (string.IsNullOrEmpty(val)) val = EmptyValue;
            Layout.Add(new Label(val));
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
        if (OnStopEditing?.Invoke(value) ?? true)
        {
            Property.SetValue(value);
        }
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