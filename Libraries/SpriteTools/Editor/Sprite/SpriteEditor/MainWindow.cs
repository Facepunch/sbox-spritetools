using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SpriteTools.SpriteEditor;

[EditorForAssetType( "sprite" )]
[EditorApp( "Sprite Editor", "emoji_emotions", "Edit 2D Sprites" )]
public partial class MainWindow : DockWindow, IAssetEditor
{
	public Action OnAssetLoaded;
	public Action OnTextureUpdate;
	public Action OnAnimationChanges;
	public Action OnAnimationSelected;
	public Action OnPlayPause;

	public bool CanOpenMultipleAssets => false;

	private readonly UndoStack _undoStack = new();
	public UndoStack UndoStack => _undoStack;
	bool _dirty = true;

	private Asset _asset;
	public SpriteResource Sprite;
	public SpriteAnimation SelectedAnimation
	{
		get => _selectedAnimation;
		set
		{
			_selectedAnimation = value;
			CurrentFrameIndex = 0;
		}
	}
	private SpriteAnimation _selectedAnimation;
	public int CurrentFrameIndex
	{
		get => _currentFrameIndex;
		set
		{
			_currentFrameIndex = value;
			OnTextureUpdate?.Invoke();
		}
	}

	private int _currentFrameIndex = 0;
	RealTimeSince frameTimer = 0;
	float FrameTime => ( ( SelectedAnimation?.FrameRate ?? 0 ) == 0 ) ? 0 : ( 1f / ( SelectedAnimation?.FrameRate ?? 30 ) );

	public bool Playing = true;
	public Timeline.Timeline Timeline;
	ToolBar toolBar;

	Option _undoMenuOption;
	Option _redoMenuOption;
	bool _isPingPonging = false;

	public MainWindow ()
	{
		DeleteOnClose = true;

		Size = new Vector2( 1280, 720 );
		Sprite = new SpriteResource();
		Sprite.Animations.Clear();

		SetWindowIcon( "emoji_emotions" );

		RestoreDefaultDockLayout();
	}

	protected override void OnKeyPress ( KeyEvent e )
	{
		base.OnKeyPress( e );

		if ( e.Key == KeyCode.Space )
		{
			PlayPause();
		}
	}

	public void AssetOpen ( Asset asset )
	{
		Open( "", asset );
		Show();
	}

	public void SelectMember ( string memberName )
	{

	}

	void UpdateWindowTitle ()
	{
		Title = $"{_asset?.Name ?? "Untitled Sprite"} - Sprite Editor" + ( _dirty ? "*" : "" );
	}

	public void RebuildUI ()
	{
		MenuBar.Clear();

		{
			var file = MenuBar.AddMenu( "File" );
			file.AddOption( "New", "common/new.png", () => New(), "editor.new" ).StatusTip = "New Sprite";
			file.AddOption( "Open", "common/open.png", () => Open(), "editor.open" ).StatusTip = "Open Sprite";
			file.AddOption( "Save", "common/save.png", () => Save(), "editor.save" ).StatusTip = "Save Sprite";
			file.AddOption( "Save As...", "common/save.png", () => Save( true ), "editor.save-as" ).StatusTip = "Save Sprite As...";
			file.AddSeparator();
			file.AddOption( new Option( "Exit" ) { Triggered = Close } );
		}

		{
			var edit = MenuBar.AddMenu( "Edit" );
			_undoMenuOption = edit.AddOption( "Undo", "undo", () => Undo(), "editor.undo" );
			_redoMenuOption = edit.AddOption( "Redo", "redo", () => Redo(), "editor.redo" );

			// edit.AddSeparator();
			// edit.AddOption( "Cut", "common/cut.png", CutSelection, "Ctrl+X" );
			// edit.AddOption( "Copy", "common/copy.png", CopySelection, "Ctrl+C" );
			// edit.AddOption( "Paste", "common/paste.png", PasteSelection, "Ctrl+V" );
			// edit.AddOption( "Select All", "select_all", SelectAll, "Ctrl+A" );
		}

		{
			var view = MenuBar.AddMenu( "View" );

			view.AboutToShow += () => OnViewMenu( view );
		}

		CreateToolBar();

	}

