using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

public abstract class BaseTileTool : EditorTool
{
    protected TilesetTool Parent;

    protected bool ShouldMergeAutotiles = true;

    protected AutotileBrush AutotileBrush
    {
        get
        {
            if (AutotileWidget.Instance is null) return null;
            return AutotileWidget.Instance.Brush;
        }
    }

    public BaseTileTool(TilesetTool parent)
    {
        Parent = parent;
    }

    public override void OnEnabled()
    {
        base.OnEnabled();

        TilesetToolInspector.Active?.UpdateMainSheet();
    }

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
    }

    protected Vector3 GetGizmoPos()
    {
        if (Parent.SelectedComponent.Layers.Count == 0) return Vector3.Zero;
        if (Parent.SelectedComponent.Transform is null) return Vector3.Zero;

        var tr = SceneEditorSession.Active.Scene.Trace
            .Ray(Gizmo.CurrentRay, 50000)
            .Run();

        var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
        var center = Parent.SelectedComponent.WorldPosition;
        var offset = new Vector3(
            center.x % tileSize.x,
            center.y % tileSize.y,
            center.z
        );

        var layerIndex = (Parent.SelectedComponent.Layers.Count - 1) - Parent.SelectedComponent.Layers.IndexOf(Parent.SelectedLayer);
        var pos = (tr.EndPosition - new Vector3(tileSize.x / 2f, tileSize.y / 2f, 0) - offset)
                    .SnapToGrid(tileSize.x, true, false, false)
                    .SnapToGrid(tileSize.y, false, true, false)
                    .WithZ(layerIndex + 0.5f);

        return pos + offset;
    }

    protected void UpdateTilePositions(List<Vector2> positions)
    {
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
            if (!ShouldMergeAutotiles && !Parent.SelectedLayer.AutoTilePositions[brush.Id].Contains(existingTilePos))
                continue;
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

    protected void AddAutotilePosition(ref List<(Vector2Int, Vector2Int)> list, Dictionary<Vector2Int, bool> overrides, Vector2Int pos, Vector2Int tilePos)
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
    protected bool CanUseTool()
    {
        if (Parent?.SelectedLayer?.TilesetResource is null) return false;
        if (TilesetTool.Active?.SelectedTile is null)
        {
            if (Parent.SelectedLayer.TilesetResource.Tiles.Count > 0)
            {
                Parent.SelectedTile = Parent.SelectedLayer.TilesetResource.Tiles.FirstOrDefault();
                Parent._sceneObject.UpdateTileset(Parent.SelectedLayer.TilesetResource);
            }
            else
                return false;
        }

        return true;
    }
}