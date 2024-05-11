using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class RenderingWidget : NativeRenderingWidget
{
    MainWindow MainWindow;

    public Action OnDragSelected;

    public SceneWorld World;
    public SceneObject TextureRect;
    public Vector2 TextureSize;
    public Draggable OriginMarker;
    List<Draggable> Attachments = new();
    public Draggable LastDragged = null;
    public Material PreviewMaterial;

    float targetZoom = 115f;
    Vector2 cameraGrabPos = Vector2.Zero;
    bool cameraGrabbing = false;

    Draggable dragging = null;
    Vector3 draggableGrabPos = Vector3.Zero;

    bool holdingControl = false;

    public RenderingWidget(MainWindow window, Widget parent) : base(parent)
    {
        MainWindow = window;
        MouseTracking = true;
        FocusMode = FocusMode.Click;

        World = new SceneWorld();
        Camera = new SceneCamera
        {
            World = World,
            AmbientLightColor = Color.White * 1f,
            ZNear = 0.1f,
            ZFar = 4000,
            EnablePostProcessing = true,
            Position = new Vector3(0, 0, targetZoom),
            Angles = new Angles(90, 180, 0),
            Ortho = true,
            OrthoHeight = 512f,
            AntiAliasing = true,
            BackgroundColor = Theme.ControlBackground,
        };

        new SceneDirectionalLight(World, new Angles(90, 0, 0), Color.White);

        var backgroundMat = Material.Load("materials/sprite_editor_transparent.vmat");
        var background = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        background.SetMaterialOverride(backgroundMat);
        background.Position = new Vector3(0, 0, -1);

        PreviewMaterial = Material.Load("materials/spritegraph.vmat").CreateCopy();
        PreviewMaterial.Set("Texture", Color.Transparent);
        TextureRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        TextureRect.SetMaterialOverride(PreviewMaterial);
        TextureRect.Flags.WantsFrameBufferCopy = true;
        TextureRect.Flags.IsTranslucent = true;
        TextureRect.Flags.IsOpaque = false;
        TextureRect.Flags.CastShadows = false;

        var markerMaterial = Material.Load("materials/sprite_editor_origin.vmat");
        OriginMarker = new Draggable(World, "models/preview_quad.vmdl", Transform.Zero);
        OriginMarker.SetMaterialOverride(markerMaterial);
        OriginMarker.Tags.Add("origin");
        OriginMarker.Position = new Vector3(0, 0, 1f);
        OriginMarker.Flags.WantsFrameBufferCopy = true;
        OriginMarker.Flags.IsTranslucent = true;
        OriginMarker.Flags.IsOpaque = false;
        OriginMarker.Flags.CastShadows = false;
        OriginMarker.OnPositionChanged = (Vector2 pos) =>
        {
            if (MainWindow.SelectedAnimation is null) return;

            var origin = (pos / 100f) + (Vector2.One * 0.5f);
            if (!holdingControl)
            {
                origin = origin.SnapToGrid(1f / TextureSize.x, true, false);
                origin = origin.SnapToGrid(1f / TextureSize.y, false, true);
            }

            MainWindow.SelectedAnimation.Origin = origin;
        };
    }

    protected override void OnWheel(WheelEvent e)
    {
        base.OnWheel(e);

        Zoom(e.Delta);
    }

    protected override void OnMousePress(MouseEvent e)
    {
        base.OnMousePress(e);

        if (dragging is null && e.MiddleMouseButton)
        {
            cameraGrabbing = true;
            cameraGrabPos = e.LocalPosition;
        }
        else if (e.LeftMouseButton)
        {
            var cursorLocalPos = Editor.Application.CursorPosition - ScreenRect.Position;
            var tr = World.Trace.Ray(Camera.GetRay(cursorLocalPos, Size), 5000f).Run();
            if (tr.SceneObject is Draggable draggable)
            {
                dragging = draggable;
                draggableGrabPos = tr.EndPosition.WithZ(0f);
                LastDragged = draggable;
            }
            else
            {
                LastDragged = null;
            }
            OnDragSelected?.Invoke();
        }
    }

    protected override void OnMouseMove(MouseEvent e)
    {
        base.OnMouseMove(e);

        if (dragging is not null)
        {
            var cursorLocalPos = Editor.Application.CursorPosition - ScreenRect.Position;
            var tr = World.Trace.Ray(Camera.GetRay(cursorLocalPos, Size), 5000f).Run();
            var pos = tr.EndPosition.WithZ(0f);
            draggableGrabPos = pos;
            dragging?.OnPositionChanged.Invoke(new Vector2(pos.y, pos.x));
        }
        else if (cameraGrabbing)
        {
            var delta = (cameraGrabPos - e.LocalPosition) * (Camera.OrthoHeight / 512f);
            Camera.Position = new Vector3(Camera.Position.x + delta.y, Camera.Position.y + delta.x, Camera.Position.z);
            cameraGrabPos = e.LocalPosition;
        }
    }

    protected override void OnMouseReleased(MouseEvent e)
    {
        base.OnMouseReleased(e);

        if (e.MiddleMouseButton)
        {
            cameraGrabbing = false;
        }
        if (dragging is not null)
        {
            dragging = null;
        }
    }

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);

        if (e.Key == KeyCode.Control)
        {
            holdingControl = true;
        }
    }

    protected override void OnKeyRelease(KeyEvent e)
    {
        base.OnKeyRelease(e);

        if (e.Key == KeyCode.Control)
        {
            holdingControl = false;
        }
    }

    public override void PreFrame()
    {
        Camera.OrthoHeight = Camera.OrthoHeight.LerpTo(targetZoom, 0.1f);
        float scale = Camera.OrthoHeight / 1024f;
        if (MainWindow.SelectedAnimation is not null)
        {
            OriginMarker.RenderingEnabled = true;
            var origin = MainWindow.SelectedAnimation.Origin;
            origin -= Vector2.One * 0.5f;
            origin *= 100f;
            OriginMarker.Position = new Vector3(origin.y, origin.x, 1f);
            OriginMarker.Transform = OriginMarker.Transform.WithScale(new Vector3(scale, scale, 1f));
        }
        else
        {
            OriginMarker.RenderingEnabled = false;
        }

        scale /= 1.5f;
        foreach (var attachmentWidget in Attachments)
        {
            attachmentWidget.RenderingEnabled = false;
        }

        foreach (var attachment in MainWindow.SelectedAnimation?.Attachments ?? new List<SpriteAttachment>())
        {
            if (attachment is null) continue;
            var name = attachment.Name.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var attach = Attachments.FirstOrDefault(a => a.Tags.Has(name));
            var has = Attachments.Any(a => a.Tags.Has(name));
            if (!has)
            {
                var markerMaterial = Material.Load("materials/sprite_editor_attachment.vmat").CreateCopy();
                attach = new Draggable(World, "models/preview_quad.vmdl", Transform.Zero);
                attach.SetMaterialOverride(markerMaterial);
                attach.Tags.Add(name);
                attach.Position = new Vector3(0, 0, 10f);
                attach.Transform = attach.Transform.WithRotation(new Angles(0, 45, 0));
                attach.Flags.WantsFrameBufferCopy = true;
                attach.Flags.IsTranslucent = true;
                attach.Flags.IsOpaque = false;
                attach.Flags.CastShadows = false;
                attach.OnPositionChanged = (Vector2 pos) =>
                {
                    if (MainWindow.SelectedAnimation is null) return;

                    var attachPos = (pos / 100f) + (Vector2.One * 0.5f);
                    if (!holdingControl)
                    {
                        attachPos = attachPos.SnapToGrid(1f / TextureSize.x, true, false);
                        attachPos = attachPos.SnapToGrid(1f / TextureSize.y, false, true);
                    }

                    var currentAttachment = MainWindow.SelectedAnimation.Attachments.FirstOrDefault(a => a.Name.ToLowerInvariant() == name);
                    if (currentAttachment is null) return;

                    var index = MainWindow.CurrentFrameIndex;
                    for (int i = currentAttachment.Points.Count; i <= index; i++)
                    {
                        currentAttachment.Points.Add(attachPos);
                    }
                    currentAttachment.Points[index] = attachPos;

                    // Log.Info($"{currentAttachment.} - {name} - {index} - {attachPos}");
                };
                Attachments.Add(attach);
            }
            else
            {
                if (MainWindow.SelectedAnimation is not null)
                {
                    if (MainWindow.CurrentFrameIndex < attachment.Points.Count)
                    {
                        var attachPos = attachment.Points[MainWindow.CurrentFrameIndex];
                        attachPos -= Vector2.One * 0.5f;
                        attachPos *= 100f;
                        attach.Position = new Vector3(attachPos.y, attachPos.x, 10f);
                    }
                    else
                    {
                        for (int i = attachment.Points.Count - 1; i >= 0; i--)
                        {
                            if (attachment.Points.Count > i)
                            {
                                var attachPos1 = attachment.Points[i];
                                attachPos1 -= Vector2.One * 0.5f;
                                attachPos1 *= 100f;
                                attach.Position = new Vector3(attachPos1.y, attachPos1.x, 10f);
                                break;
                            }
                        }
                    }
                }
            }
            if (attachment.Visible)
            {
                attach.RenderingEnabled = true;
            }
            attach.Transform = attach.Transform.WithScale(new Vector3(scale, scale, 1f));
            attach.ColorTint = attachment.Color;
        }
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        World?.Delete();
        World = null;
    }

    public void Zoom(float delta)
    {
        targetZoom *= 1f - (delta / 500f);
        targetZoom = targetZoom.Clamp(1, 1000);
    }

    public void Fit()
    {
        targetZoom = 115f;
        Camera.Position = new Vector3(0, 0, targetZoom);
    }
}