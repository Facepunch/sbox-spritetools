using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

[CustomEditor(typeof(TilesetResource.Tile))]
public class ResourceTileControlWidget : ReferencedResourceWidget<TilesetResource>
{
    public override bool SupportsMultiEdit => false;

    public ResourceTileControlWidget(SerializedProperty property) : base(property) { }

    protected override void PopulateResources(SerializedProperty property)
    {
        var tile = property.GetValue<TilesetResource.Tile>();
        Resources.Add(tile?.Tileset);
    }

    protected override void PopulateComboBox()
    {
        var resource = Resources.FirstOrDefault();
        foreach (var tile in resource.Tiles)
        {
            ComboBox.AddItem(tile.GetName(), null, () =>
            {
                SerializedProperty.SetValue(tile);
            }, selected: tile == SerializedProperty.GetValue<TilesetResource.Tile>());
        }
    }
}