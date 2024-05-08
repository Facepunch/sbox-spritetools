using Sandbox;
using System.Collections.Generic;
using System.ComponentModel;

namespace SpriteTools;

[GameResource("2D Sprite", "sprite", "A 2D sprite atlas", Icon = "emoji_emotions")]
public class SpriteResource : GameResource
{
	public List<SpriteAnimation> Animations { get; set; } = new()
	{
		new SpriteAnimation()
		{
			Name = "animation_01"
		}
	};

}

public class SpriteAnimation
{
	[Property, Title("Animation Name")] private string _nameProp => Name;
	public string Name { get; set; }
	[Property, Range(0f, 999f, 0.01f, true, false), DefaultValue(15f)] public float FrameRate { get; set; } = 15.0f;
	[Property, Range(0f, 1f, 0.01f, true, false), DefaultValue(0.5f)] public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);
	[Property, DefaultValue(true)] public bool Looping { get; set; } = true;
	public List<string> Frames { get; set; }

	public SpriteAnimation()
	{
		Frames = new List<string>();
	}

	public SpriteAnimation(string name)
	{
		Name = name;
		Frames = new List<string>();
	}
}