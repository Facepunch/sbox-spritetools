using System;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetEditor;

public class AutotileTileControl : Widget
{
    internal AutotileBrushControl ParentBrush;
    internal AutotileBrush.Tile Tile;

    int Index;

    public AutotileTileControl(AutotileBrushControl brush, int index)
    {
        Index = index;
        ParentBrush = brush;
        Tile = ParentBrush.Brush.Tiles[index];

        VerticalSizeMode = SizeMode.Flexible;

        StatusTip = $"Select Tile";
        Cursor = CursorShape.Finger;

        Layout = Layout.Row();
        Layout.AddSpacingCell(20);
        Layout.Margin = 4;
        Layout.Spacing = 4;

        FixedSize = 26;

        IsDraggable = true;
        AcceptDrops = true;
    }

    protected override void OnPaint()
    {
        Texture tex = null;
        if ((Tile?.Tiles?.Count ?? 0) > 0)
        {
            if (ParentBrush.ParentList.MainWindow.Tileset.TileTextures.TryGetValue(Tile?.Tiles[0]?.Id ?? Guid.Empty, out var texture))
            {
                tex = texture;
            }
        }
        if (tex is null)
        {
            var tileCount = ParentBrush.Brush.Is47Tiles ? 47 : 16;
            tex = Texture.Load(Editor.FileSystem.Mounted, $"images/guides/tile-guide-{tileCount}-{Index}.png");
        }
        var pixmap = Pixmap.FromTexture(tex);
        Paint.Draw(LocalRect, pixmap);

        var color = IsUnderMouse ? Color.White : Color.Transparent;
        if (ParentBrush.ParentList.SelectedTile == Tile)
        {
            color = Theme.Blue;
        }
        Paint.SetBrushAndPen(color.WithAlpha(MathF.Min(0.5f, color.a)), Color.Transparent);
        Paint.DrawRect(LocalRect);
    }

    protected override void OnMouseClick(MouseEvent e)
    {
        base.OnMouseClick(e);

        ParentBrush?.ParentList?.SelectTile(this);

        e.Accepted = true;
    }

    protected override void OnContextMenu(ContextMenuEvent e)
    {
        base.OnContextMenu(e);

        var m = new Menu(this);

        m.AddOption("Clear", "clear", Clear);

        m.OpenAtCursor(false);

        e.Accepted = true;
    }

    void Clear()
    {
        Tile.Tiles?.Clear();
    }
}