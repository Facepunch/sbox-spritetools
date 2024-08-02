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

    public override void OnUpdate()
    {
        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tilePos = pos / Parent.SelectedLayer.TilesetResource.TileSize;

        if (holding)
        {
            var positions = new List<Vector2>();

            var min = Vector2.Min(startPos, tilePos);
            var max = Vector2.Max(startPos, tilePos);
            for (int x = (int)min.x; x <= (int)max.x; x++)
            {
                for (int y = (int)min.y; y <= (int)max.y; y++)
                {
                    if (Hollow && (x != min.x && x != max.x && y != min.y && y != max.y)) continue;
                    positions.Add(new Vector2(x, y) - tilePos);
                }
            }
            Parent._sceneObject.SetPositions(positions);

            if (!Gizmo.IsLeftMouseDown)
            {
                holding = false;
                Parent._sceneObject.ClearPositions();

                foreach (var ppos in positions)
                {
                    Parent.PlaceTile(tilePos + ppos);
                }
            }
        }
        else if (Gizmo.IsLeftMouseDown)
        {
            startPos = tilePos;
            holding = true;
        }

    }

    [Shortcut("tileset-tools.rectangle-tool", "r", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(RectangleTileTool));
    }
}