using Sandbox;
using Editor;
using System.Collections.Generic;

namespace SpriteTools.TilesetTool;


[CustomEditor(typeof(List<TilesetComponent.Layer>))]
public class TilesetLayerListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

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

        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();

        foreach (var layer in layers)
        {
            var button = content.Add(new TilesetLayerControl(this, layer));
            button.MouseClick = () => SelectLayer(layer);
        }
    }

    void CreateNewLayer()
    {
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Add(new TilesetComponent.Layer());
        SerializedProperty.SetValue(layers);
        UpdateList();
    }

    public void SelectLayer(TilesetComponent.Layer layer)
    {
        if (TilesetTool.Active is null) return;

        TilesetTool.Active.SelectedLayer = layer;
    }

    public void DeleteLayer(TilesetComponent.Layer layer)
    {
        var layers = SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        layers.Remove(layer);
        SerializedProperty.SetValue(layers);
        UpdateList();
    }

}