using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

public class AutotileTileControl : Widget
{
	internal AutotileBrushControl ParentBrush;
	internal AutotileBrush.Tile Tile;

	int Index;

	public AutotileTileControl ( AutotileBrushControl brush, int index )
	{
		Index = index;
		ParentBrush = brush;
		Tile = ParentBrush.Brush.Tiles[index];

		VerticalSizeMode = SizeMode.Flexible;

		StatusTip = $"Select Tile {index}";
		Cursor = CursorShape.Finger;

		Layout = Layout.Row();
		Layout.AddSpacingCell( 20 );
		Layout.Margin = 4;
		Layout.Spacing = 4;

		FixedSize = 26;

		IsDraggable = true;
		AcceptDrops = true;
	}

	protected override void OnPaint ()
	{
		Texture tex = null;
		if ( ( Tile?.Tiles?.Count ?? 0 ) > 0 )
		{
			var tileRef = Tile?.Tiles?.FirstOrDefault();
			var tile = tileRef?.Tileset?.Tiles?.FirstOrDefault( x => x?.Id == tileRef?.Id );
			if ( tile is not null )
			{
				var atlas = TileAtlas.FromTileset( tile.Tileset );
				if ( atlas is not null )
				{
					tex = atlas.GetTextureFromCell( tile.Position );
				}
			}
		}
		if ( tex is null )
		{
			string tileType = "2x2e";
			switch ( ParentBrush.Brush.AutotileType )
			{
				case AutotileType.Bitmask2x2Edge: tileType = "2x2e"; break;
				// case AutotileType.Bitmask2x2Corner: tileType = "2x2c"; break;
				case AutotileType.Bitmask3x3: tileType = "3x3m"; break;
				case AutotileType.Bitmask3x3Complete: tileType = "3x3c"; break;
			}
			tex = GetGuide( tileType, Index );
		}
		if ( tex is not null )
		{
			var pixmap = Pixmap.FromTexture( tex );
			Paint.Draw( LocalRect, pixmap );
		}

		var color = ( IsUnderMouse && Enabled ) ? Color.White : Color.Transparent;
		if ( ParentBrush.ParentList.SelectedTile == Tile )
		{
			color = Theme.Blue;
		}
		Paint.SetBrushAndPen( color.WithAlpha( MathF.Min( 0.5f, color.a ) ), Color.Transparent );
		Paint.DrawRect( LocalRect );

		if ( !Enabled )
		{
			Paint.SetBrushAndPen( Theme.WindowBackground.WithAlpha( 0.5f ) );
			Paint.DrawRect( LocalRect );
		}
	}

	protected override void OnMouseClick ( MouseEvent e )
	{
		base.OnMouseClick( e );

		ParentBrush?.ParentList?.SelectTile( this );

		e.Accepted = true;
	}

	protected override void OnContextMenu ( ContextMenuEvent e )
	{
		base.OnContextMenu( e );

		var m = new Menu( this );

		m.AddOption( "Clear", "clear", Clear );

		m.OpenAtCursor( false );

		e.Accepted = true;
	}

	void Clear ()
	{
		Tile.Tiles?.Clear();
	}

	static Texture GetGuide ( string tileType, int index )
	{
		var imagePath = $"images/guides/tile-guide-{tileType}.png";
		List<Rect> rects = new();
		switch ( tileType )
		{
			case "2x2e":
				{
					for ( int i = 0; i < 16; i++ )
					{
						int x = i % 8;
						int y = i / 8;
						rects.Add( new Rect( x * 8, y * 8, 8, 8 ) );
					}
					break;
				}
			case "3x3m":
				{
					for ( int i = 0; i < 47; i++ )
					{
						var x = i % 7;
						var y = i / 7;
						rects.Add( new Rect( x * 8, y * 8, 8, 8 ) );
					}
					break;
				}
			case "3x3c":
				{
					for ( int i = 0; i < 256; i++ )
					{
						var x = i % 16;
						var y = i / 16;
						rects.Add( new Rect( x * 3, y * 3, 3, 3 ) );
					}
					break;
				}
		}
		if ( rects.Count == 0 ) return Texture.White;

		var atlas = TextureAtlas.FromSpritesheet( imagePath, rects );
		return atlas.GetTextureFromFrame( index );
	}
}