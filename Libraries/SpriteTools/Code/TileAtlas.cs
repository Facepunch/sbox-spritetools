using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools;

/// <summary>
/// A class that re-packs a tileset with 1px borders to avoid bleeding.
/// </summary>
public class TileAtlas
{
	Texture Texture;
	Vector2 OriginalTileSize;
	Vector2Int TileSize;
	Vector2Int TileCounts;
	Dictionary<Vector2Int, Texture> TileCache = new();


	public static Dictionary<TilesetResource, TileAtlas> Cache = new();

	public Vector2 GetTiling ()
	{
		return (Vector2)OriginalTileSize / Texture.Size;
	}

	public Vector2 GetOffset ( Vector2Int cellPosition )
	{
		return new Vector2( cellPosition.x * TileSize.x + 1, cellPosition.y * TileSize.y + 1 ) / Texture.Size;
	}

	public static TileAtlas FromTileset ( TilesetResource tilesetResource )
	{
		if ( tilesetResource is null ) return null;

		if ( Cache?.ContainsKey( tilesetResource ) ?? false )
		{
			return Cache[tilesetResource];
		}

		if ( tilesetResource.Tiles.Count() == 0 )
		{
			return null;
		}

		if ( tilesetResource.Tiles.Any( x => x?.Tileset is null ) )
		{
			return null;
		}

		var path = tilesetResource.FilePath;
		if ( !FileSystem.Mounted.FileExists( path ) )
		{
			Log.Error( $"Tileset texture file {path} does not exist." );
			return null;
		}
		var texture = Texture.LoadFromFileSystem( path, FileSystem.Mounted );
		var atlas = new TileAtlas();

		var tileSize = tilesetResource.TileSize;
		atlas.TileSize = tileSize + Vector2Int.One * 2;
		atlas.OriginalTileSize = tileSize;

		var hTiles = tilesetResource.Tiles.Max( x => x.Position.x + x.Size.x );
		var vTiles = tilesetResource.Tiles.Max( x => x.Position.y + x.Size.y );
		atlas.TileCounts = new Vector2Int( hTiles, vTiles );

		var textureSize = new Vector2Int( hTiles * ( tileSize.x + 2 ), vTiles * ( tileSize.y + 2 ) );

		byte[] textureData = new byte[textureSize.x * textureSize.y * 4];
		for ( int i = 0; i < textureSize.x; i++ )
		{
			for ( int j = 0; j < textureSize.y; j++ )
			{
				var ind = ( j * textureSize.x + i ) * 4;
				textureData[ind] = 0;
				textureData[ind + 1] = 0;
				textureData[ind + 2] = 0;
				textureData[ind + 3] = 0;
			}
		}

		var pixels = texture.GetPixels();

		foreach ( var tile in tilesetResource.Tiles )
		{
			for ( int n = 0; n < tile.Size.x; n++ )
			{
				for ( int m = 0; m < tile.Size.y; m++ )
				{
					var cellPos = tile.Position + new Vector2Int( n, m );

					var tSize = tileSize * tile.Size;
					var tPos = cellPos * atlas.TileSize + Vector2Int.One;
					var sampleX = cellPos.x * tileSize.x;
					var sampleY = cellPos.y * tileSize.y;
					for ( int i = -1; i <= tSize.x; i++ )
					{
						for ( int j = -1; j <= tSize.y; j++ )
						{
							var sampleInd = (int)( ( sampleY + Math.Clamp( j, 0, tSize.y - 1 ) ) * texture.Size.x + sampleX + Math.Clamp( i, 0, tSize.x - 1 ) );
							var color = pixels[sampleInd];
							var ind = ( ( tPos.y + j ) * textureSize.x + tPos.x + i ) * 4;
							if ( ind < 0 || ind >= textureData.Length ) continue;
							textureData[ind + 0] = color.r;
							textureData[ind + 1] = color.g;
							textureData[ind + 2] = color.b;
							textureData[ind + 3] = color.a;
						}
					}


				}
			}

		}

		var builder = Texture.Create( textureSize.x, textureSize.y );
		builder.WithData( textureData );
		builder.WithMips( 0 );
		atlas.Texture = builder.Finish();

		Cache[tilesetResource] = atlas;

		return atlas;
	}

	public Texture GetTextureFromCell ( Vector2Int cellPosition )
	{
		if ( TileCache.ContainsKey( cellPosition ) )
		{
			return TileCache[cellPosition];
		}

		int x = cellPosition.x * TileSize.x + 1;
		int y = cellPosition.y * TileSize.y + 1;
		int outputSizeX = TileSize.x - 2;
		int outputSizeY = TileSize.y - 2;
		byte[] textureData = new byte[outputSizeX * outputSizeY * 4];
		var pixels = Texture.GetPixels();
		for ( int i = 0; i < outputSizeX; i++ )
		{
			for ( int j = 0; j < outputSizeY; j++ )
			{
				int ind = ( i + j * outputSizeX ) * 4;
				int sampleIndex = (int)( x + i + ( y + j ) * Texture.Size.x );
				var color = pixels[sampleIndex];
				textureData[ind + 0] = color.r;
				textureData[ind + 1] = color.g;
				textureData[ind + 2] = color.b;
				textureData[ind + 3] = color.a;
			}
		}

		var builder = Texture.Create( outputSizeX, outputSizeY );
		builder.WithData( textureData );
		builder.WithMips( 0 );
		var texture = builder.Finish();
		TileCache[cellPosition] = texture;
		return texture;
	}

	// Cast to texture
	public static implicit operator Texture ( TileAtlas atlas )
	{
		return atlas?.Texture ?? null;
	}

	public static void ClearCache ( string path = "" )
	{
		if ( path.StartsWith( "/" ) ) path = path.Substring( 1 );
		if ( string.IsNullOrEmpty( path ) )
		{
			Cache.Clear();
		}
		else
		{
			Cache = Cache.Where( x => x.Key.FilePath != path ).ToDictionary( x => x.Key, x => x.Value );
		}
	}

	public static void ClearCache ( TilesetResource tileset )
	{
		Cache.Remove( tileset );
	}
}