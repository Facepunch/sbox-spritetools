using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.SpriteEditor.Inspector;

[CustomEditor( typeof( List<SpriteAttachment> ) )]
public class AttachmentListControlWidget : ControlWidget
{
	MainWindow MainWindow;
	public override bool SupportsMultiEdit => false;

	SerializedCollection Collection;
	int lastCount = 0;

	Layout Content;

	IconButton addButton;

	public AttachmentListControlWidget ( SerializedProperty property, MainWindow window ) : base( property )
	{
		MainWindow = window;
		Layout = Layout.Column();
		Layout.Spacing = 2;

		if ( !property.TryGetAsObject( out var so ) || so is not SerializedCollection sc )
			return;

		Collection = sc;
		Collection.OnEntryAdded = Rebuild;
		Collection.OnEntryRemoved = Rebuild;

		Content = Layout.Column();

		Layout.Add( Content );

		Rebuild();
	}

	[EditorEvent.Frame]
	void OnFrame ()
	{
		if ( Collection.Count() != lastCount )
		{
			Rebuild();
		}
	}

	public void Rebuild ()
	{
		using var _ = SuspendUpdates.For( this );

		Content.Clear( true );
		Content.Margin = 0;

		var grid = Layout.Grid();
		grid.VerticalSpacing = 2;
		grid.HorizontalSpacing = 2;

		int y = 0;
		foreach ( var entry in Collection )
		{
			var attachment = entry.GetValue<SpriteAttachment>();

			var control = Create( entry );
			var index = y;
			//grid.AddCell( 0, y, new IconButton( "drag_handle" ) { IconSize = 13, Foreground = Theme.ControlBackground, Background = Color.Transparent, FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight } );
			grid.AddCell( 1, y, control, 1, 1, control.CellAlignment );
			var visibilityButton = grid.AddCell( 2, y, new IconButton( "visibility" ) { Background = Theme.ControlBackground, FixedWidth = Theme.RowHeight, FixedHeight = Theme.RowHeight } );
			visibilityButton.ToolTip = "Toggle attachment visibility";
			var clearButton = grid.AddCell( 3, y, new IconButton( "clear", () => DeleteAttachmentPopup( index ) ) { Background = Theme.ControlBackground, FixedWidth = Theme.RowHeight, FixedHeight = Theme.RowHeight } );
			clearButton.ToolTip = "Remove attachment";

			visibilityButton.Icon = ( attachment?.Visible ?? true ) ? "visibility" : "visibility_off";
			visibilityButton.OnClick = () =>
			{
				MainWindow.PushUndo( "Toggle {attachment.Name} visibility" );
				attachment.Visible = !attachment.Visible;
				visibilityButton.Icon = attachment.Visible ? "visibility" : "visibility_off";
				MainWindow.PushRedo();
			};

			y++;
		}

		// bottom row
		{
			addButton = grid.AddCell( 1, y, new IconButton( "add" ) { Background = Theme.ControlBackground, ToolTip = "Add attachment", FixedWidth = Theme.RowHeight, FixedHeight = Theme.RowHeight } );
			addButton.MouseClick = AddEntry;
		}

		Content.Add( grid );
		lastCount = Collection.Count();
	}

	void AddEntry ()
	{
		Collection.Add( null );
	}

	void RemoveEntry ( int index )
	{
		Collection.RemoveAt( index );
	}

	protected override void OnPaint ()
	{
		Paint.Antialiasing = true;

		Paint.ClearPen();
		Paint.SetBrush( Theme.TextControl.Darken( 0.6f ) );
	}

	void DeleteAttachmentPopup ( int removeIndex )
	{
		var popup = new PopupWidget( MainWindow );
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add( new Label( $"Are you sure you want to delete this attachment?" ) );

		var button = new Button.Primary( "Delete" );


		button.MouseClick = () =>
		{
			RemoveEntry( removeIndex );
			popup.Visible = false;
		};

		popup.Layout.Add( button );

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add( button );

		popup.Position = Editor.Application.CursorPosition;
		popup.Visible = true;
	}

}
