using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SpriteTools.TilesetEditor;


[CustomEditor(typeof(List<TilesetResource.Tile>))]
public class TilesetTileListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

    internal MainWindow MainWindow;

    internal List<TilesetTileControl> Selected = new();
    internal List<TilesetTileControl> Buttons = new();

    internal Layout content;
    ScrollArea scrollArea;

    KeyboardModifiers modifiers;

    public TilesetTileListControl(SerializedProperty property) : base(property)
    {
        Layout = Layout.Column();

        if (property.IsNull)
        {
            property.SetValue(new List<TilesetComponent.Layer>());
        }

        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.CanShrink;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.CanGrow;
        scrollArea.MinimumHeight = 157;
        scrollArea.MaximumHeight = 157;
        scrollArea.FixedHeight = 157;
        Layout.Add(scrollArea);

        // Content list
        content = Layout.Column();
        content.Margin = 4;
        content.AddStretchCell();
        scrollArea.Canvas.Layout.Add(content);

        scrollArea.Canvas.Layout.AddStretchCell();

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateList();
    }

    protected override void OnPaint()
    {
        Paint.SetBrush(Theme.ControlBackground);
        Paint.SetPen(Theme.ControlBackground);
        Paint.DrawRect(scrollArea.LocalRect, 4);
    }

    public void UpdateList()
    {
        content.Clear(true);
        Buttons.Clear();

        var tiles = SerializedProperty.GetValue<List<TilesetResource.Tile>>();

        foreach (var tile in tiles)
        {
            var button = content.Add(new TilesetTileControl(this, tile));
            Buttons.Add(button);
        }
    }

    void CreateNewLayer()
    {
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Add(new TilesetComponent.Layer());
        SerializedProperty.SetValue(layers);
        UpdateList();
    }

    internal void SelectTile(TilesetTileControl button, TilesetResource.Tile tile)
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
        var tiles = new List<TilesetResource.Tile>();
        SerializedProperty.SetValue(tiles);
        UpdateList();
    }

    public void DeleteTile(TilesetResource.Tile tile)
    {
        var tiles = SerializedProperty.GetValue<List<TilesetResource.Tile>>();
        tiles.Remove(tile);
        SerializedProperty.SetValue(tiles);
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