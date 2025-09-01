using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools;

[AssetType( Name = "2D Tileset", Extension = "tileset", Category = "SpriteTools" )]
public partial class TilesetResource : GameResource
{
	/// <summary>
	/// The file path to the image referenced by the tileset.
	/// </summary>
	[Property, ImageAssetPath, Title( "Tileset Image" ), Group( "Tileset Setup" )]
	public string FilePath { get; set; }

	/// <summary>
	/// The size of each tile in the tileset (in pixels).
	/// </summary>

	[Property, Group( "Tileset Setup" )]
	public Vector2Int TileSize { get; set; } = new Vector2Int( 32, 32 );

	/// <summary>
	/// The separation between each tile in the tileset (in pixels).
	/// </summary>
	[Property, Group( "Tileset Setup" )]
	public Vector2Int TileSeparation { get; set; } = 0;

	/// <summary>
	/// How much the tileset should be scaled when placed in the Scene.
	/// </summary>

	[Property, Group( "Additional Settings" )]
	public float TileScale { get; set; } = 1.0f;

	/// <summary>
	/// The list of all tiles in the tileset.
	/// </summary>
	[Property, Group( "Tiles" )]
	public List<Tile> Tiles { get; set; } = new();

	/// <summary>
	/// The tileset to inherit autotile settings from. This is useful if you have multiple
	/// tilesets that are laid out in the exact same way.
	/// </summary>
	[Property, Group( "Autotile Settings" )]
	public TilesetResource InheritAutotileFrom { get; set; }

	/// <summary>
	/// A list of the autotile brushes for this tileset.
	/// </summary>
	[Property, Group( "Autotile Brushes" ), Order( 9999 )]
	public List<AutotileBrush> AutotileBrushes { get; set; } = new();

	[JsonIgnore, Hide]
	internal Dictionary<Guid, Tile> TileMap { get; set; } = new();

	/// <summary>
	/// The size of the referenced texture in pixels (as it was when the tiles were first generated).
	/// </summary>
	[Hide] public Vector2Int CurrentTextureSize { get; set; } = Vector2Int.One;

	/// <summary>
	/// The size of each tile in pixels (as it was when the tiles were first generated)
	/// </summary>
	[Hide] public Vector2Int CurrentTileSize { get; set; } = new Vector2Int( 32, 32 );

	/// <summary>
	/// Returns the UV tiling scale for the tileset.
	/// </summary>
	/// <returns></returns>
	public Vector2 GetTiling ()
	{
		return (Vector2)CurrentTileSize / CurrentTextureSize;
	}

	/// <summary>
	/// Returns the UV offset for the given cell position.
	/// </summary>
	/// <param name="cellPosition"></param>
	/// <returns></returns>
	public Vector2 GetOffset ( Vector2Int cellPosition )
	{
		return new Vector2( cellPosition.x * CurrentTileSize.x, cellPosition.y * CurrentTileSize.y ) / CurrentTextureSize;
	}

	/// <summary>
	/// Returns the size of each tile in world units.
	/// </summary>
	/// <returns></returns>
	public Vector2 GetTileSize ()
	{
		return TileSize * TileScale;
	}

	/// <summary>
	/// Returns the size of each tile in world units from when it was first generated.
	/// </summary>
	/// <returns></returns>
	public Vector2 GetCurrentTileSize ()
	{
		return CurrentTileSize * TileScale;
	}

	/// <summary>
	/// Add a tile to the tileset.
	/// </summary>
	/// <param name="tile"></param>
	public void AddTile ( Tile tile )
	{
		Tiles.Add( tile );
		TileMap[tile.Id] = tile;
		tile.Tileset = this;
	}

	/// <summary>
	/// Remove a tile from the tileset
	/// </summary>
	/// <param name="tile"></param>
	public void RemoveTile ( Tile tile )
	{
		TileMap.Remove( tile.Id );
		Tiles.Remove( tile );
	}

	/// <summary>
	/// Get a tile from its ID.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public Tile GetTileFromId ( Guid id )
	{
		if ( id == Guid.Empty ) return null;
		if ( TileMap.ContainsKey( id ) )
		{
			return TileMap[id];
		}
		return null;
	}

