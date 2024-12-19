using Sandbox;
using SpriteTools.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools;

[GameResource("2D Tileset", "tileset", "A 2D Tileset atlas", Icon = "calendar_view_month", IconBgColor = "#fab006")]
public partial class TilesetResource : GameResource
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

	[Property, Group("Autotile Settings")]
	public TilesetResource ImportAutotileFrom { get; set; }

	[Property, Group("Autotile Brushes"), Order(9999)]
	public List<AutotileBrush> AutotileBrushes { get; set; } = new();

	[JsonIgnore, Hide]
	internal Dictionary<Guid, Tile> TileMap { get; set; } = new();

	[Hide] public Vector2Int CurrentTextureSize { get; set; } = Vector2Int.One;
	[Hide] public Vector2Int CurrentTileSize { get; set; } = new Vector2Int(32, 32);

	[JsonIgnore, Hide] internal Dictionary<string, (Vector2Int, Color32[])> TilesetPixels { get; set; } = new();

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

	public Tile GetTileFromId(Guid id)
	{
		if (id == Guid.Empty) return null;
		if (TileMap.ContainsKey(id))
		{
			return TileMap[id];
		}
		return null;
	}

	public string SerializeString()
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

	public void DeserializeString(string json)
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

		ReloadTileset();
	}

	protected override void PostReload()
	{
		base.PostReload();

		ReloadTileset();
	}

	void ReloadTileset()
	{
		var sourceFile = this.ResourcePath;
		if (sourceFile.EndsWith("_c")) sourceFile = sourceFile.Substring(0, sourceFile.Length - 2);
		var json = Json.Deserialize<JsonObject>(FileSystem.Mounted.ReadAllText(sourceFile));
		var tileList = json["Tiles"] as JsonArray;
		if (tileList is not null)
		{
			Tiles.Clear();
			foreach (var obj in tileList)
			{
				if (obj is JsonObject jsonObj)
				{
					var tile = new Tile(0, 1);
					tile.Deserialize(jsonObj);
					tile.Tileset = this;
					Tiles.Add(tile);
				}
			}
		}

		InternalUpdateTiles();
	}

	public void InternalUpdateTiles()
	{
		foreach (var tile in Tiles)
		{
			TileMap[tile.Id] = tile;
			tile.Tileset = this;
		}
	}

	protected override void OnJsonSerialize(JsonObject node)
	{
		base.OnJsonSerialize(node);

		var tilesList = node["Tiles"] as JsonArray;
		tilesList.Clear();
		foreach (var tile in Tiles)
		{
			tilesList.Add(tile.Serialize());
		}
		node["Tiles"] = tilesList;
	}

	public class TileTextureData
	{
		public Vector2Int Size { get; set; }
		public byte[] Data { get; set; }

		public TileTextureData(Vector2Int size, byte[] data)
		{
			Size = size;
			Data = data;
		}
	}
}
