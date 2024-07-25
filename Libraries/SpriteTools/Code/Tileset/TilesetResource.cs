using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

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

	public int AtlasWidth { get; set; } = 16;
	public List<Tile> Tiles { get; set; } = new();

	public void InitFromAtlas(string filePath, int tileSize, int atlasWidth)
	{
		var texture = Texture.Load(FileSystem.Mounted, filePath);
		FilePath = filePath;
		TileSize = tileSize;
		AtlasWidth = atlasWidth;
	}

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
			["TileSeparation"] = TileSeparation.ToString(),
			["AtlasWidth"] = AtlasWidth,
			["Tiles"] = Json.Serialize(Tiles)
		};
		return obj.ToJsonString();
	}

	public void Deserialize(string json)
	{
		var obj = JsonNode.Parse(json);
		FilePath = obj["FilePath"].GetValue<string>();
		TileSize = obj["TileSize"].GetValue<int>();
		TileSeparation = Vector2Int.Parse(obj["TileSeparation"].GetValue<string>());
		AtlasWidth = obj["AtlasWidth"].GetValue<int>();
		Tiles = Json.Deserialize<List<Tile>>(obj["Tiles"].GetValue<string>());
	}

	public class Tile
	{
		public Rect SheetRect { get; set; }

		public Tile(Rect sheetRect)
		{
			SheetRect = sheetRect;
		}

		public Tile Copy()
		{
			var copy = new Tile(SheetRect);
			return copy;
		}
	}
}
