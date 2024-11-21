using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetEditor;

[CustomEditor(typeof(AutotileBrush.TileReference))]
public class GameObjectControlWidget : ControlWidget
{
    public override bool SupportsMultiEdit => false;

    public GameObjectControlWidget(SerializedProperty property) : base(property)
    {
        if (!property.IsEditable)
            ReadOnly = true;

        SetSizeMode(SizeMode.Default, SizeMode.Default);

        Layout = Layout.Column();
        Layout.Spacing = 2;

        Cursor = CursorShape.Finger;
        AcceptDrops = true;
    }

    protected override void OnContextMenu(ContextMenuEvent e)
    {
        var m = new ContextMenu(this);

        if (!ReadOnly)
        {
            m.AddOption("Clear", "backspace", action: Clear);
        }

        if (m.OptionCount > 0)
        {
            m.OpenAtCursor(false);
        }

        e.Accepted = true;
    }

    protected override void PaintControl()
    {
        var rect = LocalRect.Shrink(6, 0);
        var tile = SerializedProperty.GetValue<AutotileBrush.TileReference>();
        if ((tile?.Id ?? System.Guid.Empty) == System.Guid.Empty)
        {
            Paint.SetPen(Theme.ControlText.WithAlpha(0.3f));
            Paint.DrawIcon(rect, "grid_3x3", 14, TextFlag.LeftCenter);
            rect.Left += 22;
            Paint.DrawText(rect, "None", TextFlag.LeftCenter);
        }
        else
        {
            Paint.SetPen(Theme.Green);
            Paint.DrawIcon(rect, "grid_on", 14, TextFlag.LeftCenter);
            rect.Left += 22;
            var realTile = tile?.Tileset?.Tiles.FirstOrDefault(t => t.Id == tile.Id);
            Paint.DrawText(rect, string.IsNullOrEmpty(realTile?.Name) ? $"Tile {realTile?.Position ?? Vector2Int.Zero}" : realTile.Name, TextFlag.LeftCenter);
        }
    }

    private string BuildName(GameObject go)
    {
        string str = "";

        if (go.Parent.IsValid() && go.Parent is not Scene)
        {
            str += $"{BuildName(go.Parent)} > ";
        }

        str += $"{go.Name}";

        return str;
    }

    // protected override void OnMouseClick(MouseEvent e)
    // {
    //     if (e.LeftMouseButton)
    //     {
    //         e.Accepted = true;
    //         var go = SerializedProperty.GetValue<GameObject>();
    //         if (go.IsValid() && go is not PrefabScene)
    //         {
    //             SceneEditorSession.Active?.Selection.Set(go);
    //             SceneEditorSession.Active.FullUndoSnapshot($"Selected {go}");
    //         }
    //         else
    //         {
    //             if (ReadOnly) return;

    //             var resource = (go as PrefabScene)?.Source ?? null;
    //             var asset = resource != null ? AssetSystem.FindByPath(resource.ResourcePath) : null;

    //             var prefabAssetType = AssetType.Find("prefab", false);
    //             var picker = new AssetPicker(this, new List<AssetType> { prefabAssetType });
    //             picker.MultiSelect = IsInList;
    //             picker.Window.Title = $"Select Prefab";
    //             picker.OnAssetHighlighted = (o) => UpdateFromAsset(o.FirstOrDefault());
    //             picker.OnAssetPicked = (o) => UpdateFromAssets(o);
    //             picker.Window.Show();

    //             picker.SetSelection(asset);
    //         }
    //     }
    // }

    void Clear()
    {
        SerializedProperty.SetValue<GameObject>(null);
        SignalValuesChanged();
    }

    public override void OnDragHover(DragEvent ev)
    {
        ev.Action = DropAction.Ignore;

        if (ev.Data.OfType<GameObject>().Any())
        {
            ev.Action = DropAction.Link;
            return;
        }

        if (ev.Data.HasFileOrFolder)
        {
            var asset = AssetSystem.FindByPath(ev.Data.Files.First());
            if (asset is null) return;
            if (asset.AssetType.FileExtension != "prefab") return;

            if (asset.TryLoadResource(out PrefabFile prefabFile))
            {
                ev.Action = DropAction.Link;
                return;
            }
        }

    }

    public override void OnDragDrop(DragEvent ev)
    {
        if (ev.Data.OfType<GameObject>().FirstOrDefault() is { } go)
        {
            SerializedProperty.SetValue(go);
            return;
        }

        if (ev.Data.HasFileOrFolder)
        {
            var asset = AssetSystem.FindByPath(ev.Data.Files.First());
            if (asset is null) return;
            if (asset.AssetType.FileExtension != "prefab") return;

            if (asset.TryLoadResource(out PrefabFile prefabFile))
            {
                SerializedProperty.SetValue(SceneUtility.GetPrefabScene(prefabFile));

                return;
            }
        }
    }
}
