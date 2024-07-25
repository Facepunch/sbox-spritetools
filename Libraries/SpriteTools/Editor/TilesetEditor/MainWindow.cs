using Editor;
using Editor.NodeEditor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SpriteTools.TilesetEditor;

[EditorForAssetType("tileset")]
[EditorApp("Tileset Editor", "calendar_view_month", "Edit Tilesets")]
public partial class MainWindow : DockWindow, IAssetEditor
{
    public bool CanOpenMultipleAssets => false;

    private readonly UndoStack _undoStack = new();
    public UndoStack UndoStack => _undoStack;
    bool _dirty = true;

    private Asset _asset;
    public TilesetResource Tileset;

    ToolBar toolBar;

    Option _undoMenuOption;
    Option _redoMenuOption;

    public MainWindow()
    {
        DeleteOnClose = true;

        Size = new Vector2(1280, 720);
        Tileset = new TilesetResource();

        SetWindowIcon("emoji_emotions");

        RestoreDefaultDockLayout();
    }

    public void AssetOpen(Asset asset)
    {
        Open("", asset);
        Show();
    }

    public void SelectMember(string memberName)
    {

    }

    void UpdateWindowTitle()
    {
        Title = ($"{_asset.Name} - Tileset Editor" ?? "Untitled Tileset - Tileset Editor") + (_dirty ? "*" : "");
    }

    public void RebuildUI()
    {
        MenuBar.Clear();

        {
            var file = MenuBar.AddMenu("File");
            file.AddOption("New", "common/new.png", () => New(), "CTRL+N").StatusText = "New Tileset";
            file.AddOption("Open", "common/open.png", () => Open(), "Ctrl+O").StatusText = "Open Tileset";
            file.AddOption("Save", "common/save.png", () => Save(), "Ctrl+S").StatusText = "Save Tileset";
            file.AddOption("Save As...", "common/save.png", () => Save(true), "Ctrl+Shift+S").StatusText = "Save Tileset As...";
            file.AddSeparator();
            file.AddOption(new Option("Exit") { Triggered = Close });
        }

        {
            var edit = MenuBar.AddMenu("Edit");
            _undoMenuOption = edit.AddOption("Undo", "undo", () => Undo(), "Ctrl+Z");
            _redoMenuOption = edit.AddOption("Redo", "redo", () => Redo(), "Ctrl+Y");

            // edit.AddSeparator();
            // edit.AddOption( "Cut", "common/cut.png", CutSelection, "Ctrl+X" );
            // edit.AddOption( "Copy", "common/copy.png", CopySelection, "Ctrl+C" );
            // edit.AddOption( "Paste", "common/paste.png", PasteSelection, "Ctrl+V" );
            // edit.AddOption( "Select All", "select_all", SelectAll, "Ctrl+A" );
        }

        {
            var view = MenuBar.AddMenu("View");

            view.AboutToShow += () => OnViewMenu(view);
        }

        CreateToolBar();

    }

    private void OnViewMenu(Menu view)
    {
        view.Clear();
        view.AddOption("Restore To Default", "settings_backup_restore", RestoreDefaultDockLayout);
        view.AddSeparator();

        foreach (var dock in DockManager.DockTypes)
        {
            var o = view.AddOption(dock.Title, dock.Icon);
            o.Checkable = true;
            o.Checked = DockManager.IsDockOpen(dock.Title);
            o.Toggled += (b) => DockManager.SetDockState(dock.Title, b);
        }
    }

