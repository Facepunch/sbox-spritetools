using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

[CustomEditor(typeof(AutotileBrush))]
public class AutotileBrushControlWidget : ReferencedResourceWidget<TilesetResource>
{
    public override bool SupportsMultiEdit => false;

    public AutotileBrushControlWidget(SerializedProperty property) : base(property) { }

    protected override void PopulateResources(SerializedProperty property)
    {
        var brush = property.GetValue<AutotileBrush>();
        var allTilesets = ResourceLibrary.GetAll<TilesetResource>();
        foreach (var tileset in allTilesets)
        {
            if (tileset.AutotileBrushes.Contains(brush))
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
        foreach (var brush in resource.AutotileBrushes)
        {
            ComboBox.AddItem(brush.Name, null, () =>
            {
                SerializedProperty.SetValue(brush);
            }, selected: brush == SerializedProperty.GetValue<AutotileBrush>());
        }
    }
}