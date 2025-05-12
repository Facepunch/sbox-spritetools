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

	internal List<SpritesheetImporterFrame> Frames = new List<SpritesheetImporterFrame>();

	ControlSheet ControlSheet { get; set; }

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
		leftSide.Margin = 16;
		var leftContent = new Widget();
		leftContent.MaximumWidth = 300;
		leftContent.Layout = Layout.Column();
		leftContent.Layout.Spacing = 4;
		ControlSheet = new ControlSheet();
		UpdateControlSheet();
		leftContent.Layout.Add( ControlSheet );
		leftContent.Layout.AddStretchCell();

		var buttonCommit = new Button( "Commit Settings", "shortcut", this );
		buttonCommit.Clicked += () =>
		{
			CommitFrames( Settings.GetFrames() );
		};
		leftContent.Layout.Add( buttonCommit );

		var buttonReset = new Button( "Reset All Settings", "refresh", this );
		buttonReset.Clicked += () =>
		{
			Settings = new ImportSettings();
			UpdateControlSheet();
		};
		leftContent.Layout.Add( buttonReset );

		var buttonLoad = new Button( "Import Spritesheet", "download", this );
		buttonLoad.Clicked += ImportSpritesheet;
		leftContent.Layout.Add( buttonLoad );

		leftSide.Add( leftContent );
		Layout.Add( leftSide );

		Preview = new Preview( this );
		Layout.Add( Preview );
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
	void UpdateControlSheet ()
	{
		ControlSheet?.Clear( true );
		ControlSheet.AddObject( Settings.GetSerialized() );
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