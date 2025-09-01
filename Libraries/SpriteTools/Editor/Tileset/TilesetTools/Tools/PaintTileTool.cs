using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to paint tiles on the selected layer.
/// </summary>
[Title( "Paint" )]
[Icon( "brush" )]
[Alias( "tileset-tools.paint-tool" )]
[Group( "1" )]
[Order( 0 )]
public class PaintTileTool : BaseTileTool
{
	public PaintTileTool ( TilesetTool parent ) : base( parent ) { }

	/// <summary>
	/// The size of the Brush when Painting.
	/// </summary>
	[Group( "Paint Tool" ), Property, Range( 1, 12 ), Step( 1 )]
	public int BrushSize
	{
		get => _brushSize;
		set
		{
			_brushSize = value;
			lastTilePos = -999999;
		}
	}
	private int _brushSize = 1;

	/// <summary>
	/// Whether the Brush is round or square.
	/// </summary>
	[Group( "Paint Tool" ), Property]
	public bool IsRound
	{
		get => _isRound;
		set
		{
			_isRound = value;
			lastTilePos = -999999;
		}
	}
	private bool _isRound = false;

	/// <summary>
	/// If enabled, Autotiles of different types will attempt to connect with each other.
	/// </summary>
	[Group( "Paint Tool" ), Property, ShowIf( nameof( this.CanSeeAutotileSettings ), true )]
	public bool MergeDifferentAutotiles
	{
		get => ShouldMergeAutotiles;
		set
		{
			ShouldMergeAutotiles = value;
		}
	}
	private bool CanSeeAutotileSettings => AutotileWidget.Instance?.Brush is not null;

	Vector2Int lastTilePos;
	bool isPainting = false;

	public override void OnUpdate ()
	{
		if ( !CanUseTool() ) return;
		if ( Parent.SelectedComponent.Transform is null ) return;

		var pos = GetGizmoPos();
		var tile = Parent.SelectedTile;
		var tilePos = (Vector2Int)( ( pos - Parent.SelectedComponent.WorldPosition ) / Parent.SelectedLayer.TilesetResource.GetTileSize() );

		Parent._sceneObject.Transform = new Transform( pos, Rotation.Identity, 1 );
		Parent._sceneObject.RenderingEnabled = true;

		if ( tilePos != lastTilePos )
		{
			UpdateTilePositions();
		}

		if ( Gizmo.IsLeftMouseDown )
		{
			if ( _componentUndoScope is null )
			{
				_componentUndoScope = SceneEditorSession.Active.UndoScope( "Paint Tile" ).WithComponentChanges( Parent.SelectedComponent ).Push();
			}
			var brush = AutotileBrush;
			if ( brush is not null )
			{
				Place( tilePos );
			}
			else if ( tile.Size.x > 1 || tile.Size.y > 1 )
			{
				for ( int x = 0; x < tile.Size.x; x++ )
				{
					var ux = x;
					var xx = x;
					if ( Parent.Settings.HorizontalFlip ) ux = tile.Size.x - x - 1;
					for ( int y = 0; y < tile.Size.y; y++ )
					{
						var uy = y;
						var yy = -y;
						var offsetPos = new Vector2Int( xx, yy );

						if ( Parent.Settings.Angle == 90 )
							offsetPos = new Vector2Int( -offsetPos.y, offsetPos.x );
						else if ( Parent.Settings.Angle == 180 )
							offsetPos = new Vector2Int( -offsetPos.x, -offsetPos.y );
						else if ( Parent.Settings.Angle == 270 )
							offsetPos = new Vector2Int( offsetPos.y, -offsetPos.x );

						Parent.PlaceTile( tilePos + offsetPos, tile.Id, new Vector2Int( ux, uy ), false );
					}
				}
				Parent.SelectedComponent.IsDirty = true;
			}
			else
			{
				Place( tilePos );
			}
			isPainting = true;
		}
		else if ( isPainting )
		{
			_componentUndoScope?.Dispose();
			_componentUndoScope = null;
			isPainting = false;
		}

		// if (Parent?.SelectedLayer?.AutoTilePositions is not null)
		// {
		//     var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
		//     using (Gizmo.Scope("test", Transform.Zero))
		//     {
		//         Gizmo.Draw.Color = Color.Red.WithAlpha(0.1f);
		//         foreach (var group in Parent.SelectedLayer.AutoTilePositions)
		//         {
		//             var brush = group.Key;
		//             foreach (var position in group.Value)
		//             {
		//                 Gizmo.Draw.WorldText(Parent.SelectedLayer.GetAutotileBitmask(brush, position).ToString(),
		//                     new Transform(
		//                         Parent.SelectedComponent.WorldPosition + (Vector3)((Vector2)position * tileSize) + (Vector3)(tileSize * 0.5f) + Vector3.Up * 200,
		//                         Rotation.Identity,
		//                         0.3f
		//                     ),
		//                     "Poppins", 24
		//                 );
		//             }
		//         }
		//     }
		// }
	}

