using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using Editor;
using Sandbox;

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

    Dictionary<Vector2, TilesetResource.Tile> tileDict;
    RealTimeSince timeSinceLastCornerHover = 0;
    RealTimeSince timeSinceResize = 0;

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
            Cursor = CursorShape.Arrow;

        tileDict = new();
        foreach (var tile in MainWindow.Tileset.Tiles)
        {
            for (int i = 0; i < tile.SheetRect.Size.x; i++)
            {
                for (int j = 0; j < tile.SheetRect.Size.y; j++)
                {
                    var realTile = (i == 0 && j == 0) ? tile : null;
                    tileDict[tile.SheetRect.Position + new Vector2(i, j)] = realTile;
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
            frameWidth = MainWindow.Tileset.TileSize / TextureSize.x * planeWidth;
            frameHeight = MainWindow.Tileset.TileSize / TextureSize.y * planeHeight;
            xSeparation = MainWindow.Tileset.TileSeparation.x / TextureSize.x * planeWidth;
            ySeparation = MainWindow.Tileset.TileSeparation.y / TextureSize.y * planeHeight;

            if (hasTiles)
            {
                int framesPerRow = MainWindow.Tileset.CurrentTextureSize.x / MainWindow.Tileset.CurrentTileSize;
                int framesPerHeight = MainWindow.Tileset.CurrentTextureSize.y / MainWindow.Tileset.CurrentTileSize;

                using (Gizmo.Scope("tiles"))
                {
                    Gizmo.Draw.Color = new Color(0.1f, 0.4f, 1f);
                    Gizmo.Draw.LineThickness = 2f;

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
                int framesPerRow = (int)TextureSize.x / MainWindow.Tileset.TileSize;
                int framesPerHeight = (int)TextureSize.y / MainWindow.Tileset.TileSize;

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
        bool isSelected = MainWindow.SelectedTile == tile;
        using (Gizmo.Scope($"tile_{xi}_{yi}", Transform.Zero.WithPosition(isSelected ? (Vector3.Up * 5f) : Vector3.Zero)))
        {
            float sizeX = tile.SheetRect.Size.x;
            float sizeY = tile.SheetRect.Size.y;

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
                    MainWindow.SelectedTile = tile;
                }
                else if (Gizmo.WasRightMousePressed)
                {
                    MainWindow.Tileset.Tiles.Remove(tile);
                    MainWindow.inspector.UpdateControlSheet();
                    if (isSelected) MainWindow.SelectedTile = MainWindow.Tileset.Tiles?.FirstOrDefault() ?? null;
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

            DrawBox(x, y, width, height);
        }
    }

    void DraggableCorner(TilesetResource.Tile tile, int x, int y, float xx, float yy)
    {
        int currentX = (int)tile.SheetRect.Position.x;
        int currentY = (int)tile.SheetRect.Position.y;
        float xi = currentX + x / 2f;
        float yi = currentY + y / 2f;
        float width = frameWidth * (int)tile.SheetRect.Size.x;
        float height = frameHeight * (int)tile.SheetRect.Size.y;

        bool canExpand = true;
        bool canShrink = true;

        // Can Expand Logic
        if (x != 0)
        {
            int nextX = currentX + (x > 0 ? (x * (int)tile.SheetRect.Size.x) : x);
            if (nextX < 0 || nextX >= (TextureSize.x / MainWindow.Tileset.CurrentTileSize)) canExpand = false;
            else
            {
                for (int i = 0; i < tile.SheetRect.Size.y; i++)
                {
                    int nextY = currentY + i;
                    if (tileDict.ContainsKey(new Vector2(nextX, nextY)))
                    {
                        canExpand = false;
                        break;
                    }
                }
            }
        }

        if (canExpand && y != 0)
        {
            int nextY = currentY + (y > 0 ? (y * (int)tile.SheetRect.Size.y) : y);
            if (nextY < 0 || nextY >= (TextureSize.y / MainWindow.Tileset.CurrentTileSize)) canExpand = false;
            else
            {
                for (int i = 0; i < tile.SheetRect.Size.x; i++)
                {
                    int nextX = currentX + i;
                    if (tileDict.ContainsKey(new Vector2(nextX, nextY)))
                    {
                        canExpand = false;
                        break;
                    }
                }
            }
        }

        // Can Shrink Logic
        if (x != 0 && tile.SheetRect.Size.x == 1) canShrink = false;
        if (y != 0 && tile.SheetRect.Size.y == 1) canShrink = false;

        bool canDrag = canExpand || canShrink;

        using (Gizmo.Scope($"corner_{xi}_{yi}"))
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

                    if (timeSinceResize > 0.1f)
                    {
                        var preDelta = bbox.Center - Gizmo.CurrentRay.Position;
                        var delta = new Vector2(-preDelta.y, -preDelta.x);//Gizmo.Pressed.CursorDelta;
                        var rect = tile.SheetRect;

                        // Horizontal check
                        if (x != 0)
                        {
                            if (Math.Abs(delta.x) > frameWidth / 2f)
                            {
                                // Expanding
                                if (Math.Sign(delta.x) == Math.Sign(x))
                                {
                                    // Expanding Backwards
                                    if (delta.x < 0)
                                    {
                                        rect.Position -= new Vector2(1, 0);
                                        rect.Size += new Vector2(1, 0);
                                    }
                                    else
                                    {
                                        rect.Size += new Vector2(1, 0);
                                    }
                                }
                                // Shinking
                                else
                                {
                                    // Shrinking Backwards
                                    if (delta.x > 0)
                                    {
                                        rect.Size -= new Vector2(1, 0);
                                        rect.Position += new Vector2(1, 0);
                                    }
                                    else
                                    {
                                        rect.Size -= new Vector2(1, 0);
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
                                    // Expanding
                                    if (delta.y < 0)
                                    {
                                        rect.Position -= new Vector2(0, 1);
                                        rect.Size += new Vector2(0, 1);
                                    }
                                    else
                                    {
                                        rect.Size += new Vector2(0, 1);
                                    }
                                }
                                else
                                {
                                    // Shrink
                                    if (delta.y > 0)
                                    {
                                        rect.Size -= new Vector2(0, 1);
                                        rect.Position += new Vector2(0, 1);
                                    }
                                    else
                                    {
                                        rect.Size -= new Vector2(0, 1);
                                    }
                                }
                            }
                        }

                        tile.SheetRect = rect;
                        timeSinceResize = 0f;
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
                    var tile = new TilesetResource.Tile(new Rect(new Vector2(xi, yi), 1))
                    {
                        Tileset = MainWindow.Tileset
                    };
                    MainWindow.Tileset.Tiles.Add(tile);
                    MainWindow.inspector.UpdateControlSheet();
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
}