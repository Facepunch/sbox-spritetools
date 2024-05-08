using Editor;
using Editor.NodeEditor;
using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools.SpriteEditor;

[EditorForAssetType( "sprite" )]
public partial class MainWindow : DockWindow, IAssetEditor
{
    public Action OnAssetLoaded;
    public Action OnTextureUpdate;
    public Action OnAnimationChanges;
    public Action OnAnimationSelected;

    internal static List<MainWindow> AllWindows { get; } = new List<MainWindow>();
    public bool CanOpenMultipleAssets => true;

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
    public string CurrentTexturePath => (SelectedAnimation?.Frames?.Count > 0) ? (SelectedAnimation?.Frames[CurrentFrameIndex] ?? "") : "";
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
    float FrameTime => ((SelectedAnimation?.FrameRate ?? 0) == 0) ? 0 : (1f / (SelectedAnimation?.FrameRate ?? 30));

    public bool Playing = false;

    public MainWindow()
    {
        DeleteOnClose = true;

        Size = new Vector2( 1280, 720 );

        // Make this window stay on top of the editor, by making it a dialog
        Parent = EditorWindow;
        WindowFlags = WindowFlags.Dialog | WindowFlags.Customized | WindowFlags.CloseButton | WindowFlags.WindowSystemMenuHint | WindowFlags.WindowTitle | WindowFlags.MaximizeButton;

        SetWindowIcon( "emoji_emotions" );

        AllWindows.Add( this );

        RestoreDefaultDockLayout();
    }

    protected override void OnClosed()
    {
        base.OnClosed();

        AllWindows.Remove( this );
    }

    protected override void OnFocus( FocusChangeReason reason )
    {
        base.OnFocus( reason );

        // Move this window to the end of the list, so it has priority
        // when opening a new graph

        AllWindows.Remove( this );
        AllWindows.Add( this );
    }

    public void AssetOpen( Asset asset )
    {
        Sprite = asset.LoadResource<SpriteResource>();

        if ( (Sprite.Animations?.Count ?? 0) > 0 )
        {
            SelectedAnimation = Sprite.Animations[0];
            OnAnimationSelected?.Invoke();
        }

        Title = $"{asset.Name} - Sprite Editor" ?? "Untitled Sprite - Sprite Editor";

        OnAssetLoaded?.Invoke();
        OnTextureUpdate?.Invoke();
        Show();
    }

    public void SelectMember( string memberName )
    {

    }

    public void RebuildUI()
    {
        MenuBar.Clear();

        {
            var file = MenuBar.AddMenu( "File" );
            file.AddOption( new Option( "Save" ) { Shortcut = "Ctrl+S", Triggered = Save } );
            file.AddSeparator();
            file.AddOption( new Option( "Exit" ) { Triggered = Close } );
        }

        {
            var edit = MenuBar.AddMenu( "Edit" );
            // _undoMenuOption = edit.AddOption( "Undo", "undo", Undo, "Ctrl+Z" );
            // _redoMenuOption = edit.AddOption( "Redo", "redo", Redo, "Ctrl+Y" );

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
    }

    private void OnViewMenu( Menu view )
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

    protected override void RestoreDefaultDockLayout()
    {
        var inspector = new Inspector( this );
        var preview = new Preview.Preview( this );
        var timeline = new Timeline.Timeline( this );
        var animationList = new AnimationList.AnimationList( this );
        // var errorList = new ErrorList( null, this );

        DockManager.Clear();
        DockManager.RegisterDockType( "Inspector", "edit", () => new Inspector( this ) );
        DockManager.RegisterDockType( "Animations", "directions_walk", () => new AnimationList.AnimationList( this ) );
        DockManager.RegisterDockType( "Preview", "emoji_emotions", () => new Preview.Preview( this ) );
        DockManager.RegisterDockType( "Frames", "view_column", () => new Timeline.Timeline( this ) );
        // DockManager.RegisterDockType( "ErrorList", "error", () => new ErrorList( null, this ) );

        DockManager.AddDock( null, inspector, DockArea.Left, DockManager.DockProperty.HideOnClose );
        DockManager.AddDock( null, preview, DockArea.Right, DockManager.DockProperty.HideOnClose, split: 0.65f );

        DockManager.AddDock( preview, timeline, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.25f );
        DockManager.AddDock( inspector, animationList, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.45f );

        // DockManager.AddDock( inspector, errorList, DockArea.Bottom, DockManager.DockProperty.HideOnClose, split: 0.75f );

        DockManager.Update();

        RebuildUI();
    }

    public void Save()
    {

    }

    [EditorEvent.Frame]
    void Frame()
    {
        if ( SelectedAnimation is null ) return;
        if ( SelectedAnimation?.Frames?.Count == 0 ) return;
        if ( FrameTime == 0 ) return;

        while ( frameTimer >= FrameTime )
        {
            CurrentFrameIndex = (CurrentFrameIndex + 1) % SelectedAnimation.Frames.Count;
            frameTimer -= FrameTime;
        }
    }
}
