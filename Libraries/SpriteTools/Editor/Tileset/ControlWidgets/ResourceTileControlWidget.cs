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
        var allTilesets = ResourceLibrary.GetAll<TilesetResource>();
        foreach (var tileset in allTilesets)
        {
            if (tileset.Tiles.Contains(tile))
            {
                Resources.Add(tileset);
                return;
            }
        }
        Resources.Add(null);
    }

    protected override void PopulateComboBox()
    {
        var resource = Resources.FirstOrDefault();
        foreach (TilesetResource.Tile tile in resource.Tiles)
        {
            ComboBox.AddItem(tile.GetName(), null, () =>
            {
                SerializedProperty.SetValue(tile);
            }, selected: tile == SerializedProperty.GetValue<TilesetResource.Tile>());
        }
    }
}