	private void OnViewMenu ( Menu view )
	{
		view.Clear();
		view.AddOption( "Restore To Default", "settings_backup_restore", RestoreDefaultDockLayout );
		view.AddSeparator();

		foreach ( var dock in DockManager.DockTypes )
		{
			var o = view.AddOption( dock.Title, dock.Icon );
			o.Checkable = true;
			o.Checked = DockManager.IsDockOpen( dock.Title );
			o.Toggled += ( b ) => DockManager.SetDockState( dock.Title, b );
		}
	}

	protected override void RestoreDefaultDockLayout ()
	{
		var inspector = new Inspector.Inspector( this );
		var preview = new Preview.Preview( this );
		Timeline = new Timeline.Timeline( this );
		var animationList = new AnimationList.AnimationList( this );
		// var errorList = new ErrorList( null, this );

		DockManager.Clear();
		DockManager.RegisterDockType( "Inspector", "edit", () => new Inspector.Inspector( this ) );
		DockManager.RegisterDockType( "Animations", "directions_walk", () => new AnimationList.AnimationList( this ) );
		DockManager.RegisterDockType( "Preview", "emoji_emotions", () => new Preview.Preview( this ) );
		DockManager.RegisterDockType( "Timeline", "view_column", () =>
		{
			Timeline = new Timeline.Timeline( this );
			return Timeline;
		} );
		// DockManager.RegisterDockType( "ErrorList", "error", () => new ErrorList( null, this ) );

		DockManager.AddDock( null, inspector, DockArea.Left, DockManager.DockProperty.HideOnClose );
		DockManager.AddDock( null, preview, DockArea.Right, DockManager.DockProperty.HideOnClose, split: 0.8f );

		DockManager.AddDock( preview, Timeline, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.2f );
		DockManager.AddDock( inspector, animationList, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.45f );

		// DockManager.AddDock( inspector, errorList, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.75f );

		DockManager.Update();

		RebuildUI();
	}

	[Shortcut( "editor.new", "CTRL+N", ShortcutType.Window )]
	public void New ()
	{
		PromptSave( () => CreateNew() );
	}

	public void CreateNew ( string savePath = null )
	{
		if ( string.IsNullOrEmpty( savePath ) ) savePath = GetSavePath( "New Sprite" );

		_asset = null;
		Sprite = AssetSystem.CreateResource( "sprite", savePath ).LoadResource<SpriteResource>();
		_dirty = false;
		_undoStack.Clear();

		if ( Sprite.Animations.Count > 0 )
		{
			SelectedAnimation = Sprite.Animations[0];
			OnAnimationSelected?.Invoke();
		}

		UpdateWindowTitle();
		OnAssetLoaded?.Invoke();
		OnTextureUpdate?.Invoke();
	}

	[Shortcut( "editor.open", "CTRL+O", ShortcutType.Window )]
	public void Open ()
	{
		var fd = new FileDialog( null )
		{
			Title = "Open Sprite",
			DefaultSuffix = ".sprite"
		};

		fd.SetNameFilter( "2D Sprite (*.sprite)" );

		if ( !fd.Execute() ) return;

		PromptSave( () => Open( fd.SelectedFile ) );
	}

	public void Open ( string path, Asset asset = null )
	{
		if ( !string.IsNullOrEmpty( path ) )
		{
			asset ??= AssetSystem.FindByPath( path );
		}
		if ( asset == null ) return;

		if ( asset == _asset )
		{
			Focus();
			return;
		}

		var sprite = asset.LoadResource<SpriteResource>();
		if ( sprite == null )
		{
			Log.Warning( $"Failed to load sprite from {asset.RelativePath}" );
			return;
		}

		StateCookie = "sprite-editor-window-" + sprite.ResourceId;

		_asset = asset;
		_dirty = false;
		_undoStack.Clear();
		Sprite = sprite;
		UpdateWindowTitle();

		OnAssetLoaded?.Invoke();
		OnTextureUpdate?.Invoke();

		if ( ( Sprite.Animations?.Count ?? 0 ) > 0 )
		{
			SelectedAnimation = Sprite.Animations[0];
			OnAnimationSelected?.Invoke();
		}
	}

