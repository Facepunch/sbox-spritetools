using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

[Title("Paint")]
[Icon("brush")]
[Alias("tilesettool.paint")]
[Group("1")]
[Shortcut("editortool.tileset.paint", "b")]
[Order(0)]
public class PaintTileTool : BaseTileTool
{
    public PaintTileTool(TilesetTool parent) : base(parent) { }

    public override void OnUpdate()
    {
        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);

        var tilePos = pos / Parent.SelectedLayer.TilesetResource.TileSize;
        if (Gizmo.IsLeftMouseDown)
        {
            Parent.PlaceTile(tilePos);
        }
    }
}