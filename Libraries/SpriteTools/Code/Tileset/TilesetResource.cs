using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools;

[GameResource("Tileset", "tileset", "A 2D Tileset atlas", Icon = "calendar_view_month", IconBgColor = "#fab006")]
public class TilesetResource : GameResource
{
	[Property, ImageAssetPath] public string Atlas { get; set; }
	[Property] public int TileSize { get; set; }
	[Property] public int TileSpacing { get; set; }
	[Property] public int TileMargin { get; set; }
	[Property] public int Width { get; set; }
	[Property] public int Height { get; set; }

	public void Init()
	{
		var texture = Texture.Load(Atlas);
		if (texture == null)
			return;

		Width = texture.Width;
		Height = texture.Height;
	}

	// Calculate the number of cells in the atlas
	public List<int> CalculateCellOfAtlas()
	{
		List<int> cells = new List<int>();
		if (Width == 0 || Height == 0)
		{
			Init();
		}

		int tilesPerRow = (Width - TileMargin) / (TileSize + TileSpacing);
		int tilesPerColumn = (Height - TileMargin) / (TileSize + TileSpacing);
		for (int y = 0; y < tilesPerColumn; y++)
		{
			for (int x = 0; x < tilesPerRow; x++)
			{
				cells.Add(x + y * tilesPerRow);
			}
		}
		return cells;
	}

	public Vector2 GetTiling()
	{
		if (Width == 0 || Height == 0)
		{
			Init();
		}

		if (Width == 0 || Height == 0)
		{
			throw new DivideByZeroException("Texture width or height is zero.");
		}

		float u = 1.0f / (Width / (TileSize + TileSpacing));
		float v = 1.0f / (Height / (TileSize + TileSpacing));
		return new Vector2(u, v);
	}

	public Vector2 GetOffset(int index)
	{
		if (Width == 0 || Height == 0)
		{
			Init();
		}

		if (Width == 0 || Height == 0)
		{
			throw new DivideByZeroException("Texture width or height is zero.");
		}

		int tilesPerRow = (Width - TileMargin) / (TileSize + TileSpacing);
		int xIndex = index % tilesPerRow;
		int yIndex = index / tilesPerRow;

		float u = (TileMargin + xIndex * (TileSize + TileSpacing)) / (float)Width;
		float v = (TileMargin + yIndex * (TileSize + TileSpacing)) / (float)Height;

		return new Vector2(u, v);
	}

	public int GetTileIndex(Vector2 offset, Vector2 tiling)
	{
		if (Width == 0 || Height == 0)
		{
			Init();
		}

		int tilesPerRow = (Width - TileMargin) / (TileSize + TileSpacing);
		int tilesPerColumn = (Height - TileMargin) / (TileSize + TileSpacing);

		const float tolerance = 0.001f;

		for (int y = 0; y < tilesPerColumn; y++)
		{
			for (int x = 0; x < tilesPerRow; x++)
			{
				float u = (TileMargin + x * (TileSize + TileSpacing)) / (float)Width;
				float v = (TileMargin + y * (TileSize + TileSpacing)) / (float)Height;

				if (Math.Abs(offset.x - u) < tolerance && Math.Abs(offset.y - v) < tolerance)
				{
					return x + y * tilesPerRow;
				}
			}
		}
		return -1;
	}
}
