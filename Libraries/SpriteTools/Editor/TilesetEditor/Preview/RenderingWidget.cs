using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetEditor.Preview;

public class RenderingWidget : SpriteRenderingWidget
{
    MainWindow MainWindow;

    public RenderingWidget(MainWindow window, Widget parent) : base(parent)
    {
        MainWindow = window;
    }
}