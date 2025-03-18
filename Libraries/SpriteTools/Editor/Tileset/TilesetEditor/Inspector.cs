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
    ControlSheet selectedAutotileSheet;
    internal SegmentedControl segmentedControl;

    internal Button btnRegenerate;
    Button btnDeleteAll;
    WarningBox warningBox;
    ExpandGroup selectedTileGroup;
    ExpandGroup selectedAutotileGroup;
    internal TilesetTileListControl tileList;
    internal AutotileBrushListControl autotileBrushList;

    AutotileType SelectedAutotileType
    {
        get => _selectedAutotileType;
        set
        {
            if (_selectedAutotileType == value) return;

            var brush = autotileBrushList?.SelectedBrush?.Brush;
            if (brush is null) return;

            if ((brush.Tiles?.Length ?? 0) > 0 && brush.Tiles.Any(x => (x?.Tiles?.Count ?? 0) > 0))
            {
                var popup = new PopupWindow(
                    "Change Brush Type?",
                    "Are you sure you want to change the Brush Type?\nThis will remove all existing tiles in the brush.",
                    "Cancel", new Dictionary<string, Action>{
                        {"OK", () => {
                            _selectedAutotileType = value;
                            brush.SetAutotileType(value);
                        }}
                    });

                popup.Show();
            }
            else
            {
                _selectedAutotileType = value;
                brush.SetAutotileType(value);
            }
        }
    }
    AutotileType _selectedAutotileType;

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
        selectedAutotileSheet = new ControlSheet();

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
        segmentedControl.AddOption("Autotile Brushes", "brush");
        segmentedControl.OnSelectedChanged = (index) =>
        {
            UpdateControlSheet();
        };

        scroller.Canvas.Layout.Add(controlSheet);

        scroller.Canvas.Layout.AddSpacingCell(8);

        {
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
        }

        {
            selectedAutotileGroup = scroller.Canvas.Layout.Add(new ExpandGroup(this));
            selectedAutotileGroup.Title = "Selected Autotile";
            selectedAutotileGroup.SetOpenState(true);
            var w = new Widget();
            w.Layout = Layout.Column();
            w.VerticalSizeMode = SizeMode.CanGrow;
            w.HorizontalSizeMode = SizeMode.Flexible;
            w.Layout.Add(selectedAutotileSheet);
            w.Layout.AddSpacingCell(8);
            selectedAutotileGroup.SetWidget(w);
        }

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
        UpdateSelectedAutotileSheet();
    }

    [EditorEvent.Hotload]
    void OnHotload()
    {
        UpdateControlSheet();
        UpdateSelectedSheet();
        UpdateSelectedAutotileSheet();
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

            if (prop.Name == "FilePath")
                MainWindow.preview.UpdateTexture(prop.GetValue<string>());
        };

        controlSheet.AddObject(serializedObject, (SerializedProperty prop) =>
        {
            if (segmentedControl.SelectedIndex == 0 && prop.GroupName != "Tileset Setup") return false;
            if (segmentedControl.SelectedIndex > 0 && prop.GroupName == "Tileset Setup") return false;
            if (segmentedControl.SelectedIndex == 1 && (prop.GroupName?.Contains("Autotile") ?? false)) return false;
            if (segmentedControl.SelectedIndex == 2 && !(prop.GroupName?.Contains("Autotile") ?? false)) return false;
            return prop.HasAttribute<PropertyAttribute>() && !prop.HasAttribute<HideAttribute>();
        });

        var setupVisible = segmentedControl.SelectedIndex == 0;
        var hasTiles = (MainWindow?.Tileset?.Tiles?.Count ?? 0) > 0;
        selectedTileGroup.Visible = (segmentedControl.SelectedIndex == 1) && hasTiles;
        selectedAutotileGroup.Visible = (segmentedControl.SelectedIndex == 2) && (MainWindow?.Tileset?.AutotileBrushes?.Count ?? 0) > 0;
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

        objs.OnPropertyChanged += (prop) =>
        {
            MainWindow.SetDirty();
        };
        selectedTileSheet.AddObject(objs, (SerializedProperty prop) =>
        {
            return !prop.HasAttribute<HideAttribute>() && prop.HasAttribute<PropertyAttribute>();
        });
    }

    internal void UpdateSelectedAutotileSheet()
    {
        selectedAutotileSheet?.Clear(true);

        if (autotileBrushList?.SelectedTile is not null)
        {

            var serializedObject = autotileBrushList.SelectedTile.GetSerialized();
            selectedAutotileSheet.AddObject(serializedObject, (SerializedProperty prop) =>
            {
                return !prop.HasAttribute<HideAttribute>() && prop.HasAttribute<PropertyAttribute>();
            });

            return;
        }

        if (autotileBrushList?.SelectedBrush is null) return;

        _selectedAutotileType = autotileBrushList.SelectedBrush.Brush.AutotileType;

        var serializedBrush = autotileBrushList.SelectedBrush.Brush.GetSerialized();
        selectedAutotileSheet.AddObject(serializedBrush, (SerializedProperty prop) =>
        {
            return !prop.HasAttribute<HideAttribute>() && prop.HasAttribute<PropertyAttribute>();
        });

        selectedAutotileSheet.AddObject(this.GetSerialized(), (SerializedProperty prop) =>
        {
            return prop.Name == "SelectedAutotileType";
        });
    }

}