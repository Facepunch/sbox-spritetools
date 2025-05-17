using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

namespace SpriteTools.TilesetEditor;

public class AutotileBrushControl : Widget
{
	internal AutotileBrushListControl ParentList;
	internal AutotileBrush Brush;

	Drag dragData;
	bool draggingAbove = false;
	bool draggingBelow = false;

	Layout TileContent;
	List<AutotileTileControl> TileControls = new();

	public AutotileBrushControl ( AutotileBrushListControl list, AutotileBrush brush )
	{
		ParentList = list;
		Brush = brush;

		MouseClick = () =>
		{
			ParentList.SelectBrush( this );
		};

		VerticalSizeMode = SizeMode.Flexible;

		StatusTip = $"Select Brush \"{brush.Name}\"";
		Cursor = CursorShape.Finger;

		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		TileContent = Layout.Add( Layout.Row() );

		CreateTileControls();

		IsDraggable = true;
		AcceptDrops = true;
	}

	public override void OnDestroyed ()
	{
		base.OnDestroyed();

		if ( ParentList.SelectedBrush == this )
		{
			ParentList.SelectedBrush = null;
			ParentList.SelectedTile = null;
		}
	}

	void CreateTileControls ()
	{
		if ( TileContent is null ) return;
		TileContent.Clear( true );
		TileControls.Clear();

		var tileCount = Brush.TileCount;
		for ( int i = 0; i < tileCount; i++ )
		{
			var tileControl = new AutotileTileControl( this, i );
			TileContent.Add( tileControl );
			TileControls.Add( tileControl );
		}
	}

	[EditorEvent.Frame]
	void Frame ()
	{
		var tileCount = Brush?.TileCount ?? 0;
		if ( tileCount > 0 && tileCount != TileControls.Count )
		{
			CreateTileControls();
		}
	}

	protected override void OnPaint ()
	{
		MaximumWidth = ParentList.Width - 12;
		if ( Enabled )
		{
			if ( dragData?.IsValid ?? false )
			{
				Paint.SetBrushAndPen( Theme.WindowBackground.WithAlpha( 0.5f ) );
				Paint.DrawRect( LocalRect, 4 );
			}
			else if ( ParentList.SelectedBrush == this )
			{
				Paint.SetBrushAndPen( Theme.Highlight.Darken( 0.5f ) );
				Paint.DrawRect( LocalRect, 4 );
			}
			else if ( IsUnderMouse )
			{
				Paint.SetBrushAndPen( Theme.Text.WithAlpha( 0.1f ) );
				Paint.DrawRect( LocalRect, 4 );
			}
		}
		else
		{
			Paint.SetBrushAndPen( Theme.WindowBackground.WithAlpha( 0.5f ) );
			Paint.DrawRect( LocalRect );
		}

		var brushName = string.IsNullOrEmpty( Brush.Name ) ? $"Brush {ParentList.Buttons.IndexOf( this ) + 1}" : Brush.Name;
		Paint.SetBrushAndPen( Color.Transparent );
		var textRect = LocalRect.Shrink( 6, 4 );
		textRect.Top += 2f;
		Paint.DrawTextBox( textRect, brushName, Theme.TextControl, 8, 4, TextFlag.LeftTop );

		var tileCount = Brush.TileCount;
		Paint.SetBrushAndPen( Theme.TextLight );
		var size = 26f;
		var padding = 3;
		var tileWidth = MathF.Floor( Width / ( size + padding ) );
		for ( int i = 0; i < tileCount; i++ )
		{
			var x = i % tileWidth;
			var y = MathF.Floor( i / tileWidth );
			var tileRect = new Rect( LocalRect.TopLeft + new Vector2( 4, 26 ) + new Vector2( x * ( size + padding ), y * ( size + padding ) ), new Vector2( size, size ) );
			if ( TileControls.ElementAt( i ) is AutotileTileControl tileControl )
			{
				tileControl.Position = tileRect.Position;
			}
		}
		var maxY = 1 + MathF.Floor( tileCount / tileWidth );
		FixedHeight = 28 + ( size + padding ) * maxY;

		if ( draggingAbove )
		{
			Paint.SetPen( Theme.Highlight, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.TopLeft, LocalRect.TopRight );
			draggingAbove = false;
		}
		else if ( draggingBelow )
		{
			Paint.SetPen( Theme.Highlight, 2f, PenStyle.Dot );
			Paint.DrawLine( LocalRect.BottomLeft, LocalRect.BottomRight );
			draggingBelow = false;
		}
	}

