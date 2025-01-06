using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool;

[CustomEditor(typeof(TilesetComponent.ComponentControls))]
public class TilesetComponentControlsWidget : ControlWidget
{
    public override bool SupportsMultiEdit => false;

    TilesetComponent TilesetComponent;

    public TilesetComponentControlsWidget(SerializedProperty property) : base(property)
    {
        TilesetComponent = property.Parent.Targets.First() as TilesetComponent;
        if (TilesetComponent is null)
        {
            return;
        }

        Layout = Layout.Column();
        Layout.Spacing = 2;

        Rebuild();
    }

    protected override void OnPaint() { }

    void Rebuild()
    {
        Layout.Clear(true);

        var btn = Layout.Add(new Button("Open in Tileset Tool", this));
        btn.Icon = "dashboard";
        btn.Clicked += () =>
        {
            TilesetTool.OpenComponent(TilesetComponent);
        };
    }
}