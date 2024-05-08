using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class RenderingWidget : NativeRenderingWidget
{
    MainWindow MainWindow;

    private SceneWorld World;
    public SceneObject TextureRect;
    public Vector2 TextureSize;
    Draggable OriginMarker;
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

        PreviewMaterial = Material.Load("materials/spritegraph.vmat");
        PreviewMaterial.Set("Color", Color.Transparent);
        TextureRect = new SceneObject(World, "models/preview_quad.vmdl", Transform.Zero);
        TextureRect.SetMaterialOverride(PreviewMaterial);
        TextureRect.Flags.WantsFrameBufferCopy = true;
        TextureRect.Flags.IsTranslucent = true;
        TextureRect.Flags.IsOpaque = false;

        var markerMaterial = Material.Load("materials/sprite_editor_origin.vmat");
        OriginMarker = new Draggable(World, "models/preview_quad.vmdl", Transform.Zero);
        OriginMarker.SetMaterialOverride(markerMaterial);
        OriginMarker.Tags.Add("origin");
        OriginMarker.Position = new Vector3(0, 0, 1f);
        OriginMarker.Flags.WantsFrameBufferCopy = true;
        OriginMarker.Flags.IsTranslucent = true;
        OriginMarker.Flags.IsOpaque = false;
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
            var delta = (pos - draggableGrabPos).WithZ(0f);
            var dragPos = dragging.Position + delta;
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