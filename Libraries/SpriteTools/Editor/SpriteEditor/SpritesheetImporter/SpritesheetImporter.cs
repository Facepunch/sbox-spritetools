using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class SpritesheetImporter : Dialog
{
    public string Path { get; set; }

    Preview Preview { get; set; }
    public Action<string, List<Rect>> OnImport { get; set; }

    ImportSettings Settings { get; set; } = new ImportSettings();

    ControlSheet ControlSheet { get; set; }

    public SpritesheetImporter(Widget parent, string path) : base(parent, false)
    {
        Path = path;

        Window.Title = "Spritesheet Importer";
        Window.WindowTitle = "Spritesheet Importer";
        Window.Size = new Vector2(960, 540);
        Window.SetModal(true);
        Window.MinimumSize = 200;
        Window.MaximumSize = 10000;

        BuildLayout();
    }

    void BuildLayout()
    {
        Layout = Layout.Row();

        var leftSide = Layout.Column();
        leftSide.Margin = 16;
        var leftContent = new Widget();
        leftContent.MaximumWidth = 300;
        leftContent.Layout = Layout.Column();
        ControlSheet = new ControlSheet();
        UpdateControlSheet();
        leftContent.Layout.Add(ControlSheet);
        leftContent.Layout.AddStretchCell();
        leftSide.Add(leftContent);
        Layout.Add(leftSide);

        Preview = new Preview(this);
        Layout.Add(Preview);
    }

    [EditorEvent.Hotload]
    void UpdateControlSheet()
    {
        ControlSheet?.Clear(true);
        ControlSheet.AddObject(Settings.GetSerialized());
    }

}