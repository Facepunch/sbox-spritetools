using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to erase tiles from the selected layer.
/// </summary>
[Title("Erase")]
[Icon("delete")]
[Alias("tilesettool.eraser")]
[Group("2")]
[Shortcut("editortool.tileset.eraser", "e")]
[Order(1)]
public class EraserTileTool : BaseTileTool
{
    public EraserTileTool(TilesetTool parent) : base(parent) { }

    public override void OnUpdate()
    {
        var pos = GetGizmoPos();
        Parent._sceneObject.RenderingEnabled = false;

        var tilePos = pos / Parent.SelectedLayer.TilesetResource.TileSize;
        if (Gizmo.IsLeftMouseDown)
        {
            Parent.EraseTile(tilePos);
        }

        using (Gizmo.Scope("eraser"))
        {
            var tileSize = Parent.SelectedLayer.TilesetResource.TileSize;
            Gizmo.Draw.Color = Color.Red.WithAlpha(0.5f);
            Gizmo.Draw.SolidBox(new BBox(pos, pos + new Vector3(tileSize.x, tileSize.y, 0)));
        }
    }
}