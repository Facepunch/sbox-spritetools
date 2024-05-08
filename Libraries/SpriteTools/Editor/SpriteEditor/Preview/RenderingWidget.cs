using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class RenderingWidget : NativeRenderingWidget
{
    MainWindow MainWindow;

    private SceneWorld World;
    public SceneObject TextureRect;
    public Material PreviewMaterial;

    float targetZoom = 115f;
    Vector2 cameraGrabPos = Vector2.Zero;
    bool cameraGrabbing = false;

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
    }

    protected override void OnWheel(WheelEvent e)
    {
        base.OnWheel(e);

        targetZoom *= 1f - (e.Delta / 500f);
        targetZoom = targetZoom.Clamp(1, 1000);
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

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);

        if (e.Key == KeyCode.Space)
        {
            MainWindow?.PlayPause();
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
}