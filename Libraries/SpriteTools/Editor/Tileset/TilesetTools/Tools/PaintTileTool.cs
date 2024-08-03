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

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
        Parent._sceneObject.RenderingEnabled = true;

        var tilePos = (Vector2Int)(pos / Parent.SelectedLayer.TilesetResource.TileSize);
        if (Gizmo.IsLeftMouseDown)
        {
            Parent.PlaceTile(tilePos, TilesetTool.Active.SelectedTile.Position);
        }
    }

    [Shortcut("tileset-tools.paint-tool", "b", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(PaintTileTool));
    }
}