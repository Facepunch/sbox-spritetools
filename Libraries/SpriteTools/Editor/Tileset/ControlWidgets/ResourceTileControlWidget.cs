using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

[CustomEditor(typeof(TilesetResource.Tile))]
public class ResourceTileControlWidget : ControlWidget
{
    public override bool SupportsMultiEdit => false;

    List<TilesetResource> Tilesets = new();
    ComboBox TileComboBox;

    public ResourceTileControlWidget(SerializedProperty property) : base(property)
    {
        Layout = Layout.Row();
        Layout.Spacing = 2;

        var tile = property.GetValue<TilesetResource.Tile>();

        Tilesets.Add(tile?.Tileset);
        var tilesetCollection = Tilesets.GetSerialized() as SerializedCollection;
        var tilesetProperty = tilesetCollection.ElementAt(0);

        tilesetCollection.OnPropertyChanged += (p) =>
        {
            // Tilesets.Clear();
            // Tilesets.AddRange(tilesetCollection.Select(x => x.GetValue<TilesetResource>()));
            Rebuild();
        };
        var resourceWidget = new ResourceControlWidget(tilesetProperty)
        {
            MaximumWidth = 150
        };
        Layout.Add(resourceWidget);

        TileComboBox = new ComboBox(this)
        {
            MinimumWidth = 200
        };
        Layout.Add(TileComboBox);

        var popupButton = new IconButton("edit_note");
        popupButton.Background = Color.Transparent;
        popupButton.IconSize = 17;
        popupButton.OnClick = OpenPopup;
        popupButton.ToolTip = $"Edit more..";

        Layout.Add(popupButton);

        Rebuild();
    }

    void Rebuild()
    {
        TileComboBox.Clear();

        var tileset = Tilesets.FirstOrDefault();
        if (tileset is null)
        {
            return;
        }

        foreach (var tile in tileset.Tiles)
        {
            TileComboBox.AddItem(tile.GetName(), null, () =>
            {
                SerializedProperty.SetValue(tile);
            }, selected: tile == SerializedProperty.GetValue<TilesetResource.Tile>());
        }
    }

    protected void OpenPopup()
    {
        if (!SerializedProperty.TryGetAsObject(out var obj)) return;

        // if it's nullable, create for the actual value rather than the nullable container
        if (SerializedProperty.IsNullable)
        {
            // best way to do this?
            obj = SerializedProperty.GetValue<object>().GetSerialized();
            obj.ParentProperty = SerializedProperty;
        }

        if (obj is null)
        {
            Log.Error("Cannot create ControlSheet for a null object");
            return;
        }

        EditorUtility.OpenControlSheet(obj, this);
    }

    protected override void PaintUnder()
    {
    }
}