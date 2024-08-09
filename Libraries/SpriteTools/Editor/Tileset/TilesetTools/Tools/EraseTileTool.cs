using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

/// <summary>
/// Used to erase tiles from the selected layer.
/// </summary>
[Title("Erase")]
[Icon("delete")]
[Alias("tileset-tools.erase-tool")]
[Group("2")]
[Order(1)]
public class EraserTileTool : BaseTileTool
{
    public EraserTileTool(TilesetTool parent) : base(parent) { }

    bool isErasing = false;

    public override void OnUpdate()
    {
        if (!CanUseTool()) return;

        var pos = GetGizmoPos();
        Parent._sceneObject.RenderingEnabled = false;

        var tileSize = Parent.SelectedLayer.TilesetResource.GetTileSize();
        var tilePos = pos / tileSize;
        if (Gizmo.IsLeftMouseDown)
        {
            Parent.EraseTile(tilePos);
            isErasing = true;
        }
        else if (isErasing)
        {
            SceneEditorSession.Active.FullUndoSnapshot($"Erase Tiles");
            isErasing = false;
        }

        using (Gizmo.Scope("eraser"))
        {
            Gizmo.Draw.Color = Color.Red.WithAlpha(0.5f);
            Gizmo.Draw.SolidBox(new BBox(pos, pos + new Vector3(tileSize.x, tileSize.y, 0)));
        }
    }

    [Shortcut("tileset-tools.erase-tool", "e", typeof(SceneViewportWidget))]
    public static void ActivateSubTool()
    {
        if (EditorToolManager.CurrentModeName != nameof(TilesetTool)) return;
        EditorToolManager.SetSubTool(nameof(EraserTileTool));
    }
}