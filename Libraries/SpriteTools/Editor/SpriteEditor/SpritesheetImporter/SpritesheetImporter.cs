using System;
using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class SpritesheetImporter : Dialog
{
    public static string Path { get; set; }

    SpritesheetPreview Preview { get; set; }
    public Action<string, List<Rect>> OnImport { get; set; }

    public SpritesheetImporter(Widget parent, string path) : base(parent, false)
    {
        Path = path;

        Window.Size = new Vector2(1280, 720);
        Window.SetModal(true);
        Window.MinimumSize = 200;
        Window.MaximumSize = 10000;

        Layout = Layout.Row();

        var leftSide = Layout.Column();
        leftSide.Add(new Label("Spritesheet Importer"));
        Layout.Add(leftSide);

        Preview = new SpritesheetPreview(this);
        Layout.Add(Preview);

    }

}