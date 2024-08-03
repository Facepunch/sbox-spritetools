using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SpriteTools.TilesetTool;


[CustomEditor(typeof(List<TilesetComponent.Layer>))]
public class TilesetLayerListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

    internal List<TilesetLayerControl> LayerControls = new();

    Layout content;
    ScrollArea scrollArea;

    public TilesetLayerListControl(SerializedProperty property) : base(property)
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

        // Add button
        var row = Layout.AddRow();
        row.AddStretchCell();
        row.Margin = 16;
        var btnAdd = row.Add(new Button.Primary("Create New Layer", "add"));
        btnAdd.HorizontalSizeMode = SizeMode.CanGrow;
        btnAdd.Clicked = CreateNewLayer;
        row.AddStretchCell();
        var btnBake = row.Add(new Button("Bake Selected Layer", "palette"));
        btnBake.HorizontalSizeMode = SizeMode.CanGrow;
        btnBake.Clicked = BakeSelectedLayerPopup;
        row.AddStretchCell();

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
        LayerControls.Clear();

        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();

        var collisionLayer = layers.FirstOrDefault(x => x.IsCollisionLayer);
        int index = 0;
        foreach (var layer in layers)
        {
            var button = content.Add(new TilesetLayerControl(this, layer));
            if (collisionLayer is null && index == 0) button.icoCollisionLayer.Visible = true;
            else if (layer == collisionLayer) button.icoCollisionLayer.Visible = true;
            button.MouseClick = () => SelectLayer(layer);
            LayerControls.Add(button);
            index++;
        }
    }

    void CreateNewLayer()
    {
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Add(new TilesetComponent.Layer());
        SerializedProperty.SetValue(layers);
        UpdateList();

        if (EditorToolManager.CurrentModeName == nameof(TilesetTool))
        {
            TilesetTool.Active.SelectedLayer = layers.Last();
            TilesetTool.Active.UpdateInspector?.Invoke();
        }
    }

    void BakeSelectedLayerPopup()
    {
        var layer = TilesetTool.Active?.SelectedLayer;
        if (layer is null) return;

        var confirm = new PopupWindow(
            $"Bake Selected Layer",
            $"Are you sure you want to Bake the Tiles on Layer \"{layer.Name}\"?\nThis will detach all tiles from their current references.", "No",
            new Dictionary<string, Action>() { { "Yes", BakeSelectedLayer } }
        );
        confirm.Show();
    }

    void BakeSelectedLayer()
    {
        var layer = TilesetTool.Active?.SelectedLayer;
        if (layer is null) return;

        var tilemap = layer?.TilesetResource?.TileMap;
        if (tilemap is null) return;

        foreach (var tile in layer.Tiles)
        {
            if (tile.Value.TileId == Guid.Empty || !tilemap.ContainsKey(tile.Value.TileId)) continue;
            var tileRef = tilemap[tile.Value.TileId];
            tile.Value.TileId = default;
            tile.Value.CellPosition = default;
            tile.Value.BakedPosition = tileRef.Position + tile.Value.CellPosition;
        }
    }

    public void SelectLayer(TilesetComponent.Layer layer)
    {
        if (TilesetTool.Active is null) return;

        TilesetTool.Active.SelectedLayer = layer;
        TilesetTool.Active.UpdateInspector?.Invoke();
    }

    public void DeleteLayer(TilesetComponent.Layer layer)
    {
        bool isSelected = TilesetTool.Active.SelectedLayer == layer;
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Remove(layer);
        SerializedProperty.SetValue(layers);
        UpdateList();

        if (EditorToolManager.CurrentModeName == nameof(TilesetTool) && isSelected)
        {
            TilesetTool.Active.SelectedLayer = layers.FirstOrDefault();
            TilesetTool.Active.UpdateInspector?.Invoke();
        }
    }

}