	private void Restore ()
	{
		var path = _asset?.AbsolutePath;
		if ( string.IsNullOrEmpty( path ) )
		{
			_dirty = false;
			return;
		}

		var contents = File.ReadAllText( path );
		var json = Json.ParseToJsonObject( contents );
		var animations = json["Animations"];
		ReloadFromString( animations.ToJsonString() );

		_dirty = false;
	}

	[Shortcut( "editor.save", "CTRL+S", ShortcutType.Window )]
	public bool Save ( bool saveAs = false )
	{
		var savePath = ( _asset == null || saveAs ) ? GetSavePath() : _asset.AbsolutePath;
		if ( string.IsNullOrWhiteSpace( savePath ) ) return false;

		if ( saveAs )
		{
			// If we're saving as, we want to register the new asset
			_asset = null;
		}

		// Register the asset if we haven't already
		_asset ??= AssetSystem.CreateResource( "sprite", savePath );
		_asset.SaveToDisk( Sprite );
		_dirty = false;
		UpdateWindowTitle();

		if ( _asset == null )
		{
			Log.Warning( $"Failed to register asset at path {savePath}" );
			return false;
		}

		MainAssetBrowser.Instance?.Local?.UpdateAssetList();

		return true;
	}

	[Shortcut( "editor.save-as", "CTRL+SHIFT+S", ShortcutType.Window )]
	private void SaveAs ()
	{
		Save( true );
	}

	[EditorEvent.Frame]
	void Frame ()
	{
		if ( SelectedAnimation is null ) return;
		if ( SelectedAnimation?.Frames?.Count == 0 ) return;
		if ( FrameTime == 0 ) return;

		if ( Playing )
		{
			if ( SelectedAnimation.LoopMode != SpriteResource.LoopMode.PingPong )
			{
				_isPingPonging = false;
			}
			while ( frameTimer >= FrameTime )
			{
				AdvanceFrame();
			}
		}
		else
		{
			frameTimer = 0f;
		}

		_undoOption.Enabled = _undoStack.CanUndo;
		_redoOption.Enabled = _undoStack.CanRedo;
		_undoMenuOption.Enabled = _undoStack.CanUndo;
		_redoMenuOption.Enabled = _undoStack.CanRedo;

		_undoOption.Text = _undoStack.UndoName ?? "Undo";
		_redoOption.Text = _undoStack.RedoName ?? "Redo";
		_undoMenuOption.Text = _undoStack.UndoName ?? "Undo";
		_redoMenuOption.Text = _undoStack.RedoName ?? "Redo";

		_undoOption.StatusTip = _undoStack.UndoName ?? "Undo";
		_redoOption.StatusTip = _undoStack.RedoName ?? "Redo";
		_undoMenuOption.StatusTip = _undoStack.UndoName ?? "Undo";
		_redoMenuOption.StatusTip = _undoStack.RedoName ?? "Redo";
	}

