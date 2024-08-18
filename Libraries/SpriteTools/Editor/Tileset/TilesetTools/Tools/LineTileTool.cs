using System;
using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to paint tiles on the selected layer.
/// </summary>
[Title("Line")]
[Icon("horizontal_rule")]
[Alias("tileset-tools.line-tool")]
[Group("1")]
[Order(0)]
public class LineTileTool : BaseTileTool
{
    public LineTileTool(TilesetTool parent) : base(parent) { }

    [Property, Range(0, 8, 1)] public int Separation { get; set; } = 0;

    Vector2 startPos;
    bool holding = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tilePos = pos / Parent.SelectedLayer.TilesetResource.GetTileSize();

        if (holding)
        {
            var positions = new List<Vector2>() { startPos - tilePos };

            float sep = 1f / (Separation + 1f);
            var dx = tilePos.x - startPos.x;
            var dy = tilePos.y - startPos.y;
            dx *= sep;
            dy *= sep;
            var delta = Math.Max(Math.Abs(dx), Math.Abs(dy));
            for (int i = 0; i <= delta; i++)
            {
                var x = startPos.x + i * dx / delta / sep;
                var y = startPos.y + i * dy / delta / sep;
                x = (int)Math.Round(x);
                y = (int)Math.Round(y);
                var thisPos = new Vector2(x, y) - tilePos;
                if (!positions.Contains(thisPos))
                    positions.Add(thisPos);
            }
            Parent._sceneObject.SetPositions(positions);

            if (!Gizmo.IsLeftMouseDown)
            {
                holding = false;
                Parent._sceneObject.ClearPositions();

                var tile = TilesetTool.Active.SelectedTile;
                foreach (var ppos in positions)
                {
                    Parent.PlaceTile((Vector2Int)(tilePos + ppos), tile.Id, Vector2Int.Zero, false);
                }
                Parent.SelectedComponent.IsDirty = true;
                SceneEditorSession.Active.FullUndoSnapshot($"Paint Tile Line");
            }
        }
        else if (Gizmo.IsLeftMouseDown)
        {
            startPos = tilePos;
            holding = true;
        }

    }

    [Shortcut("tileset-tools.line-tool", "l", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(LineTileTool));
    }
}