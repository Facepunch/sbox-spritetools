using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class RenderingWidget : SpriteRenderingWidget
{

    public RenderingWidget(Widget parent) : base(parent)
    {

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


    }
}