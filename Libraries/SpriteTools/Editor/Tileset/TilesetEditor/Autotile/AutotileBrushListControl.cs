using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SpriteTools.TilesetEditor;


[CustomEditor(typeof(List<AutotileBrush>))]
public class AutotileBrushListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

    internal MainWindow MainWindow;

    internal List<AutotileBrushControl> Selected = new();
    internal List<AutotileBrushControl> Buttons = new();

    internal Layout content;
    ScrollArea scrollArea;

    KeyboardModifiers modifiers;

    public AutotileBrushListControl(SerializedProperty property) : base(property)
    {
        Layout = Layout.Column();
        Layout.Spacing = 8;

        if (property.IsNull)
        {
            property.SetValue(new List<AutotileBrush>());
        }

        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.CanShrink;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.CanGrow;
        scrollArea.MinimumHeight = 300;
        scrollArea.MaximumHeight = 300;
        scrollArea.FixedHeight = 300;
        Layout.Add(scrollArea);

        // Content list
        content = Layout.Column();
        content.Margin = 4;
        content.AddStretchCell();
        scrollArea.Canvas.Layout.Add(content);

        // Buttons
        var row = Layout.AddRow();
        row.AddStretchCell();
        row.Spacing = 8;
        var btn16Tile = row.Add(new Button.Primary("New 16-Tile Brush", "add"));
        btn16Tile.ToolTip = "Create a new brush with 16 tiles";
        btn16Tile.Clicked += () => NewBrush(false);
        var btn47Tile = row.Add(new Button.Primary("New 47-Tile Brush", "add"));
        btn47Tile.ToolTip = "Create a new brush with 47 tiles";
        btn47Tile.Clicked += () => NewBrush(true);

        scrollArea.Canvas.Layout.AddStretchCell();

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateList();
    }

    protected override void OnPaint()
    {
        if (MainWindow is null)
        {
            foreach (var window in MainWindow.OpenWindows)
            {
                if (IsDescendantOf(window.inspector))
                {
                    MainWindow = window;
                    // window.inspector.tileList = this;
                    break;
                }
            }
        }

        Paint.SetBrush(Theme.ControlBackground);
        Paint.SetPen(Theme.ControlBackground);
        Paint.DrawRect(scrollArea.LocalRect, 4);
    }

    public void UpdateList()
    {
        content.Clear(true);
        Buttons.Clear();

        var brushes = SerializedProperty.GetValue<List<AutotileBrush>>();

        foreach (var brush in brushes)
        {
            var button = content.Add(new AutotileBrushControl(this, brush));
            Buttons.Add(button);
        }
    }

    internal void SelectBrush(AutotileBrushControl button, AutotileBrush brush)
    {
        if (MainWindow is null) return;
        var buttonIndex = Buttons.IndexOf(button);
        if (modifiers.HasFlag(KeyboardModifiers.Shift))
        {
            var minIndex = Selected.Min(x => Buttons.IndexOf(x));
            var maxIndex = Selected.Max(x => Buttons.IndexOf(x));

            if (buttonIndex >= minIndex && buttonIndex <= maxIndex)
            {
                Selected.Clear();
                for (int i = minIndex; i <= buttonIndex; i++)
                {
                    if (!Selected.Contains(Buttons[i])) Selected.Add(Buttons[i]);
                }
            }
            else if (buttonIndex < minIndex)
            {
                for (int i = buttonIndex; i < minIndex; i++)
                {
                    if (!Selected.Contains(Buttons[i])) Selected.Add(Buttons[i]);
                }
            }
            else if (buttonIndex > maxIndex)
            {
                for (int i = maxIndex + 1; i <= buttonIndex; i++)
                {
                    if (!Selected.Contains(Buttons[i])) Selected.Add(Buttons[i]);
                }
            }
        }
        else
        {
            if (modifiers.HasFlag(KeyboardModifiers.Ctrl))
            {
                if (Selected.Contains(button)) Selected.Remove(button);
                else Selected.Add(button);
            }
            else
            {
                Selected.Clear();
                Selected.Add(button);
            }
        }
        MainWindow.inspector.UpdateSelectedSheet();
    }

    public void DeleteAll()
    {
        var brushes = new List<AutotileBrush>();
        SerializedProperty.SetValue(brushes);
        UpdateList();
    }

    public void DeleteBrush(AutotileBrush brush)
    {
        var brushes = SerializedProperty.GetValue<List<AutotileBrush>>();
        brushes.Remove(brush);
        SerializedProperty.SetValue(brushes);
        UpdateList();
    }

    public void NewBrush(bool is47Tiles = false)
    {
        var layers = SerializedProperty.GetValue<List<AutotileBrush>>();
        layers.Add(new AutotileBrush(is47Tiles));
        SerializedProperty.SetValue(layers);
        UpdateList();
    }

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);
        modifiers = e.KeyboardModifiers;
    }

    protected override void OnKeyRelease(KeyEvent e)
    {
        base.OnKeyRelease(e);
        modifiers = e.KeyboardModifiers;
    }

}