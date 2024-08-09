using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetTool.Preview;

public class RenderingWidget : SpriteRenderingWidget
{
	TilesetToolInspector Inspector;

	float planeWidth;
	float planeHeight;
	float startX;
	float startY;
	float frameWidth;
	float frameHeight;
	float xSeparation;
	float ySeparation;

	Dictionary<Vector2, TilesetResource.Tile> tileDict;

	protected override bool CanZoom => false;

	public RenderingWidget(TilesetToolInspector inspector, Widget parent) : base(parent)
	{
		Inspector = inspector;
		VerticalSizeMode = SizeMode.CanGrow;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		SceneInstance.Input.IsHovered = IsUnderMouse;
		SceneInstance.UpdateInputs(Camera, this);

		var layer = TilesetTool.Active?.SelectedLayer;
		if (layer is null) return;

		var tileset = layer.TilesetResource;
		if (tileset is null) return;

		var tiles = tileset?.Tiles;
		if (tiles is null) return;

		tileDict = new();
		foreach (var tile in tiles)
		{
			for (int i = 0; i < tile.Size.x; i++)
			{
				for (int j = 0; j < tile.Size.y; j++)
				{
					var realTile = (i == 0 && j == 0) ? tile : null;
					tileDict[tile.Position + new Vector2(i, j)] = realTile;
				}
			}
		}

		using (SceneInstance.Push())
		{
			var hasTiles = tiles.Count > 0;

			planeWidth = 100f * TextureRect.Transform.Scale.y;
			planeHeight = 100f * TextureRect.Transform.Scale.x;
			startX = -(planeWidth / 2f);
			startY = -(planeHeight / 2f);
			frameWidth = tileset.TileSize.x / TextureSize.x * planeWidth;
			frameHeight = tileset.TileSize.y / TextureSize.y * planeHeight;
			xSeparation = tileset.TileSeparation.x / TextureSize.x * planeWidth;
			ySeparation = tileset.TileSeparation.y / TextureSize.y * planeHeight;

			{
				int framesPerRow = tileset.CurrentTextureSize.x / tileset.CurrentTileSize.x;
				int framesPerHeight = tileset.CurrentTextureSize.y / tileset.CurrentTileSize.y;
				if (!hasTiles)
				{
					framesPerRow = (int)TextureSize.x / tileset.TileSize.x;
					framesPerHeight = (int)TextureSize.y / tileset.TileSize.y;
				}

				using (Gizmo.Scope("tiles"))
				{
					Gizmo.Draw.Color = new Color(0.1f, 0.4f, 1f);
					Gizmo.Draw.LineThickness = 3f;

					int xi = 0;
					int yi = 0;

					if (framesPerRow * framesPerHeight < 2048)
					{
						while (yi < framesPerHeight)
						{
							while (xi < framesPerRow)
							{
								if (tileDict.TryGetValue(new Vector2(xi, yi), out var tile))
								{
									if (tile is not null)
									{
										TileControl(xi, yi, tileDict[new Vector2(xi, yi)]);
									}
								}
								xi++;
							}
							xi = 0;
							yi++;
						}
					}
				}
			}
		}
	}

	void TileControl(int xi, int yi, TilesetResource.Tile tile)
	{
		bool isSelected = TilesetTool.Active?.SelectedTile == tile;
		using (Gizmo.Scope($"tile_{tile.Id}", Transform.Zero.WithPosition(isSelected ? (Vector3.Up * 5f) : Vector3.Zero)))
		{
			float sizeX = tile.Size.x;
			float sizeY = tile.Size.y;

			var x = startX + (xi * frameWidth + xi * xSeparation);
			var y = startY + (yi * frameHeight + yi * ySeparation);
			var width = frameWidth * sizeX;
			var height = frameHeight * sizeY;

			var bbox = BBox.FromPositionAndSize(new Vector3(y + height / 2f, x + width / 2f, 1f), new Vector3(height, width, 1f));
			Gizmo.Hitbox.BBox(bbox);

			if (isSelected)
			{
				Gizmo.Draw.LineThickness = 4;
				Gizmo.Draw.Color = Color.Yellow;
			}

			if (Gizmo.IsHovered)
			{
				using (Gizmo.Scope("hover"))
				{
					Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(0.5f);
					Gizmo.Draw.SolidBox(bbox);
				}
				if (Gizmo.WasLeftMousePressed)
				{
					TilesetTool.Active.SelectedTile = tile;
				}
			}

			DrawBox(x, y, width, height);
		}
	}

	void DrawBox(float x, float y, float width, float height)
	{
		Gizmo.Draw.Line(new Vector3(y, x, 0), new Vector3(y, x + width, 0));
		Gizmo.Draw.Line(new Vector3(y, x, 0), new Vector3(y + height, x, 0));
		Gizmo.Draw.Line(new Vector3(y + height, x, 0), new Vector3(y + height, x + width, 0));
		Gizmo.Draw.Line(new Vector3(y + height, x + width, 0), new Vector3(y, x + width, 0));
	}

	bool CanExpand(TilesetResource.Tile tile, int x, int y)
	{
		int currentX = tile.Position.x;
		int currentY = tile.Position.y;

		if (x != 0)
		{
			int nextX = currentX + (x > 0 ? (x * (int)tile.Size.x) : x);
			if (nextX < 0 || nextX >= (TextureSize.x / tile.Tileset.CurrentTileSize.x)) return false;
			else
			{
				for (int i = 0; i < tile.Size.y; i++)
				{
					int nextY = currentY + i;
					if (tileDict.ContainsKey(new Vector2(nextX, nextY)))
					{
						return false;
					}
				}
			}
		}

		if (y != 0)
		{
			int nextY = currentY + (y > 0 ? (y * (int)tile.Size.y) : y);
			if (nextY < 0 || nextY >= (TextureSize.y / tile.Tileset.CurrentTileSize.y)) return false;
			else
			{
				for (int i = 0; i < tile.Size.x; i++)
				{
					int nextX = currentX + i;
					if (tileDict.ContainsKey(new Vector2(nextX, nextY)))
					{
						return false;
					}
				}
			}
		}

		return true;
	}
}