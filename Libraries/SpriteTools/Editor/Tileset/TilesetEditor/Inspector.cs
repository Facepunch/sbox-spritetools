using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools.TilesetEditor.Inspector;

public class Inspector : Widget
{
    internal MainWindow MainWindow { get; }
    internal int SelectedTab => segmentedControl.SelectedIndex;

    ControlSheet controlSheet;
    ControlSheet selectedTileSheet;
    internal SegmentedControl segmentedControl;

    internal Button btnRegenerate;
    Button btnDeleteAll;
    WarningBox warningBox;
    ExpandGroup selectedTileGroup;
    internal TilesetTileListControl tileList;

    public Inspector(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Inspector";
        WindowTitle = "Inspector";
        SetWindowIcon("manage_search");

        Layout = Layout.Column();
        Layout.Margin = 8;

        controlSheet = new ControlSheet();
        selectedTileSheet = new ControlSheet();

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
        segmentedControl.OnSelectedChanged = (index) =>
        {
            UpdateControlSheet();
        };

        scroller.Canvas.Layout.Add(controlSheet);

        scroller.Canvas.Layout.AddSpacingCell(8);
        selectedTileGroup = scroller.Canvas.Layout.Add(new ExpandGroup(this));
        selectedTileGroup.Title = "Selected Tile";
        selectedTileGroup.SetOpenState(true);
        var w = new Widget();
        w.Layout = Layout.Column();
        w.VerticalSizeMode = SizeMode.CanGrow;
        w.HorizontalSizeMode = SizeMode.Flexible;
        w.Layout.Add(selectedTileSheet);
        w.Layout.AddSpacingCell(8);
        selectedTileGroup.SetWidget(w);

        btnRegenerate = scroller.Canvas.Layout.Add(new Button("Regenerate Tiles", icon: "refresh"));
        btnRegenerate.Clicked = MainWindow.GenerateTiles;
        scroller.Canvas.Layout.AddSpacingCell(8);

        btnDeleteAll = scroller.Canvas.Layout.Add(new Button("Delete All Tiles", icon: "delete"));
        btnDeleteAll.Clicked = MainWindow.DeleteAllTiles;
        scroller.Canvas.Layout.AddSpacingCell(8);

        warningBox = scroller.Canvas.Layout.Add(new WarningBox("", this));
        scroller.Canvas.Layout.AddStretchCell();
        Layout.Add(scroller);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateControlSheet();
        UpdateSelectedSheet();
    }

    [EditorEvent.Hotload]
    void OnHotload()
    {
        UpdateControlSheet();
        UpdateSelectedSheet();
    }

    internal void UpdateControlSheet()
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

            if (prop.Name == "FilePath")
                MainWindow.preview.UpdateTexture(prop.GetValue<string>());
        };

        var props = controlSheet.AddObject(serializedObject, null, (SerializedProperty prop) =>
        {
            if (segmentedControl.SelectedIndex == 0 && prop.GroupName != "Tileset Setup") return false;
            if (segmentedControl.SelectedIndex == 1 && prop.GroupName == "Tileset Setup") return false;
            return prop.HasAttribute<PropertyAttribute>() && !prop.HasAttribute<HideAttribute>();
        });

        foreach (var group in props)
        {
            if (group.Value is not TilesetTileListControl newTileList) continue;
            newTileList.MainWindow = MainWindow;
            tileList = newTileList;
        }

        var setupVisible = segmentedControl.SelectedIndex == 0;
        var hasTiles = (MainWindow?.Tileset?.Tiles?.Count ?? 0) > 0;
        selectedTileGroup.Visible = !setupVisible && hasTiles;
        btnRegenerate.Visible = setupVisible;
        btnRegenerate.Text = hasTiles ? "Regenerate Tiles" : "Generate Tiles";
        btnDeleteAll.Visible = setupVisible && hasTiles;
        warningBox.Visible = setupVisible == hasTiles;
        warningBox.Label.Text =
            setupVisible ? "Pressing \"Regenerate Tiles\" will regenerate all tiles in the tileset. This will remove all your existing tiles. You can undo this action at any time before you close the window."
            : "No tiles have been generated. Make sure you visit the Setup tab to slice the sheet accordingly.";
    }

    internal void UpdateSelectedSheet()
    {
        selectedTileSheet?.Clear(true);

        if (MainWindow.SelectedTiles.Count == 0) return;

        MultiSerializedObject objs = new();
        foreach (var tile in (MainWindow?.SelectedTiles ?? new()))
        {
            if (tile is null) continue;
            objs.Add(tile.GetSerialized());
        }
        objs.Rebuild();

        selectedTileSheet.AddObject(objs, null, (SerializedProperty prop) =>
        {
            return !prop.HasAttribute<HideAttribute>() && prop.HasAttribute<PropertyAttribute>();
        });
    }

}