	void UpdateTilePositions ()
	{
		var pos = GetGizmoPos();
		var brush = AutotileBrush;
		var tile = Parent.SelectedTile;
		if ( tile is null ) return;
		var tilePos = (Vector2Int)( ( pos - Parent.SelectedComponent.WorldPosition ) / Parent.SelectedLayer.TilesetResource.GetTileSize() );

		List<(Vector2Int, Vector2Int)> positions = new();
		if ( brush is null && ( tile.Size.x > 1 || tile.Size.y > 1 ) )
		{
			for ( int i = 0; i < tile.Size.x; i++ )
			{
				for ( int j = 0; j < tile.Size.y; j++ )
				{
					positions.Add( (new Vector2Int( i, -j ), tile.Position + new Vector2Int( i, j )) );
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
						positions.Add( (offset, tile.Position) );
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
					positions.Add( (new Vector2Int( i, j ) + startPos, tile.Position) );
				}
			}
		}

		// Set autobrush tiles if necessary
		if ( brush is not null )
		{
			if ( brush.AutotileType == AutotileType.Bitmask2x2Edge )
			{
				List<Vector2Int> tilesToAdd = new();
				foreach ( var ppos in positions )
				{
					bool touchingX = false;
					bool touchingY = false;
					var up = ppos.Item1.WithY( ppos.Item1.y + 1 );
					var down = ppos.Item1.WithY( ppos.Item1.y - 1 );
					var left = ppos.Item1.WithX( ppos.Item1.x - 1 );
					var right = ppos.Item1.WithX( ppos.Item1.x + 1 );
					foreach ( var ppos2 in positions )
					{
						if ( !touchingX && ( ppos2.Item1 == left || ppos2.Item1 == right ) )
						{
							touchingX = true;
						}
						if ( !touchingY && ( ppos2.Item1 == up || ppos2.Item1 == down ) )
						{
							touchingY = true;
						}
						if ( touchingX && touchingY ) break;
					}
					if ( touchingX && touchingY ) continue;

					var upLeft = up.WithX( left.x );
					var upRight = up.WithX( right.x );
					var downLeft = down.WithX( left.x );
					var downRight = down.WithX( right.x );
					if ( !tilesToAdd.Contains( up ) ) tilesToAdd.Add( up );
					if ( !tilesToAdd.Contains( down ) ) tilesToAdd.Add( down );
					if ( !tilesToAdd.Contains( left ) ) tilesToAdd.Add( left );
					if ( !tilesToAdd.Contains( right ) ) tilesToAdd.Add( right );
					if ( !tilesToAdd.Contains( upLeft ) ) tilesToAdd.Add( upLeft );
					if ( !tilesToAdd.Contains( upRight ) ) tilesToAdd.Add( upRight );
					if ( !tilesToAdd.Contains( downLeft ) ) tilesToAdd.Add( downLeft );
					if ( !tilesToAdd.Contains( downRight ) ) tilesToAdd.Add( downRight );
				}
				foreach ( var toAddPos in tilesToAdd )
				{
					if ( !positions.Contains( (toAddPos, tile.Position) ) )
						positions.Add( (toAddPos, tile.Position) );
				}
			}
		}

		UpdateTilePositions( positions.Select( x => (Vector2)x.Item1 ).ToList() );
		lastTilePos = tilePos;
	}

	void Place ( Vector2Int tilePos )
	{
		var brush = AutotileBrush;
		var tile = Parent.SelectedTile;


		foreach ( var position in Parent._sceneObject.MultiTilePositions )
		{
			if ( brush is null )
			{
				Parent.PlaceTile( tilePos + position.Item1, tile.Id, 0 );
			}
			else
			{
				Parent.PlaceAutotile( ( position.Item3 == Guid.Empty ) ? brush.Id : position.Item3, tilePos + position.Item1, false );
			}
		}

		if ( brush is not null )
		{
			foreach ( var position in Parent._sceneObject.MultiTilePositions )
			{
				var brushId = ( position.Item3 == Guid.Empty ) ? brush.Id : position.Item3;
				Parent.SelectedLayer.UpdateAutotile( brushId, tilePos + position.Item1, false, shouldMerge: MergeDifferentAutotiles );
			}
		}

		return;
	}

	[Shortcut( "tileset-tools.paint-tool", "b", typeof( SceneViewportWidget ) )]
	public static void ActivateSubTool ()
	{
		if ( EditorToolManager.CurrentModeName != nameof( TilesetTool ) ) return;
		EditorToolManager.SetSubTool( nameof( PaintTileTool ) );
	}
}