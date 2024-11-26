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

    [Group("Paint Tool"), Property, Range(1, 12, 1)]
    public int BrushSize
    {
        get => _brushSize;
        set
        {
            _brushSize = value;
            lastTilePos = -999999;
        }
    }
    private int _brushSize = 1;

    [Group("Paint Tool"), Property]
    public bool IsRound
    {
        get => _isRound;
        set
        {
            _isRound = value;
            lastTilePos = -999999;
        }
    }
    private bool _isRound = false;

    Vector2Int lastTilePos;
    bool isPainting = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;
        if (Parent.SelectedComponent.Transform is null) return;

        var pos = GetGizmoPos();
        var tile = TilesetTool.Active.SelectedTile;
        var tilePos = (Vector2Int)((pos - Parent.SelectedComponent.WorldPosition) / Parent.SelectedLayer.TilesetResource.GetTileSize());

        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        if (tilePos != lastTilePos)
        {
            UpdateTilePositions();
        }

        if (Gizmo.IsLeftMouseDown)
        {
            var brush = AutotileBrush;
            if (brush is not null)
            {
                Place(tilePos, true);
            }
            else if (tile.Size.x > 1 || tile.Size.y > 1)
            {
                for (int x = 0; x < tile.Size.x; x++)
                {
                    var ux = x;
                    var xx = x;
                    if (Parent.Settings.HorizontalFlip) ux = tile.Size.x - x - 1;
                    for (int y = 0; y < tile.Size.y; y++)
                    {
                        var uy = y;
                        var yy = -y;
                        var offsetPos = new Vector2Int(xx, yy);

                        if (Parent.Settings.Angle == 90)
                            offsetPos = new Vector2Int(-offsetPos.y, offsetPos.x);
                        else if (Parent.Settings.Angle == 180)
                            offsetPos = new Vector2Int(-offsetPos.x, -offsetPos.y);
                        else if (Parent.Settings.Angle == 270)
                            offsetPos = new Vector2Int(offsetPos.y, -offsetPos.x);

                        Parent.PlaceTile(tilePos + offsetPos, tile.Id, new Vector2Int(ux, uy), false);
                    }
                }
                Parent.SelectedComponent.IsDirty = true;
            }
            else
            {
                Place(tilePos);
            }
            isPainting = true;
        }
        else if (isPainting)
        {
            SceneEditorSession.Active.FullUndoSnapshot($"Paint Tiles");
            isPainting = false;
        }

        // if (Parent?.SelectedLayer?.AutoTilePositions is not null)
        // {
        //     var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
        //     using (Gizmo.Scope("test", Transform.Zero))
        //     {
        //         Gizmo.Draw.Color = Color.Red.WithAlpha(0.1f);
        //         foreach (var group in Parent.SelectedLayer.AutoTilePositions)
        //         {
        //             var brush = group.Key;
        //             foreach (var position in group.Value)
        //             {
        //                 Gizmo.Draw.WorldText(Parent.SelectedLayer.GetAutotileBitmask(brush, position).ToString(),
        //                     new Transform(
        //                         Parent.SelectedComponent.WorldPosition + (Vector3)((Vector2)position * tileSize) + (Vector3)(tileSize * 0.5f) + Vector3.Up * 200,
        //                         Rotation.Identity,
        //                         0.3f
        //                     ),
        //                     "Poppins", 24
        //                 );
        //             }
        //         }
        //     }
        // }
    }

    void UpdateTilePositions()
    {
        var pos = GetGizmoPos();
        var brush = AutotileBrush;
        var tile = TilesetTool.Active.SelectedTile;
        var tilePos = (Vector2Int)((pos - Parent.SelectedComponent.WorldPosition) / Parent.SelectedLayer.TilesetResource.GetTileSize());

        List<(Vector2Int, Vector2Int)> positions = new();
        if (brush is null && (tile.Size.x > 1 || tile.Size.y > 1))
        {
            for (int i = 0; i < tile.Size.x; i++)
            {
                for (int j = 0; j < tile.Size.y; j++)
                {
                    positions.Add((new Vector2Int(i, -j), tile.Position + new Vector2Int(i, j)));
                }
            }
        }
        else if (IsRound)
        {
            var size = (BrushSize - 0.9f) * 2;
            var center = new Vector2Int((int)(size / 2f), (int)(size / 2f));
            for (int i = 0; i < size * 2; i++)
            {
                for (int j = 0; j < size * 2; j++)
                {
                    var offset = new Vector2Int(i, j) - center;
                    if (offset.LengthSquared <= (size / 2) * (size / 2))
                    {
                        positions.Add((offset, tile.Position));
                    }
                }
            }
        }
        else
        {
            Vector2Int startPos = new Vector2Int(-BrushSize / 2, -BrushSize / 2);
            for (int i = 0; i < BrushSize; i++)
            {
                for (int j = 0; j < BrushSize; j++)
                {
                    positions.Add((new Vector2Int(i, j) + startPos, tile.Position));
                }
            }
        }

        // Set autobrush tiles if necessary
        if (brush is not null)
        {
            var overrides = new Dictionary<Vector2Int, bool>();
            var allPositions = new List<Vector2Int>();
            foreach (var scenePos in positions)
            {
                var setPos = tilePos + scenePos.Item1;
                overrides.Add(setPos, true);
                allPositions.Add(setPos);
            }
            foreach (var existingTilePos in Parent.SelectedLayer.Tiles.Keys)
            {
                allPositions.Add(existingTilePos);
            }
            var positionCount = positions.Count;
            for (int i = 0; i < positionCount; i++)
            {
                var scenePos = positions[i];
                var realPos = tilePos + scenePos.Item1;
                var bitmask = Parent.SelectedLayer.GetAutotileBitmask(brush.Id, realPos, overrides);
                var maskTile = brush.GetTileFromBitmask(bitmask);
                if (maskTile is not null)
                {
                    var mappedTile = Parent.SelectedLayer.TilesetResource.TileMap[maskTile.Id];
                    scenePos.Item2 = mappedTile.Position;
                    positions[i] = scenePos;
                }

                for (int xx = -1; xx <= 1; xx++)
                {
                    for (int yy = -1; yy <= 1; yy++)
                    {
                        var checkPos = realPos + new Vector2Int(xx, yy);
                        if ((xx != 0 || yy != 0) && allPositions.Contains(checkPos) && !overrides.ContainsKey(checkPos))
                        {
                            AddAutotilePosition(ref positions, overrides, checkPos, tilePos);
                            allPositions.Remove(checkPos);
                        }
                    }
                }
            }
        }

        Parent._sceneObject.SetPositions(positions);
        lastTilePos = tilePos;
    }

    void AddAutotilePosition(ref List<(Vector2Int, Vector2Int)> list, Dictionary<Vector2Int, bool> overrides, Vector2Int pos, Vector2Int tilePos)
    {
        var bitmask = Parent.SelectedLayer.GetAutotileBitmask(AutotileBrush.Id, pos, overrides);
        var maskTile = AutotileBrush.GetTileFromBitmask(bitmask);
        if (maskTile is not null)
        {
            var mappedTile = Parent.SelectedLayer.TilesetResource.TileMap[maskTile.Id];
            list.Add((pos - tilePos, mappedTile.Position));
        }
    }

    void Place(Vector2Int tilePos, bool isAutotile = false)
    {
        var brush = AutotileBrush;
        var tile = TilesetTool.Active.SelectedTile;


        foreach (var position in Parent._sceneObject.MultiTilePositions)
        {
            if (brush is null)
            {
                Parent.PlaceTile(tilePos + position.Item1, tile.Id, position.Item2);
            }
            else
            {
                Parent.PlaceAutotile(brush, tilePos + position.Item1);
            }
        }

        return;
        if (IsRound)
        {
            var size = (BrushSize - 0.9f) * 2;
            var center = new Vector2Int((int)(size / 2f), (int)(size / 2f));
            for (int i = 0; i < size * 2; i++)
            {
                for (int j = 0; j < size * 2; j++)
                {
                    var offset = new Vector2Int(i, j) - center;
                    if (offset.LengthSquared <= (size / 2) * (size / 2))
                    {
                        if (brush is null)
                        {
                            Parent.PlaceTile(tilePos + offset, tile.Id, Vector2Int.Zero);
                        }
                        else
                        {
                            Parent.PlaceAutotile(brush, tilePos + offset);
                        }
                    }
                }
            }
        }
        else
        {
            Vector2Int startPos = new Vector2Int(-BrushSize / 2, -BrushSize / 2);
            for (int i = 0; i < BrushSize; i++)
            {
                for (int j = 0; j < BrushSize; j++)
                {
                    var offset = new Vector2Int(i, j) + startPos;
                    if (brush is null)
                    {
                        Parent.PlaceTile(tilePos + offset, tile.Id, Vector2Int.Zero);
                    }
                    else
                    {
                        Parent.PlaceAutotile(brush, tilePos + offset);
                    }
                }
            }
        }
    }

    [Shortcut("tileset-tools.paint-tool", "b", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(PaintTileTool));
    }
}