	void AdvanceFrame ()
	{
		var playbackSpeed = _isPingPonging ? -1 : 1;
		var nextFrame = CurrentFrameIndex + playbackSpeed;
		var loopStart = SelectedAnimation.GetLoopStart();
		var loopEnd = SelectedAnimation.GetLoopEnd();
		if ( nextFrame > loopEnd && playbackSpeed > 0 )
		{
			if ( SelectedAnimation.LoopMode == SpriteResource.LoopMode.Forward )
			{
				nextFrame = loopStart;
			}
			else if ( SelectedAnimation.LoopMode == SpriteResource.LoopMode.PingPong )
			{
				_isPingPonging = true;
				nextFrame = Math.Max( loopEnd - 1, loopStart );
			}
			else if ( nextFrame >= SelectedAnimation.Frames.Count )
			{
				nextFrame = SelectedAnimation.Frames.Count - 1;
				PlayPause();
			}
		}
		else if ( nextFrame < loopStart && playbackSpeed < 0 )
		{
			if ( SelectedAnimation.LoopMode == SpriteResource.LoopMode.Forward )
			{
				nextFrame = loopEnd;
			}
			else if ( SelectedAnimation.LoopMode == SpriteResource.LoopMode.PingPong )
			{
				_isPingPonging = false;
				nextFrame = Math.Min( loopStart + 1, loopEnd );
			}
			else
			{
				nextFrame = 0;
				PlayPause();
			}
		}
		CurrentFrameIndex = nextFrame;
		frameTimer -= FrameTime;
		if ( CurrentFrameIndex == 0 && SelectedAnimation.LoopMode == SpriteResource.LoopMode.None )
		{
			Playing = false;
			CurrentFrameIndex = SelectedAnimation.Frames.Count - 1;
			frameTimer = 0;
		}
	}

	protected override bool OnClose ()
	{
		if ( _dirty )
		{
			var confirm = new PopupWindow(
				"Save Current Sprite", "The open sprite has unsaved changes. Would you like to save now?", "Cancel",
				new Dictionary<string, System.Action>()
				{
					{ "No", () => { Restore(); Close(); } },
					{ "Yes", () => { Save(); Close(); } }
				}
			);

			confirm.Show();

			return false;
		}

		return true;
	}

	static string GetSavePath ( string title = "Save Sprite" )
	{
		var fd = new FileDialog( null )
		{
			Title = title,
			DefaultSuffix = $".sprite"
		};

		fd.SelectFile( "untitled.sprite" );
		fd.SetFindFile();
		fd.SetModeSave();
		fd.SetNameFilter( "2D Sprite (*.sprite)" );
		if ( !fd.Execute() ) return null;

		return fd.SelectedFile;
	}

	internal void PromptImportSpritesheet ()
	{
		if ( SelectedAnimation is null )
		{
			var popup = new PopupWindow( "No Animation Selected", "Please select an animation to import a spritesheet into.", "OK", null );
			popup.Show();
			return;
		}
		var picker = AssetPicker.Create( this, AssetType.ImageFile, new() { EnableMultiselect = false, EnableCloud = false } );
		picker.Window.StateCookie = "SpriteEditor.Import";
		picker.Window.RestoreFromStateCookie();
		picker.Window.Title = $"Import Spritesheet for {SelectedAnimation.Name}";
		picker.OnAssetPicked = x =>
		{
			var path = x.FirstOrDefault()?.GetSourceFile();
			if ( string.IsNullOrEmpty( path ) ) return;
			var importer = new SpritesheetImporter.SpritesheetImporter( this, path );
			importer.OnImport += OnSpritesheetImport;
			importer.Window.Show();
		};
		picker.Window.Show();
	}

	void OnSpritesheetImport ( string path, List<Rect> frames )
	{
		PushUndo( $"Import Spritesheet with {frames.Count} frames" );
		if ( SelectedAnimation is not null )
		{
			SelectedAnimation.Frames.Clear();
			foreach ( var frame in frames )
			{
				SelectedAnimation.Frames.Add( new SpriteAnimationFrame( path ) { SpriteSheetRect = frame } );
			}
		}

		Timeline.UpdateFrameList();
		PushRedo();
	}

	void PromptSave ( Action action )
	{
		if ( !_dirty )
		{
			action?.Invoke();
			return;
		}

		var confirm = new PopupWindow(
			"Save Current Sprite", "The open sprite has unsaved changes. Would you like to save before continuing?", "Cancel",
			new Dictionary<string, Action>
			{
				{ "No", () => {
					action?.Invoke();
				} },
				{ "Yes", () => {
					if (Save()) action?.Invoke();
				}}
			} );
		confirm.Show();
	}

