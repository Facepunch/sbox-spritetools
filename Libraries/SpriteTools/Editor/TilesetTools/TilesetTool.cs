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
    [Property] public TilesetResource SelectedTileset { get; set; }

    public TilesetComponent SelectedComponent;

    bool WasGridActive = true;
    int GridSize = 64;

    SceneObject _sceneObject;

    public override void OnEnabled()
    {
        base.OnEnabled();

        AllowGameObjectSelection = false;
        Selection.Clear();
        Selection.Set(this);

        InitGrid();
        UpdateComponent();
    }

    public override void OnDisabled()
    {
        base.OnDisabled();

        ResetGrid();
    }

    public override void OnUpdate()
    {
        var state = SceneViewportWidget.LastSelected.State;
        using (Gizmo.Scope("grid"))
        {
            Gizmo.Draw.IgnoreDepth = state.Is2D;
            Gizmo.Draw.Grid(state.GridAxis, GridSize, state.GridOpacity);
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
        GridSize = ProjectCookie.Get<int>("TilesetTool.GridSize", 64);
        SceneViewportWidget.LastSelected.State.ShowGrid = false;
    }

    void ResetGrid()
    {
        ProjectCookie.Set<int>("TilesetTool.GridSize", GridSize);
        SceneViewportWidget.LastSelected.State.ShowGrid = WasGridActive;
    }

}