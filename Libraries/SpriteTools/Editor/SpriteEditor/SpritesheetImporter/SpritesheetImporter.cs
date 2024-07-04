using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class SpritesheetImporter : Dialog
{
    public static string Path { get; set; }

    Preview Preview { get; set; }
    public Action<string, List<Rect>> OnImport { get; set; }

    ImportSettings Settings { get; set; } = new ImportSettings();

    public SpritesheetImporter(Widget parent, string path) : base(parent, false)
    {
        Path = path;

        Window.Size = new Vector2(960, 540);
        Window.SetModal(true);
        Window.MinimumSize = 200;
        Window.MaximumSize = 10000;

        Rebuild();
    }

    [EditorEvent.Hotload]
    void Rebuild()
    {
        Layout = Layout.Row();

        var leftSide = Layout.Column();
        leftSide.Margin = 16;
        leftSide.Add(new Label("Spritesheet Import"));
        var settings = new ControlSheet();
        var props = Settings.GetSerialized().Where(x => x.HasAttribute<PropertyAttribute>())
            .OrderBy(x => x.SourceLine)
            .ThenBy(x => x.DisplayName);
        foreach (var prop in props)
        {
            settings.AddRow(prop);
        }
        leftSide.Add(settings);
        leftSide.AddStretchCell();
        Layout.Add(leftSide);

        Preview = new Preview(this);
        Layout.Add(Preview);
    }

}