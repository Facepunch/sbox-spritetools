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
    SegmentedControl segmentedControl;

    public Inspector(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Inspector";
        WindowTitle = "Inspector";
        SetWindowIcon("manage_search");

        Layout = Layout.Column();
        Layout.Margin = 8;

        controlSheet = new ControlSheet();

        MinimumWidth = 350f;

        var scroller = new ScrollArea(this);
        scroller.Canvas = new Widget();
        scroller.Canvas.Layout = Layout.Column();
        scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
        scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;

        var importLayout = scroller.Canvas.Layout.Add(Layout.Row());
        importLayout.Margin = new Sandbox.UI.Margin(16, 8, 16, 0);

        segmentedControl = Layout.Add(new SegmentedControl());
        segmentedControl.AddOption("Setup", "auto_fix_high");
        segmentedControl.AddOption("Tiles", "grid_on");
        segmentedControl.SelectedIndex = string.IsNullOrEmpty(MainWindow.Tileset.FilePath) ? 0 : 1;
        segmentedControl.OnSelectedChanged = (index) =>
        {
            UpdateControlSheet();
        };

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

        controlSheet.AddObject(MainWindow.Tileset.GetSerialized(), null, (SerializedProperty prop) =>
        {
            if (segmentedControl.SelectedIndex == 0 && prop.GroupName != "Tileset Setup") return false;
            if (segmentedControl.SelectedIndex == 1 && prop.GroupName == "Tileset Setup") return false;
            return prop.HasAttribute<PropertyAttribute>() && !prop.HasAttribute<HideAttribute>();
        });
    }


}