    protected override void RestoreDefaultDockLayout()
    {
        var inspector = new Inspector.Inspector(this);
        var preview = new Preview.Preview(this);
        // Timeline = new Timeline.Timeline(this);
        // var animationList = new AnimationList.AnimationList(this);

        DockManager.Clear();
        DockManager.RegisterDockType("Inspector", "edit", () => new Inspector.Inspector(this));
        DockManager.RegisterDockType("Preview", "emoji_emotions", () => new Preview.Preview(this));
        // DockManager.RegisterDockType("Animations", "directions_walk", () => new AnimationList.AnimationList(this));
        // DockManager.RegisterDockType("Timeline", "view_column", () =>
        // {
        //     Timeline = new Timeline.Timeline(this);
        //     return Timeline;
        // });

        DockManager.AddDock(null, inspector, DockArea.Left, DockManager.DockProperty.HideOnClose);
        DockManager.AddDock(null, preview, DockArea.Right, DockManager.DockProperty.HideOnClose, split: 0.8f);

        // DockManager.AddDock(preview, Timeline, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.2f);
        // DockManager.AddDock(inspector, animationList, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.45f);

        DockManager.Update();

        RebuildUI();
    }

    public void New()
    {
        PromptSave(() => CreateNew());
    }

    public void CreateNew()
    {
        var savePath = GetSavePath("New 2D Tileset");

        _asset = null;
        Tileset = AssetSystem.CreateResource("tileset", savePath).LoadResource<TilesetResource>();
        _dirty = false;
        _undoStack.Clear();

        UpdateWindowTitle();
    }

    public void Open()
    {
        var fd = new FileDialog(null)
        {
            Title = "Open 2D Tileset",
            DefaultSuffix = ".tileset"
        };

        fd.SetNameFilter("2D Tileset (*.tileset)");

        if (!fd.Execute()) return;

        PromptSave(() => Open(fd.SelectedFile));
    }

    public void Open(string path, Asset asset = null)
    {
        if (!string.IsNullOrEmpty(path))
        {
            asset ??= AssetSystem.FindByPath(path);
        }
        if (asset == null) return;

        if (asset == _asset)
        {
            Log.Warning($"{asset.RelativePath} is already open.");
            return;
        }

        var tileset = asset.LoadResource<TilesetResource>();
        if (tileset == null)
        {
            Log.Warning($"Failed to load tileset from {asset.RelativePath}");
            return;
        }

        StateCookie = "tileset-editor-window-" + tileset.ResourceId;

        _asset = asset;
        _dirty = false;
        _undoStack.Clear();
        Tileset = tileset;
        UpdateWindowTitle();
    }

    public bool Save(bool saveAs = false)
    {
        var savePath = (_asset == null || saveAs) ? GetSavePath() : _asset.AbsolutePath;
        if (string.IsNullOrWhiteSpace(savePath)) return false;

        if (saveAs)
        {
            // If we're saving as, we want to register the new asset
            _asset = null;
        }

        // Register the asset if we haven't already
        _asset ??= AssetSystem.RegisterFile(savePath);
        _asset.SaveToDisk(Tileset);
        _dirty = false;
        UpdateWindowTitle();

        if (_asset == null)
        {
            Log.Warning($"Failed to register asset at path {savePath}");
            return false;
        }

        MainAssetBrowser.Instance?.UpdateAssetList();

        return true;
    }

    [EditorEvent.Frame]
    void Frame()
    {
        _undoOption.Enabled = _undoStack.CanUndo;
        _redoOption.Enabled = _undoStack.CanRedo;
        _undoMenuOption.Enabled = _undoStack.CanUndo;
        _redoMenuOption.Enabled = _undoStack.CanRedo;

        _undoOption.Text = _undoStack.UndoName ?? "Undo";
        _redoOption.Text = _undoStack.RedoName ?? "Redo";
        _undoMenuOption.Text = _undoStack.UndoName ?? "Undo";
        _redoMenuOption.Text = _undoStack.RedoName ?? "Redo";

        _undoOption.StatusText = _undoStack.UndoName ?? "Undo";
        _redoOption.StatusText = _undoStack.RedoName ?? "Redo";
        _undoMenuOption.StatusText = _undoStack.UndoName ?? "Undo";
        _redoMenuOption.StatusText = _undoStack.RedoName ?? "Redo";
    }

    static string GetSavePath(string title = "Save Tileset")
    {
        var fd = new FileDialog(null)
        {
            Title = title,
            DefaultSuffix = $".tileset"
        };

        fd.SelectFile("untitled.tileset");
        fd.SetFindFile();
        fd.SetModeSave();
        fd.SetNameFilter("2D Tileset (*.tileset)");
        if (!fd.Execute()) return null;

        return fd.SelectedFile;
    }

