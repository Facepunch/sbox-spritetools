using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class RenderingWidget : NativeRenderingWidget
{
    MainWindow MainWindow;

    public SceneWorld World;
    public SceneObject TextureRect;
    public Vector2 TextureSize;
    Draggable OriginMarker;
    List<(Draggable, Material)> Attachments = new();
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
            var tr = World.Trace.Ray(Camera.GetRay(e.LocalPosition), 5000f).Run();
            if (tr.SceneObject is Draggable draggable)
            {
                dragging = draggable;
                draggableGrabPos = tr.EndPosition.WithZ(0f);
            }
        }
    }

    protected override void OnMouseMove(MouseEvent e)
    {
        base.OnMouseMove(e);

        if (dragging is not null)
        {
            var tr = World.Trace.Ray(Camera.GetRay(e.LocalPosition), 5000f).Run();
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

        if (e.Key == KeyCode.Space)
        {
            MainWindow?.PlayPause();
        }

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
        foreach (var attachment in MainWindow.SelectedAnimation?.Attachments ?? new List<SpriteAttachment>())
        {
            if (attachment is null) continue;
            var name = attachment.Name.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name)) continue;
            var attach = Attachments.FirstOrDefault(a => a.Item1.Tags.Has(name));
            if (!Attachments.Any(a => a.Item1.Tags.Has(name)))
            {
                var markerMaterial = Material.Load("materials/sprite_editor_attachment.vmat").CreateCopy();
                attach = (new Draggable(World, "models/preview_quad.vmdl", Transform.Zero), markerMaterial);
                attach.Item1.SetMaterialOverride(markerMaterial);
                attach.Item1.Tags.Add(name);
                attach.Item1.Position = new Vector3(0, 0, 10f);
                attach.Item1.Transform = attach.Item1.Transform.WithRotation(new Angles(0, 45, 0));
                attach.Item1.Flags.WantsFrameBufferCopy = true;
                attach.Item1.Flags.IsTranslucent = true;
                attach.Item1.Flags.IsOpaque = false;
                attach.Item1.Flags.CastShadows = false;
                attach.Item1.OnPositionChanged = (Vector2 pos) =>
                {
                    if (MainWindow.SelectedAnimation is null) return;

                    var attachPos = (pos / 100f) + (Vector2.One * 0.5f);
                    if (!holdingControl)
                    {
                        attachPos = attachPos.SnapToGrid(1f / TextureSize.x, true, false);
                        attachPos = attachPos.SnapToGrid(1f / TextureSize.y, false, true);
                    }

                    MainWindow.SelectedAnimation.Frames[MainWindow.CurrentFrameIndex].AttachmentPoints[name] = attachPos;
                };
                Attachments.Add(attach);
            }
            else
            {
                attach.Item1.RenderingEnabled = true;

                if (MainWindow.SelectedAnimation is not null)
                {
                    if (MainWindow.SelectedAnimation.Frames[MainWindow.CurrentFrameIndex].AttachmentPoints.TryGetValue(name, out var attachPos))
                    {
                        attachPos -= Vector2.One * 0.5f;
                        attachPos *= 100f;
                        attach.Item1.Position = new Vector3(attachPos.y, attachPos.x, 10f);
                    }
                    else
                    {
                        for (int i = 0; i < MainWindow.SelectedAnimation.Frames.Count; i++)
                        {
                            if (MainWindow.SelectedAnimation.Frames[i].AttachmentPoints.TryGetValue(name.ToLowerInvariant(), out var attachPos2))
                            {
                                attachPos2 -= Vector2.One * 0.5f;
                                attachPos2 *= 100f;
                                attach.Item1.Position = new Vector3(attachPos2.y, attachPos2.x, 10f);
                                break;
                            }
                        }
                    }
                }
            }
            attach.Item1.Transform = attach.Item1.Transform.WithScale(new Vector3(scale, scale, 1f));
            attach.Item1.ColorTint = attachment.Color;
        }

        int index = 0;
        foreach (var attachmentWidget in Attachments)
        {
            var attachments = MainWindow.SelectedAnimation?.Attachments;
            foreach (var attachment in attachments)
            {
                if (attachmentWidget.Item1.Tags.Has(attachment.Name.ToLowerInvariant()))
                {
                    attachmentWidget.Item1.RenderingEnabled = true;
                    var texture = Texture.Create(1, 1);
                    byte[] data = new byte[4];
                    data[0] = (byte)(attachment.Color.r * 255);
                    data[1] = (byte)(attachment.Color.g * 255);
                    data[2] = (byte)(attachment.Color.b * 255);
                    data[3] = (byte)(attachment.Color.a * 255);
                    texture.WithData(data);
                    attachmentWidget.Item2.Set("ColorMix", texture.Finish());
                    // attachmentWidget.SetMaterialOverride(AttachmentMaterials[index]);
                    break;
                }
                else
                {
                    attachmentWidget.Item1.RenderingEnabled = false;
                }
            }
            index++;
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