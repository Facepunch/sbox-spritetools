using Sandbox;
using System.Collections.Generic;

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
	[Property] public string Name { get; set; }
	[Property, Range(0f, 999f, 0.01f, true, false)] public float FrameRate { get; set; } = 15.0f;
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