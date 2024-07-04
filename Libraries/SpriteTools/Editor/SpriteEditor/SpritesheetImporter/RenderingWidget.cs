using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class RenderingWidget : SpriteRenderingWidget
{
    Material GridMaterial;

    public RenderingWidget(Widget parent) : base(parent)
    {
        var GridMaterial = Material.Load("materials/sprite_sheet_grid.vmat");

        TextureRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        TextureRect.SetMaterialOverride(GridMaterial);
        TextureRect.Flags.WantsFrameBufferCopy = true;
        TextureRect.Flags.IsTranslucent = true;
        TextureRect.Flags.IsOpaque = false;
        TextureRect.Flags.CastShadows = false;
    }

    protected override void OnMousePress(MouseEvent e)
    {
        base.OnMousePress(e);
    }

    protected override void OnMouseMove(MouseEvent e)
    {
        base.OnMouseMove(e);
    }

    protected override void OnMouseReleased(MouseEvent e)
    {
        base.OnMouseReleased(e);
    }

    public override void PreFrame()
    {
        base.PreFrame();

        // GridMaterial.Set

    }
}