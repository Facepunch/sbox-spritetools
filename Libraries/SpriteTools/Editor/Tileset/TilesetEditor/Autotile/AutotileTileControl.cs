using Editor;
using Sandbox;

namespace SpriteTools.TilesetEditor;

public class AutotileTileControl : Widget
{
    internal AutotileBrushControl ParentBrush;
    internal AutotileBrush.Tile Tile;

    public AutotileTileControl(AutotileBrushControl brush, AutotileBrush.Tile tile)
    {
        ParentBrush = brush;
        Tile = tile;

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
        var color = IsUnderMouse ? Color.Red : Theme.Grey;
        if (ParentBrush.ParentList.SelectedTile == Tile)
        {
            color = Theme.Blue;
        }
        Paint.SetBrushAndPen(color);
        Paint.DrawRect(LocalRect);
    }

    protected override void OnMouseClick(MouseEvent e)
    {
        base.OnMouseClick(e);

        ParentBrush?.ParentList?.SelectTile(this);
    }
}