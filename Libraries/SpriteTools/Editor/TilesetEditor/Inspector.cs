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
        scroller.Canvas.Layout.Add(new Button("Regenerate Tiles", icon: "refresh")).Clicked = MainWindow.RegenerateTiles;
        scroller.Canvas.Layout.AddSpacingCell(8);
        scroller.Canvas.Layout.Add(new WarningBox("Pressing \"Regenerate Tiles\" will regenerate all tiles in the tileset. This will remove all your existing tiles. You can undo this action at any time before you close the window.", this));
        scroller.Canvas.Layout.AddStretchCell();
        Layout.Add(scroller);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateControlSheet();
    }

    [EditorEvent.Hotload]
    public void UpdateControlSheet()
    {
        controlSheet?.Clear(true);

        var serializedObject = MainWindow.Tileset.GetSerialized();

        serializedObject.OnPropertyChanged += (prop) =>
        {
            if (prop is null) return;
            if (!prop.HasAttribute<PropertyAttribute>()) return;

            var undoName = $"Modify {prop.Name}";

            string buffer = "";
            if (MainWindow.UndoStack.MostRecent is not null)
            {
                if (MainWindow.UndoStack.MostRecent.name == undoName)
                {
                    buffer = MainWindow.UndoStack.MostRecent.undoBuffer;
                    MainWindow.UndoStack.PopMostRecent();
                }
                else
                {
                    buffer = MainWindow.UndoStack.MostRecent.redoBuffer;
                }
            }

            MainWindow.PushUndo(undoName, buffer);
            MainWindow.PushRedo();

            MainWindow.SetDirty();
        };

        controlSheet.AddObject(serializedObject, null, (SerializedProperty prop) =>
        {
            if (segmentedControl.SelectedIndex == 0 && prop.GroupName != "Tileset Setup") return false;
            if (segmentedControl.SelectedIndex == 1 && prop.GroupName == "Tileset Setup") return false;
            return prop.HasAttribute<PropertyAttribute>() && !prop.HasAttribute<HideAttribute>();
        });
    }

}