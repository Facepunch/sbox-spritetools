using System;
using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to paint tiles on the selected layer.
/// </summary>
[Title("Rectangle")]
[Icon("crop_square")]
[Alias("tileset-tools.rectangle-tool")]
[Group("1")]
[Order(0)]
public class RectangleTileTool : BaseTileTool
{
    public RectangleTileTool(TilesetTool parent) : base(parent) { }

    [Property] public bool Hollow { get; set; } = false;

    Vector2 startPos;
    bool holding = false;
    bool deleting = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
        var tilePos = pos / tileSize;

        if (holding)
        {
            var positions = new List<Vector2>();

            var min = Vector2.Min(startPos - tilePos, 0);
            var max = Vector2.Max(startPos - tilePos, 0);

            if (deleting)
            {
                Parent._sceneObject.RenderingEnabled = false;
                using (Gizmo.Scope("delete"))
                {
                    Gizmo.Draw.Color = Color.Red.WithAlpha(0.5f);
                    Gizmo.Draw.SolidBox(new BBox(tilePos * tileSize + min * tileSize, tilePos * tileSize + max * tileSize + tileSize));
                }
            }
            else
            {
                positions = GetPositions(min, max);
                Parent._sceneObject.RenderingEnabled = true;
                Parent._sceneObject.SetPositions(positions);
            }

            if (!Gizmo.IsLeftMouseDown && !Gizmo.IsRightMouseDown)
            {
                holding = false;
                Parent._sceneObject.ClearPositions();

                if (deleting)
                {
                    positions = GetPositions(min, max);
                    foreach (var ppos in positions)
                    {
                        Parent.EraseTile(tilePos + ppos, false);
                    }
                    SceneEditorSession.Active.FullUndoSnapshot($"Erase Tile Rectangle");
                    Parent.SelectedComponent.IsDirty = true;
                }
                else
                {
                    var tile = TilesetTool.Active.SelectedTile;
                    foreach (var ppos in positions)
                    {
                        Parent.PlaceTile((Vector2Int)(tilePos + ppos), tile.Id, Vector2Int.Zero, false);
                    }
                    SceneEditorSession.Active.FullUndoSnapshot($"Place Tile Rectangle");
                    Parent.SelectedComponent.IsDirty = true;
                }
            }
        }
        else if (Gizmo.IsLeftMouseDown || Gizmo.IsRightMouseDown)
        {
            startPos = tilePos;
            holding = true;
            deleting = Gizmo.IsRightMouseDown;
        }
    }

    List<Vector2> GetPositions(Vector2 min, Vector2 max)
    {
        var positions = new List<Vector2>();

        if (min == max)
        {
            positions.Add(min);
            return positions;
        }

        if (Vector2.Distance(min, max) < 2_500)
        {
            for (int x = (int)min.x; x <= (int)max.x; x++)
            {
                for (int y = (int)min.y; y <= (int)max.y; y++)
                {
                    if (Hollow && (x != min.x && x != max.x && y != min.y && y != max.y)) continue;
                    positions.Add(new Vector2(x, y));
                }
            }
        }

        return positions;
    }

    [Shortcut("tileset-tools.rectangle-tool", "r", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(RectangleTileTool));
    }
}