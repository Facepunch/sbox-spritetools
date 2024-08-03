using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool.Tools;

public abstract class BaseTileTool : EditorTool
{
    protected TilesetTool Parent;

    public BaseTileTool(TilesetTool parent)
    {
        Parent = parent;
    }

    public override void OnEnabled()
    {
        base.OnEnabled();

        TilesetToolInspector.Active.UpdateMainSheet();
    }

    public override void OnUpdate()
    {
        var pos = GetGizmoPos();
        Parent._sceneObject.Transform = new Transform(pos, Rotation.Identity, 1);
    }

    protected Vector3 GetGizmoPos()
    {
        if (Parent.SelectedComponent.Layers.Count == 0) return Vector3.Zero;

        var tr = SceneEditorSession.Active.Scene.Trace
            .Ray(Gizmo.CurrentRay, 500000)
            .Run();

        var tileSize = Parent.SelectedLayer.TilesetResource.TileSize;
        var layerIndex = (Parent.SelectedComponent.Layers.Count - 1) - Parent.SelectedComponent.Layers.IndexOf(Parent.SelectedLayer);
        var pos = (tr.EndPosition - new Vector3(tileSize.x / 2f, tileSize.y / 2f, 0))
                    .SnapToGrid(tileSize.x, true, false, false)
                    .SnapToGrid(tileSize.y, false, true, false)
                    .WithZ(layerIndex + 0.5f);

        return pos;
    }
}