    internal void PromptImportSpritesheet()
    {
        var picker = new AssetPicker(this, AssetType.ImageFile);
        picker.Window.StateCookie = "TilesetEditor.Import";
        picker.Window.RestoreFromStateCookie();
        picker.Window.Title = $"Import Spritesheet for {Tileset.ResourceName}";
        picker.MultiSelect = false;
        picker.OnAssetPicked = x =>
        {
            var path = x.FirstOrDefault()?.GetSourceFile();
            if (string.IsNullOrEmpty(path)) return;
            var importer = new SpritesheetImporter.SpritesheetImporter(this, path);
            importer.Window.Show();
        };
        picker.Window.Show();
    }

    void PromptSave(Action action)
    {
        if (!_dirty)
        {
            action?.Invoke();
            return;
        }

        var confirm = new PopupWindow(
            "Save Current Tileset", "The open tileset has unsaved changes. Would you like to save before continuing?", "Cancel",
            new Dictionary<string, Action>
            {
                { "No", () => {
                    action?.Invoke();
                } },
                { "Yes", () => {
                    if (Save()) action?.Invoke();
                }}
            });
        confirm.Show();
    }

    public void SetDirty()
    {
        _dirty = true;
        UpdateWindowTitle();
    }

    public void PushUndo(string name, string buffer = "")
    {
        if (string.IsNullOrEmpty(buffer)) buffer = JsonSerializer.Serialize((Tileset.Tiles, Tileset.FilePath, Tileset.AtlasWidth, Tileset.TileSize));
        _undoStack.PushUndo(name, buffer);
    }

    public void PushRedo()
    {
        _undoStack.PushRedo(JsonSerializer.Serialize((Tileset.Tiles, Tileset.FilePath, Tileset.AtlasWidth, Tileset.TileSize)));
    }

    public void Undo()
    {
        if (_undoStack.Undo() is UndoOp op)
        {
            ReloadFromString(op.undoBuffer);
            Sound.Play("ui.navigate.back");
        }
        else
        {
            Sound.Play("ui.navigate.deny");
        }
    }

    private void SetUndoLevel(int level)
    {
        if (_undoStack.SetUndoLevel(level) is UndoOp op)
        {
            ReloadFromString(op.undoBuffer);
        }
    }

    public void Redo()
    {
        if (_undoStack.Redo() is UndoOp op)
        {
            ReloadFromString(op.redoBuffer);
            Sound.Play("ui.navigate.forward");
        }
        else
        {
            Sound.Play("ui.navigate.deny");
        }
    }

    internal void ReloadFromString(string buffer)
    {
        var json = JsonSerializer.Deserialize<(List<TilesetResource.Tile>, string, int, int)>(buffer);

        Tileset.Tiles = json.Item1;
        Tileset.FilePath = json.Item2;
        Tileset.AtlasWidth = json.Item3;
        Tileset.TileSize = json.Item4;

        SetDirty();
    }

    private Option _undoOption;
    private Option _redoOption;

    private void CreateToolBar()
    {
        toolBar?.Destroy();
        toolBar = new ToolBar(this, "TilesetEditorToolbar");
        AddToolBar(toolBar, ToolbarPosition.Top);

        toolBar.AddOption("New", "common/new.png", New).StatusText = "New Tileset";
        toolBar.AddOption("Open", "common/open.png", Open).StatusText = "Open Tileset";
        toolBar.AddOption("Save", "common/save.png", () => Save()).StatusText = "Save Tileset";

        toolBar.AddSeparator();

        _undoOption = toolBar.AddOption("Undo", "undo", Undo);
        _redoOption = toolBar.AddOption("Redo", "redo", Redo);

        toolBar.AddSeparator();

        toolBar.AddSeparator();

        _undoOption.Enabled = false;
        _redoOption.Enabled = false;
    }
}
