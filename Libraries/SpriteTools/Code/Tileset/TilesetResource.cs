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
	public int TileSize { get; set; } = 32;

	[Property, Group("Tileset Setup")]
	public Vector2Int TileSeparation { get; set; } = 0;

	[Property, Group("Tiles")]
	public List<Tile> Tiles { get; set; } = new();

	public Vector2Int CurrentTextureSize { get; set; } = Vector2Int.One;
	public int CurrentTileSize { get; set; } = 32;

	public Vector2 GetTiling()
	{
		return new Vector2(1, 1);
	}

	public Vector2 GetOffset(int index)
	{
		return new Vector2(0, 0);
	}

	public string Serialize()
	{
		var obj = new JsonObject()
		{
			["FilePath"] = FilePath,
			["TileSize"] = TileSize,
			["CurrentTileSize"] = CurrentTileSize,
			["TileSeparation"] = TileSeparation.ToString(),
			["Tiles"] = Json.Serialize(Tiles)
		};
		return obj.ToJsonString();
	}

	public void Deserialize(string json)
	{
		var obj = JsonNode.Parse(json);
		FilePath = obj["FilePath"]?.GetValue<string>() ?? "";
		TileSize = obj["TileSize"]?.GetValue<int>() ?? 32;
		CurrentTileSize = obj["CurrentTileSize"]?.GetValue<int>() ?? 32;
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
		}
	}

	public class Tile
	{
		[JsonIgnore]
		public Guid Id = Guid.NewGuid();

		[JsonIgnore, ReadOnly, Property]
		public int Index => Tileset?.Tiles?.IndexOf(this) ?? -1;

		[Property]
		public string Name { get; set; } = "";

		[Property]
		public Vector2Int Position { get; set; }

		[Property]
		public Vector2Int Size { get; set; }

		[JsonIgnore]
		public TilesetResource Tileset;

		public Tile(Vector2Int position, Vector2Int size)
		{
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
