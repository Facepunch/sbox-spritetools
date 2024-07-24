using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools;

[GameResource("Tileset", "tileset", "A 2D Tileset atlas", Icon = "calendar_view_month", IconBgColor = "#fab006")]
public class TilesetResource : GameResource
{
	public List<Tile> Tiles { get; set; } = new();
	public int TileSize { get; set; } = 32;

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
		public string FilePath { get; set; }
		public Rect SheetRect { get; set; }

		public Tile(string filePath)
		{
			FilePath = filePath;
			SheetRect = new Rect(0, 0, 0, 0);
		}

		public Tile(string filePath, Rect sheetRect)
		{
			FilePath = filePath;
			SheetRect = sheetRect;
		}

		public Tile Copy()
		{
			var copy = new Tile(FilePath, SheetRect);
			return copy;
		}
	}
}
