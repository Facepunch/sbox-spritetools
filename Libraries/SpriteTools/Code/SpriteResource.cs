using Sandbox;
using System.Collections.Generic;
using System.ComponentModel;

namespace SpriteTools;

[GameResource("2D Sprite", "sprite", "A 2D sprite atlas", Icon = "emoji_emotions")]
public class SpriteResource : GameResource
{
	/// <summary>
	/// A list of animations that are available for this sprite.
	/// </summary>
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
	/// <summary>
	/// The name of the animation. This is used as a key to reference the animation.
	/// </summary>
	[Property, Title("Animation Name")]
	private string _nameProp => Name;

	/// <summary>
	/// The speed of the animation. This is the number of frames per second.
	/// </summary>
	[Property, Range(0f, 999f, 0.01f, true, false), DefaultValue(15f)]
	public float FrameRate { get; set; } = 15.0f;

	/// <summary>
	/// The origin of the sprite. This is used to determine where the sprite is drawn relative to/scaled around.
	/// </summary>
	[Property, Range(0f, 1f, 0.01f, true, false), DefaultValue(0.5f)] public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

	/// <summary>
	/// Whether or not the animation should loop.
	/// </summary>
	[Property, DefaultValue(true)]
	public bool Looping { get; set; } = true;

	/// <summary>
	/// The name of the animation. This is used as a key to reference the animation.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The list of frames that make up the animation. These are image paths.
	/// </summary>
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