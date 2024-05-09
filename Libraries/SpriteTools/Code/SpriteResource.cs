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

	protected override void PostLoad()
	{
		base.PostLoad();

		foreach (var animation in Animations)
		{
			Vector2 lastPosition = Vector2.Zero;
			bool firstFrame = true;
			int passedFrames = 0;
			foreach (var attachment in animation.Attachments)
			{
				attachment.Points = new();
				foreach (var frame in animation.Frames)
				{
					if (!frame.AttachmentPoints.ContainsKey(attachment.Name))
					{
						if (firstFrame)
						{
							passedFrames++;
						}
						else
						{
							frame.AttachmentPoints[attachment.Name] = lastPosition;
						}
					}
					else
					{
						lastPosition = frame.AttachmentPoints[attachment.Name];
						attachment.Points.Add(lastPosition);

						if (firstFrame)
						{
							for (int i = 0; i < passedFrames; i++)
							{
								attachment.Points.Add(lastPosition);
							}
							firstFrame = false;
						}
					}
				}
			}
		}
	}

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
	public List<SpriteAttachment> Attachments { get; set; }

	public SpriteAnimation()
	{
		Frames = new List<SpriteAnimationFrame>();
		Attachments = new List<SpriteAttachment>();
	}

	public SpriteAnimation(string name)
	{
		Name = name;
		Frames = new List<SpriteAnimationFrame>();
		Attachments = new List<SpriteAttachment>();
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
	/// <summary>
	/// The name of the attachment point. This is used as a key to reference the attachment point.
	/// </summary>
	public string Name { get; set; }

	/// <summary>
	/// The color of the attachment point. This is purely used as a visual aid in the Sprite Editor.
	/// </summary>
	public Color Color { get; set; }

	/// <summary>
	/// A list of points corresponding to the attachment point's position in each frame.
	/// </summary>
	public List<Vector2> Points { get; set; }

	public SpriteAttachment()
	{
		Name = "new attachment";
		Color = Color.Red;
		Points = new List<Vector2>();
	}

	public SpriteAttachment(string name)
	{
		Name = name;
		Color = Color.Red;
		Points = new List<Vector2>();
	}


}