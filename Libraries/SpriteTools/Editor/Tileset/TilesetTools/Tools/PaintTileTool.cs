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

    [Group("Paint Tool"), Property, Range(1, 12, 1)] public int BrushSize { get; set; } = 1;
    [Group("Paint Tool"), Property] public bool IsRound { get; set; } = false;


    bool isPainting = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;
        if (Parent.SelectedComponent.Transform is null) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tile = TilesetTool.Active.SelectedTile;
        List<(Vector2Int, Vector2Int)> positions = new();
        if (tile.Size.x > 1 || tile.Size.y > 1)
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
        Parent._sceneObject.SetPositions(positions);

        var tilePos = (Vector2Int)((pos - Parent.SelectedComponent.WorldPosition) / Parent.SelectedLayer.TilesetResource.GetTileSize());
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
        //         Gizmo.Draw.Color = Color.Red;
        //         foreach (var group in Parent.SelectedLayer.AutoTilePositions)
        //         {
        //             var brush = group.Key;
        //             foreach (var position in group.Value)
        //             {
        //                 Gizmo.Draw.WorldText(Parent.SelectedLayer.GetAutotileBitmask(brush, position).ToString(),
        //                     new Transform(
        //                         Parent.SelectedComponent.WorldPosition + (Vector3)((Vector2)position * tileSize) + (Vector3)(tileSize * 0.5f),
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

    void Place(Vector2Int tilePos, bool isAutotile = false)
    {
        var brush = AutotileBrush;
        var tile = TilesetTool.Active.SelectedTile;

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