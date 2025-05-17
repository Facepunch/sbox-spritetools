using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Reflection;

namespace SpriteTools.TilesetTool;

[Inspector( typeof( TilesetTool ) )]
public class TilesetToolInspector : InspectorWidget
{
	public static TilesetToolInspector Active { get; private set; }
	internal TilesetTool Tool;
	StatusWidget Header;

	ScrollArea scrollArea;
	ControlSheet toolSheet;
	ControlSheet mainSheet;
	ControlSheet selectedSheet;

	public TilesetToolInspector ( SerializedObject so ) : base( so )
	{
		if ( so.Targets.FirstOrDefault() is not TilesetTool tool ) return;

		Tool = tool;
		// Tool.UpdateInspector += UpdateHeader;
		// Tool.UpdateInspector += UpdateSelectedSheet;

		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 8;

		Active = this;
		Rebuild();
	}

	int lastBuildHash = 0;
	[EditorEvent.Frame]
	void Frame ()
	{
		int buildHash = 0;
		if ( Tool.SelectedComponent.IsValid() )
		{
			buildHash += Tool.SelectedComponent.Layers.IndexOf( Tool?.SelectedLayer );
			buildHash += Tool?.SelectedLayer?.TilesetResource?.ResourceId ?? 0;
		}
		if ( buildHash != lastBuildHash )
		{
			lastBuildHash = buildHash;
			Rebuild();
		}
	}

	[EditorEvent.Hotload]
	void Rebuild ()
	{
		if ( Layout is null ) return;
		Layout.Clear( true );

		scrollArea = new ScrollArea( this );
		scrollArea.Canvas = new Widget();
		scrollArea.Canvas.Layout = Layout.Column();
		scrollArea.Canvas.VerticalSizeMode = SizeMode.CanGrow;
		scrollArea.Canvas.HorizontalSizeMode = SizeMode.Flexible;
		scrollArea.Canvas.Layout.Spacing = 8;
		Layout.Add( scrollArea );

		Header = new StatusWidget( this );
		scrollArea.Canvas.Layout.Add( Header );
		UpdateHeader();

		mainSheet = new ControlSheet();
		scrollArea.Canvas.Layout.Add( mainSheet );
		UpdateMainSheet();

		selectedSheet = null;
		UpdateSelectedSheet();

		toolSheet = new ControlSheet();
		scrollArea.Canvas.Layout.Add( toolSheet );
		UpdateToolSheet();

		// Preview = new Preview.Preview(this);
		// scrollArea.Canvas.Layout.Add(Preview);

		scrollArea.Canvas.Layout.AddStretchCell();

	}

	internal void UpdateHeader ()
	{
		Header.Text = "Paint Tiles";
		Header.Color = ( false ) ? Theme.Red : Theme.Blue;
		Header.Icon = ( false ) ? "warning" : "dashboard";
		Header.Update();
	}

	internal void UpdateToolSheet ()
	{
		if ( !( Layout?.IsValid ?? false ) ) return;
		if ( toolSheet is null ) return;

		toolSheet?.Clear( true );

		if ( Tool?.Settings is not null )
		{
			toolSheet.AddObject( Tool.Settings.GetSerialized(), x =>
			{
				return x.HasAttribute<PropertyAttribute>() && x.PropertyType != typeof( Action );
			} );
		}
	}

	internal void UpdateMainSheet ()
	{
		if ( !( Layout?.IsValid ?? false ) ) return;
		if ( mainSheet is null ) return;

		mainSheet?.Clear( true );

		if ( Tool?.CurrentTool is not null )
		{
			var toolName = ( Tool.CurrentTool.GetType()?.GetCustomAttribute<TitleAttribute>()?.Value ?? "Unknown" ) + " Tool";
			mainSheet.AddObject( Tool.CurrentTool.GetSerialized(), x => x.HasAttribute<PropertyAttribute>() && x.PropertyType != typeof( Action ) );
		}
		if ( Tool.SelectedComponent.IsValid() )
		{
			mainSheet.AddObject( Tool.SelectedComponent.GetSerialized(), x =>
			{
				if ( x.Name == nameof( TilesetComponent.Layers ) ) return true;
				if ( !x.HasAttribute<PropertyAttribute>() ) return false;
				if ( x.TryGetAttribute<FeatureAttribute>( out var feature ) && feature.Title == "Collision" ) return false;
				if ( x.PropertyType == typeof( Action ) ) return false;
				if ( x.PropertyType == typeof( TilesetComponent.ComponentControls ) ) return false;

				return true;
			} );
		}
	}

