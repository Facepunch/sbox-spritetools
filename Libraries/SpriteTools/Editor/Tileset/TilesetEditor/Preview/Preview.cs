using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools.TilesetEditor.Preview;

public class Preview : Widget
{
    public MainWindow MainWindow { get; }
    private readonly RenderingWidget Rendering;

    internal Vector2 TextureSize => Rendering.TextureSize;

    Widget Overlay;
    WidgetWindow overlayWindowZoom;

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
        overlayWindowZoom = new WidgetWindow(this);
        overlayWindowZoom.Parent = Overlay;
        overlayWindowZoom.Layout = Layout.Row();
        overlayWindowZoom.Layout.Spacing = 4;
        overlayWindowZoom.Layout.Margin = 4;
        var btnZoomOut = overlayWindowZoom.Layout.Add(new IconButton("zoom_out"));
        btnZoomOut.OnClick = () =>
        {
            Rendering.Zoom(-250);
        };
        btnZoomOut.ToolTip = "Zoom Out";
        btnZoomOut.StatusTip = "Zoom Out View";
        var btnZoomIn = overlayWindowZoom.Layout.Add(new IconButton("zoom_in"));
        btnZoomIn.OnClick = () =>
        {
            Rendering.Zoom(250);
        };
        btnZoomIn.ToolTip = "Zoom In";
        btnZoomIn.StatusTip = "Zoom In View";
        overlayWindowZoom.Layout.AddSeparator();
        var btnFit = overlayWindowZoom.Layout.Add(new IconButton("zoom_out_map"));
        btnFit.OnClick = () =>
        {
            Rendering.Fit();
        };
        btnFit.ToolTip = "Fit to Screen";
        btnFit.StatusTip = "Fit View to Screen";
        overlayWindowZoom.WindowTitle = "Zoom Controls";

        Overlay.Layout.Add(overlayWindowZoom);

        Overlay.Show();
        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.Moved += DoLayout;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.Moved -= DoLayout;
    }

    internal void UpdateTexture(string filePath)
    {
        var texture = Texture.Load(Sandbox.FileSystem.Mounted, filePath);
        if (texture is null) return;
        Rendering.SetTexture(texture);
    }

    protected override void DoLayout()
    {
        base.DoLayout();

        if (Overlay.IsValid() && Rendering.IsValid())
        {
            Overlay.Position = Rendering.ScreenPosition;
            Overlay.Size = Rendering.Size + 1;

            overlayWindowZoom.AdjustSize();
            overlayWindowZoom.AlignToParent(TextFlag.RightTop, 4);
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