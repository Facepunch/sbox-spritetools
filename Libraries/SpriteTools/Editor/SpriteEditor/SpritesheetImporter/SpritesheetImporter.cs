using System;
using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class SpritesheetImporter : Dialog
{
    public string Path { get; set; }

    public Action<string, List<Rect>> OnImport { get; set; }

    public SpritesheetImporter(Widget parent, string path) : base(parent, false)
    {
        Path = path;

        Window.Size = new Vector2(1280, 720);
        Window.SetModal(true);
        Window.MinimumSize = 200;
        Window.MaximumSize = 10000;
        
    }

}