using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to paint tiles on the selected layer.
/// </summary>
[Title("Paint")]
[Icon("brush")]
[Alias("tileset-tools.paint-tool")]
[Group("1")]
[Order(0)]
public class PaintTileTool : BaseTileTool
{
    public PaintTileTool(TilesetTool parent) : base(parent) { }

    bool isPainting = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        List<(Vector2Int, Vector2Int)> positions = new();
        for (int i = 0; i < Parent.SelectedTile.Size.x; i++)
        {
            for (int j = 0; j < Parent.SelectedTile.Size.y; j++)
            {
                positions.Add((new Vector2Int(i, -j), Parent.SelectedTile.Position + new Vector2Int(i, j)));
            }
        }
        Parent._sceneObject.SetPositions(positions);

        var tilePos = (Vector2Int)(pos / Parent.SelectedLayer.TilesetResource.GetTileSize());
        if (Gizmo.IsLeftMouseDown)
        {
            var tile = TilesetTool.Active.SelectedTile;
            if (tile.Size.x > 1 || tile.Size.y > 1)
            {
                for (int x = 0; x < tile.Size.x; x++)
                {
                    var ux = x;
                    var xx = x;
                    if (Parent.HorizontalFlip) ux = tile.Size.x - x - 1;
                    for (int y = 0; y < tile.Size.y; y++)
                    {
                        var uy = y;
                        var yy = -y;
                        var offsetPos = new Vector2Int(xx, yy);

                        if (Parent.Angle == 90)
                            offsetPos = new Vector2Int(-offsetPos.y, offsetPos.x);
                        else if (Parent.Angle == 180)
                            offsetPos = new Vector2Int(-offsetPos.x, -offsetPos.y);
                        else if (Parent.Angle == 270)
                            offsetPos = new Vector2Int(offsetPos.y, -offsetPos.x);

                        Parent.PlaceTile(tilePos + offsetPos, tile.Id, new Vector2Int(ux, uy), false);
                    }
                }
                Parent.SelectedComponent.IsDirty = true;
            }
            else
            {
                Parent.PlaceTile(tilePos, tile.Id, Vector2Int.Zero);
            }
            isPainting = true;
        }
        else if (isPainting)
        {
            SceneEditorSession.Active.FullUndoSnapshot($"Paint Tiles");
            isPainting = false;
        }
    }

    [Shortcut("tileset-tools.paint-tool", "b", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(PaintTileTool));
    }
}