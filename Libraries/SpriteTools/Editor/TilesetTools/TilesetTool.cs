using System;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool;

[EditorTool]
[Title("Tileset Tool")]
[Description("Paint 2D tiles from a tileset")]
[Icon("dashboard")]
[Group("7")]
[Shortcut("editortool.tileset", "Shift+T")]
public partial class TilesetTool : EditorTool
{
    public static TilesetTool Active { get; private set; }

    public TilesetComponent SelectedComponent;
    public TilesetComponent.Layer SelectedLayer;
    internal Action UpdateInspector;

    bool WasGridActive = true;

    SceneObject _sceneObject;

    public override void OnEnabled()
    {
        Active = this;

        base.OnEnabled();

        AllowGameObjectSelection = false;
        Selection.Clear();
        Selection.Set(this);

        InitGrid();
        UpdateComponent();
    }

    public override void OnDisabled()
    {
        Active = null;

        base.OnDisabled();

        ResetGrid();
    }

    public override void OnUpdate()
    {
        var state = SceneViewportWidget.LastSelected.State;
        using (Gizmo.Scope("grid"))
        {
            Gizmo.Draw.IgnoreDepth = state.Is2D;
            Gizmo.Draw.Grid(state.GridAxis, SelectedLayer?.TilesetResource?.TileSize ?? 32, state.GridOpacity);
        }
    }

    void UpdateComponent()
    {
        var component = Scene.GetAllComponents<TilesetComponent>().FirstOrDefault();

        if (!component.IsValid())
        {
            var go = new GameObject()
            {
                Name = "Tileset Object"
            };
            component = go.Components.GetOrCreate<TilesetComponent>();
        }

        if (component.IsValid())
        {
            SelectedComponent = component;
            SelectedLayer = SelectedComponent.Layers.FirstOrDefault();
        }
    }

    void DoGizmo()
    {
        if (_sceneObject.IsValid())
        {

        }
    }

    void InitGrid()
    {
        WasGridActive = SceneViewportWidget.LastSelected.State.ShowGrid;
        SceneViewportWidget.LastSelected.State.ShowGrid = false;
    }

    void ResetGrid()
    {
        SceneViewportWidget.LastSelected.State.ShowGrid = WasGridActive;
    }

}