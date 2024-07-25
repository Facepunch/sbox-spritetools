using Editor;
using Sandbox;
using System;
using System.Linq;

namespace SpriteTools.TilesetEditor.Inspector;

public class Inspector : Widget
{
    public SpriteResource Sprite { get; set; }
    public MainWindow MainWindow { get; }

    ControlSheet controlSheet;

    public Inspector(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Inspector";
        WindowTitle = "Inspector";
        SetWindowIcon("manage_search");

        Layout = Layout.Column();
        controlSheet = new ControlSheet();

        MinimumWidth = 350f;

        var scroller = new ScrollArea(this);
        scroller.Canvas = new Widget();
        scroller.Canvas.Layout = Layout.Column();
        scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
        scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;

        var importLayout = scroller.Canvas.Layout.Add(Layout.Row());
        importLayout.Margin = new Sandbox.UI.Margin(16, 8, 16, 0);
        var importButton = new Button(this);
        importButton.Text = "Import Tileset";
        importButton.Icon = "add_to_photos";
        importButton.ToolTip = "Import a tileset image to split into different tiles.";
        importButton.MouseClick = MainWindow.PromptImportSpritesheet;
        importLayout.Add(importButton);

        scroller.Canvas.Layout.Add(controlSheet);
        scroller.Canvas.Layout.AddStretchCell();
        Layout.Add(scroller);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateControlSheet();
    }

    [EditorEvent.Hotload]
    void UpdateControlSheet()
    {
        controlSheet?.Clear(true);
    }


}