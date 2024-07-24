using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools;

[GameResource("2D Tileset", "tileset", "A 2D Tileset atlas", Icon = "calendar_view_month", IconBgColor = "#fab006")]
public class TilesetResource : GameResource
{
	public string FilePath { get; set; }
	public int TileSize { get; set; } = 32;
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
