using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;


[CustomEditor(typeof(List<TilesetResource.Tile>))]
public class TilesetTileListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

    internal MainWindow MainWindow;

    Layout content;
    ScrollArea scrollArea;

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

        var tiles = SerializedProperty.GetValue<List<TilesetResource.Tile>>();

        foreach (var tile in tiles)
        {
            var button = content.Add(new TilesetTileControl(this, tile));
            button.labelText.EmptyValue = $"Tile {tile.Position}";
            button.MouseClick = () => SelectTile(tile);
        }
    }

    void CreateNewLayer()
    {
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Add(new TilesetComponent.Layer());
        SerializedProperty.SetValue(layers);
        UpdateList();
    }

    public void SelectTile(TilesetResource.Tile tile)
    {
        if (MainWindow is null) return;
        MainWindow.SelectTile(tile);
    }

    public void DeleteTile(TilesetResource.Tile tile)
    {
        var tiles = SerializedProperty.GetValue<List<TilesetResource.Tile>>();
        tiles.Remove(tile);
        SerializedProperty.SetValue(tiles);
        UpdateList();
    }

}