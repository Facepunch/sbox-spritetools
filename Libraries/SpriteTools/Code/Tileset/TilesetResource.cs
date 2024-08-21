using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools;

[GameResource("2D Tileset", "tileset", "A 2D Tileset atlas", Icon = "calendar_view_month", IconBgColor = "#fab006")]
public class TilesetResource : GameResource
{
	[Property, ImageAssetPath, Title("Tileset Image"), Group("Tileset Setup")]
	public string FilePath { get; set; }

	[Property, Group("Tileset Setup")]
	public Vector2Int TileSize { get; set; } = new Vector2Int(32, 32);

	[Property, Group("Tileset Setup")]
	public Vector2Int TileSeparation { get; set; } = 0;

	[Property, Group("Additional Settings")]
	public float TileScale { get; set; } = 1.0f;

	[Property, Group("Tiles")]
	public List<Tile> Tiles { get; set; } = new();

	[JsonIgnore, Hide]
	public Dictionary<Guid, Tile> TileMap { get; set; } = new();

	[Hide] public Vector2Int CurrentTextureSize { get; set; } = Vector2Int.One;
	[Hide] public Vector2Int CurrentTileSize { get; set; } = new Vector2Int(32, 32);

	public Vector2 GetTiling()
	{
		return (Vector2)CurrentTileSize / CurrentTextureSize;
	}

	public Vector2 GetOffset(Vector2Int cellPosition)
	{
		return new Vector2(cellPosition.x * CurrentTileSize.x, cellPosition.y * CurrentTileSize.y) / CurrentTextureSize;
	}

	public Vector2 GetTileSize()
	{
		return TileSize * TileScale;
	}

	public Vector2 GetCurrentTileSize()
	{
		return CurrentTileSize * TileScale;
	}

	public void AddTile(Tile tile)
	{
		Tiles.Add(tile);
		TileMap[tile.Id] = tile;
		tile.Tileset = this;
	}

	public void RemoveTile(Tile tile)
	{
		TileMap.Remove(tile.Id);
		Tiles.Remove(tile);
	}

	public string Serialize()
	{
		var obj = new JsonObject()
		{
			["FilePath"] = FilePath,
			["TileSize"] = TileSize.ToString(),
			["CurrentTileSize"] = CurrentTileSize.ToString(),
			["TileSeparation"] = TileSeparation.ToString(),
			["Tiles"] = Json.Serialize(Tiles)
		};
		return obj.ToJsonString();
	}

	public void Deserialize(string json)
	{
		var obj = JsonNode.Parse(json);
		FilePath = obj["FilePath"]?.GetValue<string>() ?? "";
		TileSize = Vector2Int.Parse(obj["TileSize"]?.GetValue<string>() ?? "32,32");
		CurrentTileSize = Vector2Int.Parse(obj["CurrentTileSize"]?.GetValue<string>() ?? "32,32");
		TileSeparation = Vector2Int.Parse(obj["TileSeparation"]?.GetValue<string>() ?? "0,0");
		Tiles = Json.Deserialize<List<Tile>>(obj["Tiles"]?.GetValue<string>() ?? "[]");
		InternalUpdateTiles();
	}

	protected override void PostLoad()
	{
		base.PostLoad();

		InternalUpdateTiles();
	}

	protected override void PostReload()
	{
		base.PostReload();

		InternalUpdateTiles();
	}

	public void InternalUpdateTiles()
	{
		foreach (var tile in Tiles)
		{
			tile.Tileset = this;
			TileMap[tile.Id] = tile;
		}
	}

	public class Tile
	{
		public Guid Id { get; set; }

		[JsonIgnore, ReadOnly, Property]
		public int Index => Tileset?.Tiles?.IndexOf(this) ?? -1;

		[Property]
		public string Name { get; set; } = "";

		[Property]
		public TagSet Tags { get; set; }

		[Property]
		public Vector2Int Position { get; set; }

		[Property]
		public Vector2Int Size { get; set; }

		[JsonIgnore]
		public TilesetResource Tileset;

		public Tile(Vector2Int position, Vector2Int size)
		{
			Id = Guid.NewGuid();
			Position = position;
			Size = size;
		}

		public Tile Copy()
		{
			var copy = new Tile(Position, Size)
			{
				Name = Name
			};
			return copy;
		}
	}
}
