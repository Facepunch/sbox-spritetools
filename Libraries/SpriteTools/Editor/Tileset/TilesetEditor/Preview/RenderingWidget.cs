using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor.Preview;

public class RenderingWidget : SpriteRenderingWidget
{
	MainWindow MainWindow;

	float planeWidth;
	float planeHeight;
	float startX;
	float startY;
	float frameWidth;
	float frameHeight;
	float xSeparation;
	float ySeparation;
	Vector3 startMovePosition;

	Dictionary<Vector2, TilesetResource.Tile> tileDict;
	RealTimeSince timeSinceLastCornerHover = 0;

	public RenderingWidget(MainWindow window, Widget parent) : base(parent)
	{
		MainWindow = window;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		SceneInstance.Input.IsHovered = IsUnderMouse;
		SceneInstance.UpdateInputs(Camera, this);

		if (timeSinceLastCornerHover > 0.025f)
		{
			Cursor = CursorShape.Arrow;
		}

		tileDict = new();
		foreach (var tile in MainWindow.Tileset.Tiles)
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
			var hasTiles = (MainWindow?.Tileset?.Tiles?.Count ?? 0) > 0;

			planeWidth = 100f * TextureRect.Transform.Scale.y;
			planeHeight = 100f * TextureRect.Transform.Scale.x;
			startX = -(planeWidth / 2f);
			startY = -(planeHeight / 2f);
			frameWidth = MainWindow.Tileset.TileSize.x / TextureSize.x * planeWidth;
			frameHeight = MainWindow.Tileset.TileSize.y / TextureSize.y * planeHeight;
			xSeparation = MainWindow.Tileset.TileSeparation.x / TextureSize.x * planeWidth;
			ySeparation = MainWindow.Tileset.TileSeparation.y / TextureSize.y * planeHeight;

			{
				int framesPerRow = MainWindow.Tileset.CurrentTextureSize.x / MainWindow.Tileset.CurrentTileSize.x;
				int framesPerHeight = MainWindow.Tileset.CurrentTextureSize.y / MainWindow.Tileset.CurrentTileSize.y;
				if (!hasTiles)
				{
					framesPerRow = (int)TextureSize.x / MainWindow.Tileset.TileSize.x;
					framesPerHeight = (int)TextureSize.y / MainWindow.Tileset.TileSize.y;
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
								else
								{
									EmptyTileControl(xi, yi);
								}
								xi++;
							}
							xi = 0;
							yi++;
						}
					}
				}
			}

			if (!hasTiles || MainWindow.inspector.btnRegenerate.IsUnderMouse)
			{
				int framesPerRow = (int)TextureSize.x / MainWindow.Tileset.TileSize.x;
				int framesPerHeight = (int)TextureSize.y / MainWindow.Tileset.TileSize.y;

				using (Gizmo.Scope("setup"))
				{
					Gizmo.Draw.Color = Color.White.WithAlpha(0.4f);
					Gizmo.Draw.LineThickness = 1f;

					int xi = 0;
					int yi = 0;

					if (framesPerRow * framesPerHeight < 2048)
					{

						while (yi < framesPerHeight)
						{
							while (xi < framesPerRow)
							{
								var x = startX + (xi * frameWidth) + (xi * xSeparation);
								var y = startY + (yi * frameHeight) + (yi * ySeparation);
								DrawBox(x, y, frameWidth, frameHeight);
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
		bool isSelected = MainWindow?.inspector?.tileList?.Selected?.Any(x => x.Tile == tile) ?? false;
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

			if (MainWindow.inspector.SelectedTab == 1)
			{

				if (isSelected || Gizmo.Pressed.This)
				{
					Gizmo.Draw.LineThickness = 4;
					Gizmo.Draw.Color = Color.Yellow;
				}

				if (Gizmo.WasLeftMousePressed) startMovePosition = Gizmo.CurrentRay.Position;

				if (Gizmo.Pressed.This)
				{
					Cursor = CursorShape.SizeAll;
					timeSinceLastCornerHover = 0f;
					var preDelta = startMovePosition - Gizmo.CurrentRay.Position;
					var deltaf = new Vector2(-preDelta.y, -preDelta.x);
					if (Math.Abs(deltaf.x) >= frameWidth / 2f)
					{
						int xx = Math.Sign(deltaf.x);
						if (xx != 0 && CanExpand(tile, xx, 0))
						{
							startMovePosition += new Vector3(0, xx * frameWidth);
							tile.Position += new Vector2Int(xx, 0);
						}
					}
					if (Math.Abs(deltaf.y) >= frameHeight / 2f)
					{
						int yy = Math.Sign(deltaf.y);
						if (yy != 0 && CanExpand(tile, 0, yy))
						{
							startMovePosition += new Vector3(yy * frameHeight, 0);
							tile.Position += new Vector2Int(0, yy);
						}
					}
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
						MainWindow.SelectTile(tile, Gizmo.IsCtrlPressed || Gizmo.IsShiftPressed);
					}
					else if (Gizmo.WasRightMousePressed)
					{
						MainWindow.DeleteTile(tile);
					}
				}

				if (isSelected)
				{
					using (Gizmo.Scope("selected"))
					{
						Gizmo.Draw.Color = Color.Orange;
						Gizmo.Draw.LineThickness = 3;
						// Draggable Corners
						for (int i = -1; i <= 1; i++)
						{
							for (int j = -1; j <= 1; j++)
							{
								if (i == 0 && j == 0) continue;
								DraggableCorner(tile, i, j, x + width * (i + 1) / 2f, y + height * (j + 1) / 2f);
							}
						}
					}
				}
			}

			DrawBox(x, y, width, height);
		}
	}

	void DraggableCorner(TilesetResource.Tile tile, int x, int y, float xx, float yy)
	{
		int currentX = (int)tile.Position.x;
		int currentY = (int)tile.Position.y;
		float xi = currentX + x / 2f;
		float yi = currentY + y / 2f;
		float width = frameWidth * (int)tile.Size.x;
		float height = frameHeight * (int)tile.Size.y;

		// Can Expand Logic
		bool canExpandX = CanExpand(tile, x, 0);
		bool canExpandY = CanExpand(tile, 0, y);

		// Can Shrink Logic
		bool canShrinkX = !(x != 0 && tile.Size.x == 1);
		bool canShrinkY = !(y != 0 && tile.Size.y == 1);

		bool canDrag = (canExpandX && x != 0) || (canExpandY && y != 0) || (canShrinkX && x != 0) || (canShrinkY && y != 0);

		using (Gizmo.Scope($"corner_{x}_{y}"))
		{
			if (!canDrag)
			{
				Gizmo.Draw.LineThickness = 1;
				Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(0.2f);
			}

			if (canDrag)
			{
				var bbox = BBox.FromPositionAndSize(new Vector3(yy, xx, 1f), new Vector3(2, 2, 1f));
				Gizmo.Hitbox.BBox(bbox);

				if (Gizmo.Pressed.This)
				{
					Gizmo.Draw.Color = Color.Lerp(Gizmo.Draw.Color, Color.Red, 0.3f);

					var preDelta = bbox.Center - Gizmo.CurrentRay.Position;
					var delta = new Vector2(-preDelta.y, -preDelta.x);//Gizmo.Pressed.CursorDelta;
					var position = tile.Position;
					var size = tile.Size;

					// Horizontal check
					if (x != 0)
					{
						if (Math.Abs(delta.x) > frameWidth / 2f)
						{
							// Expanding
							if (Math.Sign(delta.x) == Math.Sign(x))
							{
								if (canExpandX)
								{
									// Expanding Backwards
									if (delta.x < 0)
									{
										position -= new Vector2Int(1, 0);
										size += new Vector2Int(1, 0);
									}
									else
									{
										size += new Vector2Int(1, 0);
									}
								}
							}
							// Shinking
							else if (canShrinkX)
							{
								// Shrinking Backwards
								if (delta.x > 0)
								{
									size -= new Vector2Int(1, 0);
									position += new Vector2Int(1, 0);
								}
								else
								{
									size -= new Vector2Int(1, 0);
								}
							}
						}
					}

					// Vertical check
					if (y != 0)
					{
						if (Math.Abs(delta.y) > frameHeight / 2f)
						{
							if (Math.Sign(delta.y) == Math.Sign(y))
							{
								if (canExpandY)
								{
									// Expanding
									if (delta.y < 0)
									{
										position -= new Vector2Int(0, 1);
										size += new Vector2Int(0, 1);
									}
									else
									{
										size += new Vector2Int(0, 1);
									}
								}
							}
							else if (canShrinkY)
							{
								// Shrink
								if (delta.y > 0)
								{
									size -= new Vector2Int(0, 1);
									position += new Vector2Int(0, 1);
								}
								else
								{
									size -= new Vector2Int(0, 1);
								}
							}
						}
					}

					if (tile.Position != position || tile.Size != size)
					{
						tile.Position = position;
						tile.Size = size;
					}
				}
			}

			if (canDrag && Gizmo.IsHovered)
			{
				Gizmo.Draw.SolidSphere(new Vector3(yy, xx, 10f), 0.5f, 2, 4);
				Cursor = (x, y) switch
				{
					(-1, -1) => CursorShape.SizeFDiag,
					(-1, 0) => CursorShape.SizeH,
					(-1, 1) => CursorShape.SizeBDiag,
					(0, -1) => CursorShape.SizeV,
					(0, 1) => CursorShape.SizeV,
					(1, -1) => CursorShape.SizeBDiag,
					(1, 0) => CursorShape.SizeH,
					(1, 1) => CursorShape.SizeFDiag,
					_ => CursorShape.Arrow
				};
				timeSinceLastCornerHover = 0f;
			}
			else
			{
				Gizmo.Draw.LineCircle(new Vector3(yy, xx, 10f), Vector3.Up, 0.5f, 0, 360, 8);
			}


		}
	}

	void EmptyTileControl(int xi, int yi)
	{
		using (Gizmo.Scope($"tile_{xi}_{yi}", Transform.Zero))
		{
			Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(0.04f);

			var x = startX + (xi * frameWidth + xi * xSeparation);
			var y = startY + (yi * frameHeight + yi * ySeparation);
			var width = frameWidth;
			var height = frameHeight;

			if (MainWindow.inspector.SelectedTab == 1)
			{
				var bbox = BBox.FromPositionAndSize(new Vector3(y + height / 2f, x + width / 2f, 1f), new Vector3(height, width, 1f));
				Gizmo.Hitbox.BBox(bbox);

				if (Gizmo.IsHovered)
				{
					using (Gizmo.Scope("hover"))
					{
						Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(0.2f);
						Gizmo.Draw.SolidBox(bbox);
					}
					if (Gizmo.WasLeftMousePressed)
					{
						MainWindow.CreateTile(xi, yi, Gizmo.IsCtrlPressed || Gizmo.IsShiftPressed);
					}
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
			if (nextX < 0 || nextX >= (TextureSize.x / MainWindow.Tileset.CurrentTileSize.x)) return false;
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
			if (nextY < 0 || nextY >= (TextureSize.y / MainWindow.Tileset.CurrentTileSize.y)) return false;
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