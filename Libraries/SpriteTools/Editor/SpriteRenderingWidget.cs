using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Editor;
using Sandbox;

namespace SpriteTools;

public class SpriteRenderingWidget : NativeRenderingWidget
{
    public Action OnDragSelected;

    public SceneWorld World;
    public SceneObject TextureRect;
    SceneObject BackgroundRect;
    public Material PreviewMaterial;
    public Vector2 TextureSize { get; private set; }
    public float AspectRatio { get; private set; }

    protected Gizmo.Instance SceneInstance;

    float targetZoom = 115f;
    Vector2 cameraGrabPos = Vector2.Zero;
    bool cameraGrabbing = false;

    protected virtual bool CanZoom => true;

    public SpriteRenderingWidget(Widget parent) : base(parent)
    {
        MouseTracking = true;
        FocusMode = FocusMode.Click;

        SceneInstance = new Gizmo.Instance();
        World = SceneInstance.World;
        Camera = new SceneCamera
        {
            World = World,
            AmbientLightColor = Color.White * 1f,
            ZNear = 1f,
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
        BackgroundRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        BackgroundRect.SetMaterialOverride(backgroundMat);
        BackgroundRect.Position = new Vector3(0, 0, -1);

        PreviewMaterial = Material.Load("materials/sprite_2d.vmat").CreateCopy();
        PreviewMaterial.Set("Texture", Color.Transparent);
        PreviewMaterial.Set("g_flFlashAmount", 0f);
        TextureRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        TextureRect.SetMaterialOverride(PreviewMaterial);
        TextureRect.Flags.WantsFrameBufferCopy = true;
        TextureRect.Flags.IsTranslucent = true;
        TextureRect.Flags.IsOpaque = false;
        TextureRect.Flags.CastShadows = false;
    }

    protected override void OnWheel(WheelEvent e)
    {
        base.OnWheel(e);

        if (CanZoom)
            Zoom(e.Delta);
    }

    protected override void OnMousePress(MouseEvent e)
    {
        base.OnMousePress(e);

        if (e.MiddleMouseButton)
        {
            cameraGrabbing = true;
            cameraGrabPos = e.LocalPosition;
        }
    }

    protected override void OnMouseMove(MouseEvent e)
    {
        base.OnMouseMove(e);

        if (cameraGrabbing)
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
    }

    public override void PreFrame()
    {
        Camera.OrthoHeight = Camera.OrthoHeight.LerpTo(targetZoom, 0.1f);
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

    public void SetTexture(Texture texture, Rect rect = default)
    {
        if (rect == default)
        {
            rect = new Rect(0, 0, texture.Width, texture.Height);
        }
        PreviewMaterial.Set("Texture", texture);
        TextureSize = new Vector2(texture.Width, texture.Height);
        TextureRect.SetMaterialOverride(PreviewMaterial);

        var tiling = rect.Size / TextureSize;
        var offset = rect.Position / TextureSize;
        PreviewMaterial.Set("g_vTiling", tiling);
        PreviewMaterial.Set("g_vOffset", offset);

        TextureSize = rect.Size;
        ResizeQuads();
    }

    void ResizeQuads()
    {
        if (!BackgroundRect.IsValid() || !TextureRect.IsValid())
            return;

        // Scale the quad to be the same aspect ratio as the texture
        AspectRatio = TextureSize.x / TextureSize.y;
        var size = new Vector3(1f / AspectRatio, 1f, 1f);
        if (AspectRatio < 1f)
            size = new Vector3(1f, AspectRatio, 1f);

        BackgroundRect.Transform = Transform.Zero.WithScale(size).WithPosition(new Vector3(0, 0, -1));
        TextureRect.Transform = Transform.Zero.WithScale(size);
    }
}