using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class Preview : Widget
{
    public MainWindow MainWindow { get; }
    private readonly RenderingWidget Rendering;

    Widget Overlay;
    WidgetWindow overlayWindow;

    public Preview(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Preview";
        WindowTitle = "Preview";
        SetWindowIcon("emoji_emotions");

        MinimumSize = new Vector2(256, 256);

        Layout = Layout.Column();

        Rendering = new RenderingWidget(MainWindow, this);
        Layout.Add(Rendering);

        Overlay = new Widget(this)
        {
            Layout = Layout.Row(),
            TranslucentBackground = true,
            NoSystemBackground = true,
            WindowFlags = WindowFlags.FramelessWindowHint | WindowFlags.Tool
        };
        overlayWindow = new WidgetWindow(this);
        overlayWindow.Parent = Overlay;
        overlayWindow.Layout = Layout.Row();
        overlayWindow.Layout.Spacing = 4;
        overlayWindow.Layout.Margin = 4;
        var btnZoomOut = overlayWindow.Layout.Add(new IconButton("zoom_out"));
        btnZoomOut.OnClick = () =>
        {
            Rendering.Zoom(-250);
        };
        btnZoomOut.ToolTip = "Zoom Out";
        var btnZoomIn = overlayWindow.Layout.Add(new IconButton("zoom_in"));
        btnZoomIn.OnClick = () =>
        {
            Rendering.Zoom(250);
        };
        btnZoomIn.ToolTip = "Zoom In";
        overlayWindow.Layout.AddSeparator();
        var btnFit = overlayWindow.Layout.Add(new IconButton("zoom_out_map"));
        btnFit.OnClick = () =>
        {
            Rendering.Fit();
        };
        btnFit.ToolTip = "Fit to Screen";
        overlayWindow.WindowTitle = "Zoom Controls";

        Overlay.Layout.Add(overlayWindow);
        Overlay.Show();

        UpdateTexture();
        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.OnTextureUpdate += UpdateTexture;
        MainWindow.OnAnimationSelected += UpdateWindowTitle;

        MainWindow.Moved += DoLayout;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= UpdateTexture;
        MainWindow.OnAnimationSelected -= UpdateWindowTitle;
        MainWindow.Moved -= DoLayout;
    }

    void UpdateWindowTitle()
    {
        if (MainWindow.SelectedAnimation is null)
        {
            WindowTitle = "Preview";
            return;
        }
        WindowTitle = $"Preview - {MainWindow.SelectedAnimation.Name}";
    }

    void UpdateTexture()
    {
        if (MainWindow.Sprite is null) return;
        if (string.IsNullOrEmpty(MainWindow.CurrentTexturePath)) return;

        var texture = Texture.Load(Sandbox.FileSystem.Mounted, MainWindow.CurrentTexturePath);
        Rendering.PreviewMaterial.Set("Color", texture);
        Rendering.TextureSize = new Vector2(texture.Width, texture.Height);
        Rendering.TextureRect.SetMaterialOverride(Rendering.PreviewMaterial);
    }

    protected override void DoLayout()
    {
        base.DoLayout();

        if (Overlay.IsValid() && Rendering.IsValid())
        {
            Overlay.Position = Rendering.ScreenPosition;
            Overlay.Size = Rendering.Size + 1;

            overlayWindow.AdjustSize();
            overlayWindow.AlignToParent(TextFlag.RightTop, 4);
        }
    }

    protected override void OnVisibilityChanged(bool visible)
    {
        base.OnVisibilityChanged(visible);

        if (Overlay is not null)
        {
            Overlay.Visible = visible;
        }
    }
}