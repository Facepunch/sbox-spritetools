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

    public RenderingWidget(MainWindow window, Widget parent) : base(parent)
    {
        MainWindow = window;
    }

    [EditorEvent.Frame]
    public void Frame()
    {
        using (SceneInstance.Push())
        {

            if ((MainWindow?.Tileset?.Tiles?.Count ?? 0) == 0 || MainWindow.inspector.btnRegenerate.IsUnderMouse)
            {
                using (Gizmo.Scope("setup"))
                {
                    Gizmo.Draw.Color = Color.White.WithAlpha(0.4f);
                    Gizmo.Draw.LineThickness = 1f;

                    var planeWidth = 100f * TextureRect.Transform.Scale.y;
                    var planeHeight = 100f * TextureRect.Transform.Scale.x;

                    float startX = 0;
                    float startY = 0;
                    int xi = 0;
                    int yi = 0;
                    startX = (startX / TextureSize.x * planeWidth) - (planeWidth / 2f);
                    startY = (startY / TextureSize.y * planeHeight) - (planeHeight / 2f);
                    float frameWidth = MainWindow.Tileset.TileSize / TextureSize.x * planeWidth;
                    float frameHeight = MainWindow.Tileset.TileSize / TextureSize.y * planeHeight;
                    float xSeparation = MainWindow.Tileset.TileSeparation.x / TextureSize.x * planeWidth;
                    float ySeparation = MainWindow.Tileset.TileSeparation.y / TextureSize.y * planeHeight;
                    int framesPerRow = (int)TextureSize.x / MainWindow.Tileset.TileSize;
                    int framesPerHeight = (int)TextureSize.y / MainWindow.Tileset.TileSize;

                    if (framesPerRow * framesPerHeight > 2048) return;

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

                    //ControlBox();
                }
            }
            else
            {

            }

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