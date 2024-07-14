using System;
using Sandbox;
using SpriteTools;

namespace Quest;

public sealed class Player : Component
{
	public static Player Local { get; private set; }

	[Property, Group("References")] SpriteComponent Sprite { get; set; }
	
	[Property, Group("Variables")] Vector2 HitboxSize { get; set; } = new Vector2(32, 32);

	protected override void OnAwake()
	{
		Local = this;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (Sprite is null)
			return;

		using (Gizmo.Scope("Player"))
		{
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.LineBBox(Sprite.Bounds);
		}
	}


}