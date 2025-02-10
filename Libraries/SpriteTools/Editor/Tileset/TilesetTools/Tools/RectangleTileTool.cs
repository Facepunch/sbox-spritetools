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

    /// <summary>
    /// If enabled, the rectangle will only draw the border and not be filled.
    /// </summary>
    [Group("Rectangle Tool"), Property] public bool Hollow { get; set; } = false;

    Vector2 startPos;
    Vector2 lastMin;
    Vector2 lastMax;
    Vector2 lastTilePos;
    bool holding = false;
    bool deleting = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
        var tilePos = (pos - Parent.SelectedComponent.WorldPosition) / tileSize;

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
                Parent._sceneObject.SetPositions(new List<Vector2> { Vector2.Zero });
            }
            else
            {
                positions = GetPositions(min, max);
                Parent._sceneObject.RenderingEnabled = true;
                if (tilePos != lastTilePos || min != lastMin || max != lastMax)
                {
                    UpdateTilePositions(positions);
                    lastMin = min;
                    lastMax = max;
                    lastTilePos = tilePos;
                }
            }

            if (!Gizmo.IsLeftMouseDown && !Gizmo.IsRightMouseDown)
            {
                var brush = AutotileBrush;
                holding = false;
                Parent._sceneObject.ClearPositions();

                if (deleting)
                {
                    positions = GetPositions(min, max);
                    foreach (var ppos in positions)
                    {
                        if (brush is null)
                            Parent.EraseTile(tilePos + ppos, false);
                        else
                            Parent.EraseAutoTile(brush, (Vector2Int)(tilePos + ppos));
                    }
                    Parent.SelectedComponent.IsDirty = true;
                }
                else
                {
                    var tile = TilesetTool.Active.SelectedTile;
                    if (brush is null)
                    {
                        foreach (var ppos in positions)
                        {
                            Parent.PlaceTile((Vector2Int)(tilePos + ppos), tile.Id, Vector2Int.Zero, false);
                        }
                    }
                    else
                    {
                        foreach (var ppos in positions)
                        {
                            Parent.PlaceAutotile(brush.Id, (Vector2Int)(tilePos + ppos), false);
                        }

                        foreach (var ppos in positions)
                        {
                            Parent.SelectedLayer.UpdateAutotile(brush.Id, (Vector2Int)(tilePos + ppos), false);
                        }
                    }
                    Parent.SelectedComponent.IsDirty = true;
                }
                _componentUndoScope?.Dispose();
                _componentUndoScope = null;
            }
        }
        else if (Gizmo.IsLeftMouseDown || Gizmo.IsRightMouseDown)
        {
            deleting = Gizmo.IsRightMouseDown;
            if (_componentUndoScope is null)
            {
                _componentUndoScope = SceneEditorSession.Active.UndoScope(deleting ? "Erase Tile Rectangle" : "Paint Tile Rectangle")
                    .WithComponentChanges(Parent.SelectedComponent).Push();
            }
            startPos = tilePos;
            holding = true;
        }
        else
        {
            if (tilePos != lastTilePos)
            {
                UpdateTilePositions(new List<Vector2> { 0 });
                lastTilePos = tilePos;
            }
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