using Sandbox;
using System.Collections.Generic;
using System.Linq;

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
	public List<SpriteAnimationFrame> Frames { get; set; }

	/// <summary>
	/// The list of attachment names that are available for this animation.
	/// </summary>
	public List<string> AttachmentNames { get; set; }

	public SpriteAnimation()
	{
		Frames = new List<SpriteAnimationFrame>();
		AttachmentNames = new List<string>();
	}

	public SpriteAnimation(string name)
	{
		Name = name;
		Frames = new List<SpriteAnimationFrame>();
		AttachmentNames = new List<string>();
	}


	/// <summary>
	/// Returns a list of all attachments that are available for this animation.
	/// </summary>
	public List<SpriteAttachment> GetAttachments()
	{
		var attachments = new List<SpriteAttachment>();
		foreach (var name in AttachmentNames)
		{
			var attachment = new SpriteAttachment() { Name = name };
			var points = new List<Vector2>();
			int i = 0;
			int missedValues = 0;
			foreach (var frame in Frames)
			{
				if (frame.AttachmentPoints.TryGetValue(name, out var attachPoint))
				{
					if (missedValues > 0)
					{
						for (int j = 0; j < missedValues; j++)
						{
							points.Add(attachPoint);
						}
						missedValues = 0;
					}
					points.Add(attachPoint);
				}
				else if (points.Count == 0)
				{
					missedValues++;
				}
				i++;
			}

		}
		return attachments;
	}

	/// <summary>
	/// Returns a specific attachment by name.
	/// </summary>
	/// <param name="name">The name of the animation (case-insensitive)</param>
	/// <returns></returns>
	public SpriteAttachment GetAttachment(string name)
	{
		var attachments = GetAttachments();
		return attachments.FirstOrDefault(a => a.Name.ToLowerInvariant() == name.ToLowerInvariant());
	}
}

public class SpriteAnimationFrame
{
	public string FilePath { get; set; }
	public List<string> Events { get; set; }
	public Dictionary<string, Vector2> AttachmentPoints { get; set; }

	public SpriteAnimationFrame(string filePath)
	{
		FilePath = filePath;
		Events = new List<string>();
		AttachmentPoints = new Dictionary<string, Vector2>();
	}
}

public class SpriteAttachment
{
	public string Name { get; set; }
	public List<Vector2> AttachPoints { get; set; }

	public SpriteAttachment()
	{
		AttachPoints = new List<Vector2>();
	}

}