	void Rename ()
	{
		var brush = Brush;
		OpenLineEditFlyout( Brush.Name, "What do you want to rename this Brush to?", null,
		name =>
		{
			if ( string.IsNullOrEmpty( name ) ) return;
			ParentList?.MainWindow?.PushUndo( "Rename Brush" );
			brush.Name = name;
			ParentList?.MainWindow?.SetDirty();
			ParentList?.MainWindow?.PushRedo();
		} );
	}

	void Delete ()
	{
		ParentList.DeleteBrush( Brush );
	}

	protected override void OnContextMenu ( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var m = new Menu( this );

		m.AddOption( "Clear All Tiles", "clear", () =>
		{
			foreach ( var tile in Brush.Tiles )
			{
				tile.Tiles?.Clear();
			}
			ParentList?.MainWindow?.inspector?.UpdateSelectedAutotileSheet();
			ParentList?.MainWindow?.SetDirty();
		} );

		m.AddSeparator();

		m.AddOption( "Rename", "edit", Rename );
		m.AddOption( "Delete", "delete", Delete );

		m.OpenAtCursor( false );
	}

	protected override void OnDragStart ()
	{
		base.OnDragStart();

		dragData = new Drag( this );
		dragData.Data.Object = Brush;
		dragData.Execute();
	}

	public override void OnDragHover ( DragEvent ev )
	{
		base.OnDragHover( ev );

		if ( !TryDragOperation( ev, out var dragDelta ) )
		{
			draggingAbove = false;
			draggingBelow = false;
			return;
		}

		draggingAbove = dragDelta > 0;
		draggingBelow = dragDelta < 0;
	}

	public override void OnDragDrop ( DragEvent ev )
	{
		base.OnDragDrop( ev );

		if ( !TryDragOperation( ev, out var delta ) ) return;

		var list = ParentList.SerializedProperty.GetValue<List<AutotileBrush>>();
		var index = list.IndexOf( Brush );
		var movingIndex = index + delta;
		var layer = list[movingIndex];

		list.RemoveAt( movingIndex );
		list.Insert( index, layer );

		ParentList.SerializedProperty.SetValue( list );
		ParentList.UpdateList();
	}

	bool TryDragOperation ( DragEvent ev, out int delta )
	{
		delta = 0;
		var brush = ev.Data.OfType<AutotileBrush>().FirstOrDefault();

		if ( brush == null || ParentList == null ) return false;

		var layerList = ParentList.SerializedProperty.GetValue<List<AutotileBrush>>();
		var myIndex = layerList.IndexOf( Brush );
		var otherIndex = layerList.IndexOf( brush );
		if ( myIndex == -1 || otherIndex == -1 ) return false;

		delta = otherIndex - myIndex;
		return true;
	}

	private static void OpenLineEditFlyout ( string value, string message, Vector2? position, Action<string> onSubmit )
	{
		LineEdit entry = null;

		OpenFlyout( message, position,
			( popup, button ) =>
			{
				entry = new LineEdit( popup ) { Text = value };
				entry.ReturnPressed += () => button.MouseClick?.Invoke();

				popup.Layout.Add( entry );
				entry.Focus();
			},
			() =>
			{
				try
				{
					onSubmit( entry.Value );
				}
				catch ( Exception ex )
				{
					OpenErrorFlyout( ex, position );
				}
			} );
	}

	private static void OpenFlyout ( string message, Vector2? position, Action<PopupWidget, Button> onLayout = null, Action onSubmit = null )
	{
		var popup = new PopupWidget( null );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( message ) );

		var button = new Button.Primary( "Confirm" );

		button.MouseClick += () =>
		{
			onSubmit?.Invoke();
			popup.Close();
		};

		onLayout?.Invoke( popup, button );

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );
		popup.Position = position ?? Editor.Application.CursorPosition;
		popup.Visible = true;
	}

	private static void OpenErrorFlyout ( string title, string message, Vector2? position )
	{
		OpenFlyout( $"<h3>{title}</h3><p>{message}</p>", position );
	}

	private static void OpenErrorFlyout ( Exception ex, Vector2? position )
	{
		OpenErrorFlyout( "Error", SecurityElement.Escape( ex.Message ), position );
	}
}