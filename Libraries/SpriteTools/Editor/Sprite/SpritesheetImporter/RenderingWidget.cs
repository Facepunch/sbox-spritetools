using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.SpritesheetImporter;

public class RenderingWidget : SpriteRenderingWidget
{
    SpritesheetImporter Importer;

    public RenderingWidget(SpritesheetImporter importer, Widget parent) : base(parent)
    {
        Importer = importer;
    }

    protected override void OnMousePress(MouseEvent e)
    {
        base.OnMousePress(e);
    }

    protected override void OnMouseMove(MouseEvent e)
    {
        base.OnMouseMove(e);
    }

    protected override void OnMouseReleased(MouseEvent e)
    {
        base.OnMouseReleased(e);
    }

    [EditorEvent.Frame]
    public void Frame()
    {
        if (Importer.Settings.NumberOfFrames <= 0) Importer.Settings.NumberOfFrames = 1;
        if (Importer.Settings.FramesPerRow <= 0) Importer.Settings.FramesPerRow = 1;
        if (Importer.Settings.FramesPerRow > Importer.Settings.NumberOfFrames)
        {
            Importer.Settings.NumberOfFrames = Importer.Settings.FramesPerRow;
        }

        using (SceneInstance.Push())
        {
            Gizmo.Draw.Color = Color.White;
            Gizmo.Draw.LineThickness = 2f;

            var planeWidth = 100f * TextureRect.Transform.Scale.y;
            var planeHeight = 100f * TextureRect.Transform.Scale.x;

            float startX = Importer.Settings.HorizontalCellOffset * Importer.Settings.FrameWidth + Importer.Settings.HorizontalPixelOffset;
            float startY = Importer.Settings.VerticalCellOffset * Importer.Settings.FrameHeight + Importer.Settings.VerticalPixelOffset;
            startX = (startX / TextureSize.x * planeWidth) - (planeWidth / 2f);
            startY = (startY / TextureSize.y * planeHeight) - (planeHeight / 2f);
            float frameWidth = Importer.Settings.FrameWidth / TextureSize.x * planeWidth;
            float frameHeight = Importer.Settings.FrameHeight / TextureSize.y * planeHeight;
            float xSeparation = Importer.Settings.HorizontalSeparation / TextureSize.x * planeWidth;
            float ySeparation = Importer.Settings.VerticalSeparation / TextureSize.y * planeHeight;

            int framesPerRow = Math.Clamp(Importer.Settings.FramesPerRow, 1, (int)TextureSize.x / Importer.Settings.FrameWidth);

            for (int i = 0; i < Importer.Settings.NumberOfFrames; i++)
            {
                int cellX = i % framesPerRow;
                int cellY = i / framesPerRow;

                float x = startX + (cellX) * (frameWidth + xSeparation);
                float y = startY + (cellY) * (frameHeight + ySeparation);

                // Draw Box
                Gizmo.Draw.Line(new Vector3(y, x, 0), new Vector3(y, x + frameWidth, 0));
                Gizmo.Draw.Line(new Vector3(y, x + frameWidth, 0), new Vector3(y + frameHeight, x + frameWidth, 0));
                Gizmo.Draw.Line(new Vector3(y + frameHeight, x + frameWidth, 0), new Vector3(y + frameHeight, x, 0));
                Gizmo.Draw.Line(new Vector3(y + frameHeight, x, 0), new Vector3(y, x, 0));
            }
        }
    }
}