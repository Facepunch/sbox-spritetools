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
                using (Gizmo.Scope("delete", new Transform(Parent.SelectedComponent.WorldPosition)))
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
                    SceneEditorSession.Active.FullUndoSnapshot($"Erase Tile Rectangle");
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
                            Parent.PlaceAutotile(brush, (Vector2Int)(tilePos + ppos));
                        }
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
        else
        {
            if (tilePos != lastTilePos)
            {
                UpdateTilePositions(new List<Vector2> { 0 });
                lastTilePos = tilePos;
            }
        }
    }

    void UpdateTilePositions(List<Vector2> positions)
    {
        Log.Info(positions);
        var brush = AutotileBrush;
        if (brush is null)
        {
            Parent._sceneObject.SetPositions(positions);
            return;
        }

        var pos = GetGizmoPos();
        var tilePos = (Vector2Int)((pos - Parent.SelectedComponent.WorldPosition) / Parent.SelectedLayer.TilesetResource.GetTileSize());

        var tilePositions = new List<(Vector2Int, Vector2Int)>();
        var overrides = new Dictionary<Vector2Int, bool>();
        var allPositions = new List<Vector2Int>();
        foreach (var scenePos in positions)
        {
            var setPos = (Vector2Int)(tilePos + scenePos);
            tilePositions.Add(((Vector2Int)scenePos, -1));
            overrides.Add(setPos, true);
            allPositions.Add(setPos);
        }
        foreach (var existingTilePos in Parent.SelectedLayer.Tiles.Keys)
        {
            if (!allPositions.Contains(existingTilePos))
                allPositions.Add(existingTilePos);
        }
        var positionCount = tilePositions.Count;
        for (int i = 0; i < positionCount; i++)
        {
            var scenePos = tilePositions[i];
            var realPos = tilePos + scenePos.Item1;
            var bitmask = Parent.SelectedLayer.GetAutotileBitmask(brush.Id, realPos, overrides);
            var maskTile = brush.GetTileFromBitmask(bitmask);
            if (maskTile is not null)
            {
                var mappedTile = Parent.SelectedLayer.TilesetResource.TileMap[maskTile.Id];
                scenePos.Item2 = mappedTile.Position;
                tilePositions[i] = scenePos;
            }

            for (int xx = -1; xx <= 1; xx++)
            {
                for (int yy = -1; yy <= 1; yy++)
                {
                    var checkPos = realPos + new Vector2Int(xx, yy);
                    if ((xx != 0 || yy != 0) && allPositions.Contains(checkPos) && !overrides.ContainsKey(checkPos))
                    {
                        AddAutotilePosition(ref tilePositions, overrides, checkPos, tilePos);
                        allPositions.Remove(checkPos);
                    }
                }
            }
        }

        Parent._sceneObject.SetPositions(tilePositions);
    }

    void AddAutotilePosition(ref List<(Vector2Int, Vector2Int)> list, Dictionary<Vector2Int, bool> overrides, Vector2Int pos, Vector2Int tilePos)
    {
        var brush = AutotileBrush;
        var bitmask = Parent.SelectedLayer.GetAutotileBitmask(brush.Id, pos, overrides);
        var maskTile = brush.GetTileFromBitmask(bitmask);
        if (maskTile is not null)
        {
            var mappedTile = Parent.SelectedLayer.TilesetResource.TileMap[maskTile.Id];
            list.Add((pos - tilePos, mappedTile.Position));
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