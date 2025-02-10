using System;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool;

[CustomEditor(typeof(int), NamedEditor = "autotile_index")]
public class AutotileWidget : ControlWidget
{
	public static AutotileWidget Instance { get; private set; }
	public AutotileBrush Brush { get; private set; }

	public AutotileWidget(SerializedProperty property) : base(property)
	{
		Instance = this;
		Layout = Layout.Column();
		Layout.Spacing = 2;

		AcceptDrops = false;

		Rebuild();
	}

	protected override void OnPaint()
	{

	}

	public void Rebuild()
	{
		Layout.Clear(true);

		var tool = TilesetTool.Active;
		if (tool is null) return;
		var tilesetComponent = tool?.SelectedComponent;
		var layer = tool?.SelectedLayer;
		if (!tilesetComponent.IsValid())
		{
			Layout.Add(new Label("No tileset selected"));
			return;
		}
		else if (layer is null)
		{
			Layout.Add(new Label("No layer selected"));
			return;
		}

		if (layer?.TilesetResource is null)
		{
			Layout.Add(new Label("Layer has no tileset"));
			return;
		}

		var allBrushes = layer?.TilesetResource?.GetAllAutotileBrushes();

		if ((allBrushes?.Count ?? 0) == 0)
		{
			Layout.Add(new Label("Tileset has no Autotile Brushes"));
			return;
		}

		var comboBox = new ComboBox(this);
		var v = SerializedProperty.GetValue<int>();
		if (v >= 0 && v < allBrushes.Count)
		{
			Brush = allBrushes[v];
		}
		else
		{
			Brush = null;
		}

		comboBox.AddItem("None", "check_box_outline_blank", onSelected: () => SetValue(-1), selected: v == -1);

		for (int i = 0; i < allBrushes.Count; ++i)
		{
			int index = i;
			var autotile = allBrushes[i];
			if (autotile is null) continue;
			var name = string.IsNullOrWhiteSpace(autotile.Name) ? $"Autotile {i}" : autotile.Name;
			comboBox.AddItem(name, "grid_on", onSelected: () => SetValue(index), selected: v == index);
		}

		comboBox.StateCookie = $"autotile.{tilesetComponent.Id}.{layer.Name}";

		Layout.Add(comboBox);
	}

	protected override void OnValueChanged()
	{
		Rebuild();
	}

	protected override int ValueHash
	{
		get
		{
			var tool = TilesetTool.Active;
			var tilesetComponent = tool?.SelectedComponent;
			var selectedLayer = tool?.SelectedLayer;

			var hc = new HashCode();
			hc.Add(base.ValueHash);
			hc.Add(tool);
			hc.Add(tilesetComponent?.Id.ToString() ?? "");
			hc.Add(selectedLayer);
			hc.Add(selectedLayer?.TilesetResource);

			if (tilesetComponent is not null && selectedLayer?.TilesetResource is not null)
			{
				foreach (var autotile in selectedLayer.TilesetResource.GetAllAutotileBrushes())
				{
					hc.Add(autotile.GetHashCode());
				}
			}

			return hc.ToHashCode();
		}
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		if (Instance == this)
		{
			Instance = null;
		}
	}

	void SetValue(int val)
	{
		SerializedProperty.SetValue(val);

		var layer = TilesetTool.Active?.SelectedLayer;
		var allBrushes = layer.TilesetResource.GetAllAutotileBrushes();
		if (layer is not null && val >= 0 && val < allBrushes.Count)
		{
			Brush = allBrushes[val];
		}
		else
		{
			Brush = null;
		}
	}
}
