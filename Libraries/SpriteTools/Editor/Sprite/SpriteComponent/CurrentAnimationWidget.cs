using System;
using Editor;
using Sandbox;

namespace SpriteTools;

[CustomEditor(typeof(string), WithAllAttributes = new[] { typeof(SpriteComponent.AnimationNameAttribute) })]
public class AnimationNameControlWidget : ControlWidget
{
	public AnimationNameControlWidget(SerializedProperty property) : base(property)
	{
		Layout = Layout.Column();
		Layout.Spacing = 2;

		AcceptDrops = false;

		Rebuild();
	}

	protected override void OnPaint()
	{

	}

	SpriteResource Sprite
	{
		get
		{
			if (!SerializedProperty.TryGetAttribute<SpriteComponent.AnimationNameAttribute>(out var attr))
				return null;

			var spriteProperty = SerializedProperty.Parent.GetProperty(attr.Parameter);
			if (spriteProperty is null)
				return null;

			return spriteProperty.GetValue<SpriteResource>(null);
		}
	}

	public void Rebuild()
	{
		Layout.Clear(true);

		var sprite = Sprite;
		if (sprite is null) return;

		if (sprite.Animations.Count <= 0)
		{
			Layout.Add(new Label("None"));
			return;
		}

		var comboBox = new ComboBox(this);
		var v = SerializedProperty.GetValue<string>();

		for (int i = 0; i < sprite.Animations.Count; ++i)
		{
			var name = sprite.Animations[i].Name;
			comboBox.AddItem(name, onSelected: () => SerializedProperty.SetValue(name), selected: string.Equals(v, name, StringComparison.OrdinalIgnoreCase));
		}

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
			var hc = new HashCode();
			hc.Add(base.ValueHash);
			hc.Add(Sprite);

			if (Sprite is not null)
			{
				for (int i = 0; i < Sprite.Animations.Count; ++i)
				{
					hc.Add(Sprite.Animations[i].Name);
				}
			}

			return hc.ToHashCode();
		}
	}
}
