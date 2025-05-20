using Editor;
using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools.SpritesheetImporter;

public class SpritesheetImporterFrame
{
	public Guid Id { get; set; }
	public Rect Rect { get; set; }

	public SpritesheetImporterFrame ( Rect rect )
	{
		Id = Guid.NewGuid();
		Rect = rect;
	}

	public override int GetHashCode ()
	{
		return System.HashCode.Combine( Rect );
	}
	public override bool Equals ( object obj )
	{
		if ( obj is SpritesheetImporterFrame other )
		{
			return Id == other.Id;
		}
		return false;
	}
}

public class SpritesheetImporter : Dialog
{
	public string Path { get; set; }
	Preview Preview { get; set; }
	public Action<string, List<Rect>> OnImport { get; set; }
	public ImportSettings Settings { get; set; } = new ImportSettings();

	internal bool HasModified = false;
	internal List<SpritesheetImporterFrame> Frames = new List<SpritesheetImporterFrame>();
	internal int PageIndex => SegmentedControl?.SelectedIndex ?? 0;

	ScrollArea ScrollArea { get; set; }
	SegmentedControl SegmentedControl { get; set; }

	public SpritesheetImporter ( Widget parent, string path ) : base( parent, false )
	{
		Path = path;

		Window.Title = "Spritesheet Importer";
		Window.WindowTitle = "Spritesheet Importer";
		Window.Size = new Vector2( 960, 540 );
		Window.SetModal( true );
		Window.MinimumSize = 200;
		Window.MaximumSize = 10000;

		var settings = EditorCookie.Get<ImportSettings>( "SpritesheetImporterSettings", null );
		if ( settings != null )
		{
			Settings = settings;
		}

		BuildLayout();
	}

	public void CommitFrames ( List<Rect> frames )
	{
		foreach ( var frame in frames )
		{
			var newFrame = new SpritesheetImporterFrame( frame );
			if ( Frames.Contains( newFrame ) ) continue;
			Frames.Add( newFrame );
		}
	}

	void BuildLayout ()
	{
		Layout = Layout.Row();

		var leftSide = Layout.Column();
		leftSide.Margin = 4;
		var leftContent = new Widget();
		leftContent.MaximumWidth = 300;
		leftContent.Layout = Layout.Column();
		leftContent.Layout.Spacing = 4;
		leftContent.SetStyles( "{ background-color: " + Theme.WidgetBackground.Hex + "; padding: 16px; }" );

		SegmentedControl = Layout.Add( new SegmentedControl() );
		SegmentedControl.Layout.Margin = new Sandbox.UI.Margin( 2, 2 );
		SegmentedControl.AddOption( "Setup Mode", "auto_fix_high" );
		SegmentedControl.AddOption( "Manual Mode", "highlight_alt" );
		SegmentedControl.OnSelectedChanged = ( index ) =>
		{
			UpdatePageContents();
		};
		leftContent.Layout.Add( SegmentedControl );

		ScrollArea = new ScrollArea( this );
		ScrollArea.ContentMargins = 8f;
		ScrollArea.Canvas = new Widget();
		ScrollArea.Canvas.Layout = Layout.Column();
		ScrollArea.Canvas.Layout.Margin = 4f;
		ScrollArea.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		ScrollArea.Canvas.MaximumWidth = 300;

		leftContent.Layout.Add( ScrollArea );

		var leftButtons = new Widget();
		leftButtons.Layout = Layout.Column();
		leftButtons.Layout.Spacing = 4;

		var buttonReset = new Button( "Reset All Settings", "refresh", this );
		buttonReset.Clicked += () =>
		{
			Settings = new ImportSettings();
			HasModified = false;
			Frames.Clear();
			UpdatePageContents();
		};
		leftButtons.Layout.Add( buttonReset );

		var buttonLoad = new Button( "Import Spritesheet", "download", this );
		buttonLoad.Clicked += ImportSpritesheet;
		leftButtons.Layout.Add( buttonLoad );

		leftContent.Layout.Add( leftButtons );

		leftSide.Add( leftContent );
		Layout.Add( leftSide );

		Preview = new Preview( this );
		Layout.Add( Preview );

		UpdatePageContents();
	}

	void ImportSpritesheet ()
	{
		if ( Frames.Count == 0 )
		{
			CommitFrames( Settings.GetFrames() );
		}
		OnImport?.Invoke( Path, GetRectList() );
		EditorCookie.Set<ImportSettings>( "SpritesheetImporterSettings", Settings );
		Close();
	}

	[EditorEvent.Hotload]
	void UpdatePageContents ()
	{
		ScrollArea.Canvas.Layout.Clear( true );
		if ( SegmentedControl.SelectedIndex == 0 )
		{
			var sheet = new ControlSheet();
			var serialized = Settings.GetSerialized();
			sheet.AddObject( serialized );

			ScrollArea.Canvas.Layout.Add( sheet );
			ScrollArea.Canvas.Layout.AddStretchCell();
		}
		else if ( SegmentedControl.SelectedIndex == 1 )
		{
			ScrollArea.Canvas.Layout.Add( new Label( "Click and drag anywhere to create a new frame.\n\nExisting frames can be moved or resized by clicking\nand dragging them." ) );
			ScrollArea.Canvas.Layout.AddStretchCell();
			if ( !HasModified )
			{
				Frames.Clear();
				CommitFrames( Settings.GetFrames() );
			}
		}
	}

	List<Rect> GetRectList ()
	{
		var list = new List<Rect>();
		foreach ( var frame in Frames )
		{
			list.Add( frame.Rect );
		}
		return list;
	}
}