	/// <summary>
	/// Returns a list of all autotile brushes for this tileset (including those inherited from parent tilesets).
	/// </summary>
	/// <returns></returns>
	public List<AutotileBrush> GetAllAutotileBrushes ()
	{
		var allBrushes = new List<AutotileBrush>();

		foreach ( var brush in AutotileBrushes )
		{
			brush.Tileset = this;
			allBrushes.Add( brush );
		}

		if ( InheritAutotileFrom is not null )
		{
			foreach ( var inheritedBrush in InheritAutotileFrom.GetAllAutotileBrushes() )
			{
				if ( !allBrushes.Any( x => x.GetHashCode() == inheritedBrush.GetHashCode() ) )
				{
					var newBrush = new AutotileBrush();
					newBrush.Name = inheritedBrush.Name;
					newBrush.AutotileType = inheritedBrush.AutotileType;
					var tileList = new List<AutotileBrush.Tile>();
					foreach ( var tile in inheritedBrush.Tiles )
					{
						var newTileRefs = new List<AutotileBrush.TileReference>();
						foreach ( var tileRef in tile.Tiles )
						{
							var newTileRef = new AutotileBrush.TileReference();
							newTileRef.Position = tileRef.Position;
							newTileRef.Weight = tileRef.Weight;
							newTileRef.Tileset = this;
							var pos = newTileRef.GetTilePosition();
							foreach ( var tilesetTile in Tiles )
							{
								if ( tilesetTile.Position == pos )
								{
									newTileRef.Id = tilesetTile.Id;
									break;
								}
							}
							if ( newTileRef.Id != Guid.Empty )
							{
								newTileRefs.Add( newTileRef );
							}
						}
						var newTile = new AutotileBrush.Tile();
						newTile.Tiles = newTileRefs;
						tileList.Add( newTile );
					}
					newBrush.Tiles = tileList.ToArray();
					newBrush.Tileset = this;
					allBrushes.Add( newBrush );
				}
			}
		}

		return allBrushes;
	}

	public string SerializeString ()
	{
		var obj = new JsonObject()
		{
			["FilePath"] = FilePath,
			["TileScale"] = TileScale.ToString(),
			["TileSize"] = TileSize.ToString(),
			["TileSeparation"] = TileSeparation.ToString(),
			["Tiles"] = JsonArray.Parse( Json.Serialize( Tiles ) ),
			["AutotileBrushes"] = JsonArray.Parse( Json.Serialize( AutotileBrushes ) ),
			["InheritAutotileFrom"] = InheritAutotileFrom?.ResourceId.ToString() ?? "",
			["CurrentTextureSize"] = CurrentTextureSize.ToString(),
			["CurrentTileSize"] = CurrentTileSize.ToString()
		};
		return obj.ToJsonString();
	}

	public void DeserializeString ( string json )
	{
		var obj = JsonNode.Parse( json );
		FilePath = obj["FilePath"]?.GetValue<string>() ?? "";
		TileScale = float.Parse( obj["TileScale"]?.GetValue<string>() ?? "1.0" );
		TileSize = Vector2Int.Parse( obj["TileSize"]?.GetValue<string>() ?? "32,32" );
		CurrentTileSize = Vector2Int.Parse( obj["CurrentTileSize"]?.GetValue<string>() ?? "32,32" );
		CurrentTextureSize = Vector2Int.Parse( obj["CurrentTextureSize"]?.GetValue<string>() ?? "1,1" );
		TileSeparation = Vector2Int.Parse( obj["TileSeparation"]?.GetValue<string>() ?? "0,0" );
		var tiles = obj["Tiles"].AsArray();
		Tiles.Clear();
		foreach ( var tile in tiles )
		{
			Tiles.Add( Json.Deserialize<Tile>( tile.ToJsonString() ) );
		}
		var brushes = obj["AutotileBrushes"].AsArray();
		AutotileBrushes.Clear();
		foreach ( var brush in brushes )
		{
			AutotileBrushes.Add( Json.Deserialize<AutotileBrush>( brush.ToJsonString() ) );
		}
		var inheritId = obj["InheritAutotileFrom"]?.GetValue<string>() ?? "";
		InheritAutotileFrom = ResourceLibrary.GetAll<TilesetResource>().FirstOrDefault( x => x.ResourceId.ToString() == inheritId );
		InternalUpdateTiles();
	}

	protected override void PostLoad ()
	{
		base.PostLoad();

		InternalReload();
	}

	protected override void PostReload ()
	{
		base.PostReload();

		InternalReload();
	}

	void InternalReload ()
	{
		var realTiles = new List<Tile>();
		foreach ( var tile in Tiles )
		{
			if ( tile is null ) continue;
			realTiles.Add( tile );
		}
		Tiles = realTiles;

		var realBrushes = new List<AutotileBrush>();
		foreach ( var brush in AutotileBrushes )
		{
			if ( brush is null ) continue;
			realBrushes.Add( brush );
		}
		AutotileBrushes = realBrushes;

		foreach ( var brush in AutotileBrushes )
		{
			brush.Tileset = this;
		}

		InternalUpdateTiles();
	}

	internal void InternalUpdateTiles ()
	{
		foreach ( var tile in Tiles )
		{
			TileMap[tile.Id] = tile;
			tile.Tileset = this;
		}
	}

	protected override Bitmap CreateAssetTypeIcon ( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "calendar_view_month", width, height, "#fab006", "#1a2c17" );
	}

	public class TileTextureData
	{
		public Vector2Int Size { get; set; }
		public byte[] Data { get; set; }

		public TileTextureData ( Vector2Int size, byte[] data )
		{
			Size = size;
			Data = data;
		}
	}
}
