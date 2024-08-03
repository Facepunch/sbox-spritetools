using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools.TilesetTool.Preview;

public class Preview : Widget
{
    public TilesetToolInspector Inspector { get; }
    private readonly RenderingWidget Rendering;

    internal Vector2 TextureSize => Rendering.TextureSize;

    public Preview(TilesetToolInspector inspector) : base(null)
    {
        Inspector = inspector;

        Name = "Preview";
        WindowTitle = "Preview";
        SetWindowIcon("emoji_emotions");

        MinimumSize = new Vector2(256, 512);
        HorizontalSizeMode = SizeMode.Flexible;
        VerticalSizeMode = SizeMode.CanGrow;

        Layout = Layout.Column();

        Rendering = new RenderingWidget(Inspector, this);
        Layout.Add(Rendering);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        var tileset = TilesetTool.Active?.SelectedLayer?.TilesetResource;
        if (tileset is not null && !string.IsNullOrEmpty(tileset.FilePath))
        {
            UpdateTexture(tileset.FilePath);
        }

        Inspector.Moved += DoLayout;
    }

    protected override void OnPaint()
    {
        base.OnPaint();

        Paint.SetBrush(Theme.Blue);
        Paint.DrawRect(LocalRect);
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        Inspector.Moved -= DoLayout;
    }

    internal void UpdateTexture(string filePath)
    {
        var texture = Texture.Load(Sandbox.FileSystem.Mounted, filePath);
        if (texture is null) return;
        Rendering.SetTexture(texture);
    }
}