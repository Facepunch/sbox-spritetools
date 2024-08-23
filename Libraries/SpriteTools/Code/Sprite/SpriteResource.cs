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

	/// <summary>
	/// Returns a list of names for every attachment this Sprite has.
	/// </summary>
	/// <returns>A list of names for every attachment this Sprite has</returns>
	public List<string> GetAttachmentNames()
	{
		var attachmentNames = new List<string>();
		foreach (var animation in Animations)
		{
			foreach (var attachment in animation.Attachments)
			{
				if (!attachmentNames.Contains(attachment.Name))
					attachmentNames.Add(attachment.Name);
			}
		}
		return attachmentNames;
	}

	/// <summary>
	/// Try to load a sprite from a file path.
	/// </summary>
	/// <param name="path">The path to the sprite resource</param>
	public static SpriteResource Load(string path)
	{
		return ResourceLibrary.Get<SpriteResource>(path);
	}

	/// <summary>
	/// Returns the first frame of a sprite resource as a texture.
	/// </summary>
	/// <returns></returns>
	public Texture GetPreviewTexture()
	{
		var anim = Animations.FirstOrDefault();
		if (anim is null || anim.Frames.Count == 0) return Texture.Transparent;
		if (anim.Frames.Count == 1) return Texture.Load(FileSystem.Mounted, anim.Frames[0].FilePath);
		var atlas = TextureAtlas.FromAnimation(anim);
		return atlas.GetTextureFromFrame(0);
	}

	/// <summary>
	/// Returns a list of all the texture paths used by this sprite.
	/// </summary>
	/// <returns></returns>
	public List<string> GetAllTexturePaths()
	{
		var paths = new List<string>();
		foreach (var animation in Animations)
		{
			foreach (var frame in animation.Frames)
			{
				if (paths.Contains(frame.FilePath)) continue;
				paths.Add(frame.FilePath);
			}
		}
		return paths;
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
	[Property, Range(0f, 999f, 0.01f, true, false)]
	public float FrameRate { get; set; } = 15.0f;

	/// <summary>
	/// The origin of the sprite. This is used to determine where the sprite is drawn relative to/scaled around.
	/// </summary>
	[Property, Range(0f, 1f, 0.01f, true, false)] public Vector2 Origin { get; set; } = new Vector2(0.5f, 0.5f);

	/// <summary>
	/// Whether or not the animation should loop.
	/// </summary>
	[Property]
	public bool Looping { get; set; } = false;

	/// <summary>
	/// The name of the animation. This is used as a key to reference the animation.
	/// </summary>
	public string Name { get; set; } = "";

	/// <summary>
	/// The list of frames that make up the animation. These are image paths.
	/// </summary>
	public List<SpriteAnimationFrame> Frames { get; set; } = new();

	/// <summary>
	/// The list of attachment names that are available for this animation.
	/// </summary>
	public List<SpriteAttachment> Attachments { get; set; } = new();

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

	public Vector2 GetAttachmentPosition(string attachment, int index)
	{
		var attach = Attachments?.FirstOrDefault(x => x.Name == attachment);
		if (attach is null) return Vector2.Zero;
		if (index < 0) return attach.Points.FirstOrDefault();
		if (index >= attach.Points.Count) return attach.Points.LastOrDefault();
		return attach.Points[index];
	}
}

public class SpriteAnimationFrame
{
	public string FilePath { get; set; }
	public List<string> Events { get; set; }
	public Rect SpriteSheetRect { get; set; }

	public SpriteAnimationFrame(string filePath)
	{
		FilePath = filePath;
		Events = new List<string>();
		SpriteSheetRect = new Rect(0, 0, 0, 0);
	}

	public SpriteAnimationFrame Copy()
	{
		var copy = new SpriteAnimationFrame(FilePath);
		copy.Events = new List<string>(Events);
		copy.SpriteSheetRect = SpriteSheetRect;
		return copy;
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

	/// <summary>
	/// Whether or not the attachment point is visible in the Sprite Editor.
	/// </summary>
	public bool Visible { get; set; }

	public SpriteAttachment()
	{
		Name = "new attachment";
		Color = Color.Red;
		Points = new List<Vector2>();
		Visible = true;
	}

	public SpriteAttachment(string name)
	{
		Name = name;
		Color = Color.Red;
		Points = new List<Vector2>();
		Visible = true;
	}

	public SpriteAttachment Copy()
	{
		var copy = new SpriteAttachment(Name);
		copy.Color = Color;
		copy.Points = new List<Vector2>(Points);
		copy.Visible = Visible;
		return copy;
	}

}