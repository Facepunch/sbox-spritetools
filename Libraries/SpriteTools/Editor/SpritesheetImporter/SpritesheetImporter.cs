using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.SpritesheetImporter;

public class SpritesheetImporter : Dialog
{
    public string Path { get; set; }
    Preview Preview { get; set; }
    public Action<string, List<Rect>> OnImport { get; set; }
    public ImportSettings Settings { get; set; } = new ImportSettings();

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
        var buttonLoad = new Button("Import Spritesheet", "download", this);
        buttonLoad.Clicked += ImportSpritesheet;
        leftContent.Layout.Add(buttonLoad);
        leftSide.Add(leftContent);
        Layout.Add(leftSide);

        Preview = new Preview(this);
        Layout.Add(Preview);
    }

    void ImportSpritesheet()
    {
        var frames = new List<Rect>();
        var frameWidth = Settings.FrameWidth;
        var frameHeight = Settings.FrameHeight;
        var framesPerRow = Settings.FramesPerRow;
        var frameCount = Settings.NumberOfFrames;
        var horizontalCellOffset = Settings.HorizontalCellOffset;
        var verticalCellOffset = Settings.VerticalCellOffset;
        var horizontalPixelOffset = Settings.HorizontalPixelOffset;
        var verticalPixelOffset = Settings.VerticalPixelOffset;
        var horizontalSeparation = Settings.HorizontalSeparation;
        var verticalSeparation = Settings.VerticalSeparation;

        for (int i = 0; i < frameCount; i++)
        {
            var x = (i % framesPerRow) * (frameWidth + horizontalSeparation) + horizontalPixelOffset + frameWidth * horizontalCellOffset;
            var y = (i / framesPerRow) * (frameHeight + verticalSeparation) + verticalPixelOffset + frameHeight * verticalCellOffset;
            frames.Add(new Rect(x, y, frameWidth, frameHeight));
        }

        OnImport?.Invoke(Path, frames);
        Close();
    }

    [EditorEvent.Hotload]
    void UpdateControlSheet()
    {
        ControlSheet?.Clear(true);
        ControlSheet.AddObject(Settings.GetSerialized());
    }

}