	public void PlayPause ()
	{
		_isPingPonging = false;
		Playing = !Playing;
		if ( Playing && SelectedAnimation.LoopMode == SpriteResource.LoopMode.None && CurrentFrameIndex >= SelectedAnimation.Frames.Count - 1 )
		{
			CurrentFrameIndex = 0;
		}

		OnPlayPause?.Invoke();
	}

	public void FrameNext ()
	{
		var frame = CurrentFrameIndex + 1; ;
		if ( frame >= SelectedAnimation.Frames.Count )
		{
			frame = 0;
		}
		CurrentFrameIndex = frame;
	}

	public void FramePrevious ()
	{
		var frame = CurrentFrameIndex - 1;
		if ( frame < 0 )
		{
			frame = SelectedAnimation.Frames.Count - 1;
		}
		CurrentFrameIndex = frame;
	}

	public void FrameFirst ()
	{
		CurrentFrameIndex = 0;
	}

	public void FrameLast ()
	{
		CurrentFrameIndex = SelectedAnimation.Frames.Count - 1;
	}

	internal void SetDirty ()
	{
		_dirty = true;
		UpdateWindowTitle();
	}

	public void PushUndo ( string name, string buffer = "" )
	{
		if ( string.IsNullOrEmpty( buffer ) ) buffer = JsonSerializer.Serialize( Sprite.Animations );
		_undoStack.PushUndo( name, buffer );
	}

	public void PushRedo ()
	{
		_undoStack.PushRedo( JsonSerializer.Serialize( Sprite.Animations ) );
		SetDirty();
	}

	[Shortcut( "editor.undo", "CTRL+Z", ShortcutType.Window )]
	public void Undo ()
	{
		if ( _undoStack.Undo() is UndoOp op )
		{
			ReloadFromString( op.undoBuffer );
			Sound.Play( "ui.navigate.back" );
		}
		else
		{
			Sound.Play( "ui.navigate.deny" );
		}
	}

	private void SetUndoLevel ( int level )
	{
		if ( _undoStack.SetUndoLevel( level ) is UndoOp op )
		{
			ReloadFromString( op.undoBuffer );
		}
	}

	[Shortcut( "editor.redo", "CTRL+Y", ShortcutType.Window )]
	public void Redo ()
	{
		if ( _undoStack.Redo() is UndoOp op )
		{
			ReloadFromString( op.redoBuffer );
			Sound.Play( "ui.navigate.forward" );
		}
		else
		{
			Sound.Play( "ui.navigate.deny" );
		}
	}

	internal void ReloadFromString ( string buffer )
	{
		var selectedName = SelectedAnimation?.Name;
		Sprite.Animations = JsonSerializer.Deserialize<List<SpriteAnimation>>( buffer );

		if ( Sprite.Animations.Any( x => x.Name == selectedName ) )
		{
			SelectedAnimation = Sprite.Animations.FirstOrDefault( x => x.Name == selectedName );
		}
		else
		{
			SelectedAnimation = Sprite.Animations.FirstOrDefault();
		}

		OnAssetLoaded?.Invoke();
		OnAnimationSelected?.Invoke();
		OnAnimationChanges?.Invoke();
		SetDirty();
	}

	private Option _undoOption;
	private Option _redoOption;

	private void CreateToolBar ()
	{
		toolBar?.Destroy();
		toolBar = new ToolBar( this, "SpriteEditorToolbar" );
		AddToolBar( toolBar, ToolbarPosition.Top );

		toolBar.AddOption( "New", "common/new.png", New ).StatusTip = "New Sprite";
		toolBar.AddOption( "Open", "common/open.png", Open ).StatusTip = "Open Sprite";
		toolBar.AddOption( "Save", "common/save.png", () => Save() ).StatusTip = "Save Sprite";

		toolBar.AddSeparator();

		_undoOption = toolBar.AddOption( "Undo", "undo", Undo );
		_redoOption = toolBar.AddOption( "Redo", "redo", Redo );

		toolBar.AddSeparator();

		toolBar.AddSeparator();

		_undoOption.Enabled = false;
		_redoOption.Enabled = false;
	}
}