	internal void UpdateSelectedSheet ()
	{
		if ( !( Layout?.IsValid ?? false ) ) return;

		if ( selectedSheet is null || !( selectedSheet?.IsValid ?? false ) )
		{
			selectedSheet = new ControlSheet();
			scrollArea.Canvas.Layout.Add( selectedSheet );
		}

		selectedSheet?.Clear( true );
		if ( Tool.SelectedLayer is not null )
		{
			selectedSheet.AddObject( Tool.SelectedLayer.GetSerialized(), x => x.HasAttribute<PropertyAttribute>() && x.PropertyType != typeof( Action ) );
		}
	}

	private class StatusWidget : Widget
	{
		public string Icon { get; set; }
		public string Text { get; set; }
		public string LeadText { get; set; }
		public Color Color { get; set; }

		TilesetToolInspector Inspector;

		public StatusWidget ( TilesetToolInspector parent ) : base( parent )
		{
			Inspector = parent;
			MinimumSize = 48;
			Cursor = CursorShape.Finger;
			SetSizeMode( SizeMode.Default, SizeMode.CanShrink );
		}

		protected override void OnPaint ()
		{
			var rect = new Rect( 0, Size );

			Paint.ClearPen();
			Paint.SetBrush( Theme.WindowBackground.Lighten( 0.9f ) );
			Paint.DrawRect( rect );

			rect.Left += 8;

			Paint.SetPen( Color );
			var iconRect = Paint.DrawIcon( rect, Icon, 24, TextFlag.LeftCenter );

			rect.Top += 8;
			rect.Left = iconRect.Right + 8;

			Paint.SetPen( Color );
			Paint.SetDefaultFont( 10, 500 );
			var titleRect = Paint.DrawText( rect, Text, TextFlag.LeftTop );

			rect.Top = titleRect.Bottom + 2;

			Paint.SetPen( Color.WithAlpha( 0.6f ) );
			Paint.SetDefaultFont( 8, 400 );
			var preText = "Selected Component:";
			if ( !Inspector.Tool.SelectedComponent.IsValid() )
				preText = "No Tileset Component";
			var selectedRect = Paint.DrawText( rect, preText, TextFlag.LeftTop );
			if ( Inspector.Tool.SelectedComponent.IsValid() )
			{
				var name = Inspector.Tool.SelectedComponent.GameObject.Name;
				var textPos = selectedRect.TopRight + new Vector2( 8, 0 );
				var textRect = new Rect( textPos, Paint.MeasureText( name ) );
				var boxRect = textRect.Grow( 4, 2, 18, 2 );
				var isHovering = Paint.HasMouseOver;
				var boxCol = isHovering ? Theme.ControlBackground.Lighten( 0.3f ) : Theme.ControlBackground.Darken( 0.2f );
				var color = isHovering ? Color.Lighten( 0.2f ) : Color;
				Paint.SetBrushAndPen( boxCol, Color.Transparent );
				Paint.DrawRect( boxRect );
				Paint.SetPen( color );
				var drawnRect = Paint.DrawText( textPos, name );
				var iconPos = drawnRect.TopRight + new Vector2( 2, 0 );
				Paint.DrawIcon( Rect.FromPoints( iconPos, iconPos + 14 ), "expand_more", 14 );

			}
		}

		protected override void OnMouseClick ( MouseEvent e )
		{
			base.OnMouseClick( e );

			var components = SceneEditorSession.Active.Scene.GetAllComponents<TilesetComponent>();
			Log.Info( components.Count() );
			if ( components.Count() == 0 ) return;

			var menu = new Menu();

			foreach ( var tileset in components )
			{
				var option = menu.AddOption( tileset.GameObject.Name, null, () =>
				{
					Inspector.Tool.SelectedComponent = tileset;
					Inspector.Tool.SelectedLayer = tileset.Layers.FirstOrDefault();
				} );
				option.Checkable = true;
				option.Checked = tileset == Inspector.Tool.SelectedComponent;
			}

			menu.OpenAtCursor();
		}
	}
}