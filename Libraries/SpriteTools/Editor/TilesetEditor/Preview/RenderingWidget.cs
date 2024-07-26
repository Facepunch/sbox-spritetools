using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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

    public RenderingWidget(MainWindow window, Widget parent) : base(parent)
    {
        MainWindow = window;
    }

    [EditorEvent.Frame]
    public void Frame()
    {
        SceneInstance.Input.IsHovered = IsUnderMouse;
        SceneInstance.UpdateInputs(Camera, this);

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
                int framesPerRow = (int)TextureSize.x / MainWindow.Tileset.CurrentTileSize;
                int framesPerHeight = (int)TextureSize.y / MainWindow.Tileset.CurrentTileSize;

                using (Gizmo.Scope("tiles"))
                {
                    Gizmo.Draw.Color = Color.Orange;
                    Gizmo.Draw.LineThickness = 2f;

                    int xi = 0;
                    int yi = 0;

                    if (framesPerRow * framesPerHeight < 2048)
                    {
                        while (yi < framesPerHeight)
                        {
                            while (xi < framesPerRow)
                            {
                                TileControl(xi, yi);
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

    void TileControl(int xi, int yi)
    {
        using (Gizmo.Scope($"tile_{xi}_{yi}", Transform.Zero))
        {
            int sizeX = 1;
            int sizeY = 1;

            var x = startX + (xi * frameWidth * sizeX + xi * xSeparation);
            var y = startY + (yi * frameHeight * sizeY + yi * ySeparation);
            var width = frameWidth * sizeX;
            var height = frameHeight * sizeY;

            var bbox = BBox.FromPositionAndSize(new Vector3(y + height / 2f, x + width / 2f, 1f), new Vector3(height, width, 1f));
            Gizmo.Hitbox.BBox(bbox);

            if (Gizmo.IsHovered)
            {
                Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(0.5f);
                Gizmo.Draw.SolidBox(bbox);
                Gizmo.Draw.Color = Gizmo.Draw.Color.WithAlpha(1f);
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