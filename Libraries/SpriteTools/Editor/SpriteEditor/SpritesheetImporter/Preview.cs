using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class Preview : Widget
{
    RenderingWidget Rendering;
    Widget Overlay;
    WidgetWindow overlayWindowZoom;

    public Preview(SpritesheetImporter parent) : base(parent)
    {
        Name = "Preview";
        WindowTitle = "Preview";
        SetWindowIcon("emoji_emotions");

        Layout = Layout.Column();

        Rendering = new RenderingWidget(this);
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
        Layout.Add(Overlay);

        var texture = Texture.Load(Sandbox.FileSystem.Mounted, parent.Path);
        if (texture is not null)
        {
            Rendering.PreviewMaterial.Set("Texture", texture);
        }
    }

    protected override void OnPaint()
    {
        base.OnPaint();
    }

    protected override void DoLayout()
    {
        base.DoLayout();
    }
}
