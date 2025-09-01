using Editor;
using Sandbox;
using System.Collections.Generic;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to erase tiles from the selected layer.
/// </summary>
[Title( "Erase" )]
[Icon( "delete" )]
[Alias( "tileset-tools.erase-tool" )]
[Group( "2" )]
[Order( 1 )]
public class EraserTileTool : BaseTileTool
{
	public EraserTileTool ( TilesetTool parent ) : base( parent ) { }

	/// <summary>
	/// The size of the Brush when Erasing.
	/// </summary>
	[Group( "Eraser Tool" ), Property, Range( 1, 12 ), Step( 1 )] public int BrushSize { get; set; } = 1;

	/// <summary>
	/// Whether the Brush is round or square.
	/// </summary>
	[Group( "Eraser Tool" ), Property] public bool IsRound { get; set; } = false;

	bool isErasing = false;
	List<Vector2Int> positions;

	public override void OnUpdate ()
	{
		if ( !CanUseTool() ) return;

		var pos = GetGizmoPos();
		Parent._sceneObject.RenderingEnabled = false;

		var tile = TilesetTool.Active.SelectedTile;
		positions = new();
		if ( tile.Size.x > 1 || tile.Size.y > 1 )
		{
			for ( int i = 0; i < tile.Size.x; i++ )
			{
				for ( int j = 0; j < tile.Size.y; j++ )
				{
					positions.Add( new Vector2Int( i, -j ) );
				}
			}
		}
		else if ( IsRound )
		{
			var size = ( BrushSize - 0.9f ) * 2;
			var center = new Vector2Int( (int)( size / 2f ), (int)( size / 2f ) );
			for ( int i = 0; i < size * 2; i++ )
			{
				for ( int j = 0; j < size * 2; j++ )
				{
					var offset = new Vector2Int( i, j ) - center;
					if ( offset.LengthSquared <= ( size / 2 ) * ( size / 2 ) )
					{
						positions.Add( offset );
					}
				}
			}
		}
		else
		{
			Vector2Int startPos = new Vector2Int( -BrushSize / 2, -BrushSize / 2 );
			for ( int i = 0; i < BrushSize; i++ )
			{
				for ( int j = 0; j < BrushSize; j++ )
				{
					positions.Add( new Vector2Int( i, j ) + startPos );
				}
			}
		}

		var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
		var tilePos = ( pos - Parent.SelectedComponent.WorldPosition ) / tileSize;
		if ( Gizmo.IsLeftMouseDown )
		{
			if ( _componentUndoScope is null )
			{
				_componentUndoScope = SceneEditorSession.Active.UndoScope( "Erase Tiles" ).WithComponentChanges( Parent.SelectedComponent ).Push();
			}
			var brush = AutotileBrush;
			foreach ( var ppos in positions )
			{
				if ( brush is null )
				{
					Parent.EraseTile( tilePos + ppos );
				}
				else
				{
					Parent.EraseAutoTile( brush, (Vector2Int)tilePos + ppos, false );
				}
			}

			if ( brush is not null )
			{
				foreach ( var ppos in positions )
				{
					Parent.SelectedLayer.UpdateAutotile( brush.Id, (Vector2Int)tilePos + ppos, true );
				}
			}
			isErasing = true;
		}
		else if ( isErasing )
		{
			_componentUndoScope?.Dispose();
			_componentUndoScope = null;
			isErasing = false;
		}

		using ( Gizmo.Scope( "eraser" ) )
		{
			Gizmo.Draw.Color = Color.Red.WithAlpha( 0.5f );
			foreach ( var ppos in positions )
			{
				var p = ppos * tileSize + tilePos * tileSize;
				var pp = Vector3.Up * 200 + (Vector3)p;
				Gizmo.Draw.SolidBox( new BBox( pp, pp + (Vector3)tileSize ) );
			}
		}
	}

	[Shortcut( "tileset-tools.erase-tool", "e", typeof( SceneViewportWidget ) )]
	public static void ActivateSubTool ()
	{
		if ( EditorToolManager.CurrentModeName != nameof( TilesetTool ) ) return;
		EditorToolManager.SetSubTool( nameof( EraserTileTool ) );
	}
}