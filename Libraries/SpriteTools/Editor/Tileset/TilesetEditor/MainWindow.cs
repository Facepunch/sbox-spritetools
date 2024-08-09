using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

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
	[Property]
	public List<TilesetResource.Tile> SelectedTiles
	{
		get => inspector?.tileList?.Selected?.Select(x => x?.Tile)?.ToList() ?? new();
		set
		{
			inspector.tileList.Selected.Clear();
			foreach (var tile in value)
			{
				var control = inspector.tileList.Buttons.FirstOrDefault(x => x.Tile == tile);
				if (control != null)
				{
					inspector.tileList.Selected.Add(control);
				}
			}
		}
	}

	ToolBar toolBar;
	internal Inspector.Inspector inspector;
	internal Preview.Preview preview;

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
		inspector = new Inspector.Inspector(this);
		preview = new Preview.Preview(this);
		// Timeline = new Timeline.Timeline(this);
		// var animationList = new AnimationList.AnimationList(this);

		DockManager.Clear();
		DockManager.RegisterDockType("Inspector", "edit", () => inspector = new Inspector.Inspector(this));
		DockManager.RegisterDockType("Preview", "emoji_emotions", () => preview = new Preview.Preview(this));
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

	void InitInspector()
	{
		inspector.segmentedControl.SelectedIndex = ((Tileset?.Tiles?.Count ?? 0) == 0) ? 0 : 1;
	}

	void UpdateEverything()
	{
		UpdateWindowTitle();
		inspector.UpdateControlSheet();
		inspector.UpdateSelectedSheet();
		preview.UpdateTexture(Tileset.FilePath);
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

		InitInspector();
		UpdateEverything();
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
		var firstTile = Tileset.Tiles?.FirstOrDefault();
		if (firstTile is not null)
			inspector?.tileList?.Selected?.Add(inspector.tileList.Buttons.FirstOrDefault(x => x.Tile == firstTile));

		InitInspector();
		UpdateEverything();
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

	internal void CreateTile(int x, int y, bool add = false)
	{
		var tileName = $"Tile {x},{y}";

		PushUndo($"Create Tile \"{tileName}\"");
		var tile = new TilesetResource.Tile(new Vector2Int(x, y), 1)
		{
			Tileset = Tileset
		};
		Tileset.AddTile(tile);

		if (Tileset.Tiles.Count == 1)
		{
			Tileset.CurrentTileSize = Tileset.TileSize;
			Tileset.CurrentTextureSize = (Vector2Int)preview.TextureSize;
			inspector.UpdateControlSheet();
		}
		else
		{
			var control = new TilesetTileControl(inspector.tileList, tile);
			inspector.tileList.content.Add(control);
			inspector.tileList.Buttons.Add(control);
		}

		SelectTile(tile, add);
		PushRedo();
	}

	internal void SelectTile(TilesetResource.Tile tile, bool add = false)
	{
		var btn = inspector.tileList.Buttons.FirstOrDefault(x => x.Tile == tile);
		if (add)
		{
			if (inspector.tileList.Selected.Contains(btn))
				inspector.tileList.Selected.Remove(btn);
			else
				inspector.tileList.Selected.Add(btn);
		}
		else
		{
			inspector.tileList.Selected.Clear();
			inspector.tileList.Selected.Add(btn);
		}
		inspector.UpdateSelectedSheet();
	}

	internal void DeleteTile(TilesetResource.Tile tile)
	{
		var tileName = tile.Name;
		if (string.IsNullOrEmpty(tileName)) tileName = $"Tile {tile.Position}";

		PushUndo($"Delete Tile \"{tileName}\"");
		bool isSelected = inspector.tileList.Selected.Any(x => x.Tile == tile);
		Tileset.RemoveTile(tile);

		if (isSelected) SelectTile(Tileset.Tiles?.FirstOrDefault() ?? null);
		PushRedo();

		if (Tileset.Tiles.Count == 0)
		{
			inspector.UpdateControlSheet();
		}
		else
		{
			var btns = inspector.tileList.Buttons.ToList();
			foreach (var btn in btns)
			{
				if (btn.Tile == tile)
				{
					inspector.tileList.Buttons.Remove(btn);
					btn.Destroy();
				}
			}
		}
	}

	internal void GenerateTiles()
	{
		if (Tileset is null) return;

		PushUndo("Generate Tiles");
		Tileset.Tiles.Clear();
		Tileset.TileMap.Clear();
		Tileset.CurrentTileSize = Tileset.TileSize;
		Tileset.CurrentTextureSize = (Vector2Int)preview.TextureSize;

		int x = 0;
		int y = 0;
		int framesPerRow = (int)preview.TextureSize.x / Tileset.TileSize.x;
		int framesPerHeight = (int)preview.TextureSize.y / Tileset.TileSize.y;

		while (y < framesPerHeight)
		{
			while (x < framesPerRow)
			{
				Tileset.AddTile(new TilesetResource.Tile(new Vector2Int(x, y), 1));
				x++;
			}
			x = 0;
			y++;
		}
		PushRedo();

		Tileset.InternalUpdateTiles();

		UpdateEverything();
	}

	internal void DeleteAllTiles()
	{
		if (Tileset is null) return;

		PushUndo("Delete All Tiles");
		Tileset.Tiles ??= new List<TilesetResource.Tile>();
		Tileset.Tiles?.Clear();
		PushRedo();

		UpdateEverything();
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

	internal void SetDirty()
	{
		_dirty = true;
		UpdateWindowTitle();
	}

	public void PushUndo(string name, string buffer = "")
	{
		if (string.IsNullOrEmpty(buffer)) buffer = Tileset.Serialize();
		_undoStack.PushUndo(name, buffer);
	}

	public void PushRedo()
	{
		_undoStack.PushRedo(Tileset.Serialize());
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
		Tileset.Deserialize(buffer);

		SetDirty();
		UpdateEverything();
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
