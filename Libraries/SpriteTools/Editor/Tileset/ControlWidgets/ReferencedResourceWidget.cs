using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

public class ReferencedResourceWidget<T> : ControlWidget
{
    public override bool SupportsMultiEdit => false;

    protected List<T> Resources;
    protected ComboBox ComboBox;

    public ReferencedResourceWidget(SerializedProperty property) : base(property)
    {
        Layout = Layout.Row();
        Layout.Spacing = 2;

        Resources ??= new();
        PopulateResources(property);

        var collection = Resources.GetSerialized() as SerializedCollection;
        var tilesetProperty = collection.ElementAt(0);

        collection.OnPropertyChanged += (p) =>
        {
            Rebuild();
        };
        var resourceWidget = new ResourceControlWidget(tilesetProperty)
        {
            MaximumWidth = 150
        };
        Layout.Add(resourceWidget);

        ComboBox = new ComboBox(this)
        {
            MinimumWidth = 200
        };
        Layout.Add(ComboBox);

        var popupButton = new IconButton("edit_note");
        popupButton.Background = Color.Transparent;
        popupButton.IconSize = 17;
        popupButton.OnClick = OpenPopup;
        popupButton.ToolTip = $"Edit more..";

        Layout.Add(popupButton);

        Rebuild();
    }

    protected virtual void PopulateResources(SerializedProperty property)
    {
        var t = property.GetValue<T>();
        Resources.Add(t);
    }

    protected virtual void PopulateComboBox()
    {
        // Override this
    }

    void Rebuild()
    {
        ComboBox.Clear();

        var resource = Resources.FirstOrDefault();
        if (resource is null)
        {
            T nullResource = default;
            SerializedProperty.SetValue(nullResource);
            return;
        }

        PopulateComboBox();
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