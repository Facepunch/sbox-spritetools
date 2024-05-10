using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SpriteTools;

[Icon("emoji_emotions")]
public sealed class SpriteComponent : Component, Component.ExecuteInEditor
{
    /// <summary>
    /// The sprite resource that this component uses.
    /// </summary>
    [Property]
    SpriteResource Sprite
    {
        get => _sprite;
        set
        {
            _sprite = value;
            CurrentAnimation = _sprite.Animations.FirstOrDefault();
            UpdateSprite();
        }
    }
    SpriteResource _sprite;

    /// <summary>
    /// The color tint of the sprite.
    /// </summary>
    [Property]
    public Color Tint
    {
        get => SceneObject?.ColorTint ?? Color.White;
        set
        {
            if (SceneObject != null)
                SceneObject.ColorTint = value;
        }
    }

    [Property]
    public Material MaterialOverride { get; set; }
    private Material SpriteMaterial { get; set; }

    /// <summary>
    /// The playback speed of the animation.
    /// </summary>
    [Property] public float PlaybackSpeed { get; set; } = 1.0f;


    [Property]
    [Category("Lighting")]
    public ShadowRenderType CastShadows { get; set; } = ShadowRenderType.On;

    /// <summary>
    /// A dictionary of broadcast events that this component will send (populated based on the Sprite resource)
    /// </summary>
    public Dictionary<string, Action> BroadcastEvents = new();

    /// <summary>
    /// The sprite animation that is currently playing.
    /// </summary>
    public SpriteAnimation CurrentAnimation { get; private set; }

    /// <summary>
    /// The current frame index of the animation playing.
    /// </summary>
    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        set
        {
            _currentFrameIndex = value;
            if (CurrentAnimation is not null && _currentFrameIndex >= CurrentAnimation.Frames.Count)
                _currentFrameIndex = 0;
        }
    }
    private int _currentFrameIndex = 0;
    private float _timeSinceLastFrame = 0;

    internal SceneObject SceneObject { get; set; }

    protected override void OnStart()
    {
        base.OnEnabled();

        if (Sprite is null) return;
        if (Sprite.Animations.Count > 0)
            PlayAnimation(Sprite.Animations[0].Name);

        UpdateSceneObject();
    }

    protected override void OnEnabled()
    {
        base.OnEnabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = true;
    }

    protected override void OnDisabled()
    {
        base.OnDisabled();

        if (SceneObject.IsValid())
            SceneObject.RenderingEnabled = false;
    }

    protected override void OnUpdate()
    {
        UpdateSceneObject();
    }

    protected override void DrawGizmos()
    {
        base.DrawGizmos();

        if (Game.IsPlaying) return;

        // Move bbox by origin
        var bbox = SceneObject.LocalBounds;
        bbox = bbox.Rotate(Transform.Rotation);
        var origin = CurrentAnimation.Origin - new Vector2(0.5f, 0.5f);
        var pos = Transform.Position - (new Vector3(origin.y, origin.x, 0) * bbox.Size);
        bbox = bbox.Translate(pos);
        Gizmo.Hitbox.BBox(bbox);

        if (Gizmo.IsHovered)
        {
            Gizmo.Draw.Color = Color.Orange;
            Gizmo.Draw.LineBBox(bbox);
        }
    }

    internal void UpdateSceneObject()
    {
        if (!SceneObject.IsValid())
        {
            SceneObject = new SceneObject(Scene.SceneWorld, Model.Load("models/sprite_quad.vmdl"))
            {
                Flags = { IsTranslucent = true }
            };
        }

        if (SpriteMaterial is null)
        {
            SpriteMaterial = Material.Create("spritemat", "shaders/pixelated_masked.shader");
            SceneObject.SetMaterialOverride(SpriteMaterial);
        }

        SceneObject.Flags.ExcludeGameLayer = CastShadows == ShadowRenderType.ShadowsOnly;
        SceneObject.Flags.CastShadows = CastShadows == ShadowRenderType.On || CastShadows == ShadowRenderType.ShadowsOnly;

        if (CurrentAnimation == null)
        {
            SceneObject.Transform = new Transform(Transform.Position, Transform.Rotation, Transform.Scale);
            return;
        }

        var frameRate = (1f / ((PlaybackSpeed == 0) ? 0 : (CurrentAnimation.FrameRate * Math.Abs(PlaybackSpeed))));
        _timeSinceLastFrame += ((Game.IsPlaying) ? Time.Delta : RealTime.Delta);

        if (PlaybackSpeed > 0 && _timeSinceLastFrame >= frameRate)
        {
            var frame = CurrentFrameIndex;
            frame++;
            if (frame >= CurrentAnimation.Frames.Count)
                frame = 0;
            CurrentFrameIndex = frame;
            _timeSinceLastFrame = 0;
        }
        else if (PlaybackSpeed < 0 && _timeSinceLastFrame >= frameRate)
        {
            var frame = CurrentFrameIndex;
            frame--;
            if (frame < 0)
                frame = CurrentAnimation.Frames.Count - 1;
            CurrentFrameIndex = frame;
            _timeSinceLastFrame = 0;
        }

        var texture = Texture.Load(FileSystem.Mounted, CurrentAnimation.Frames[CurrentFrameIndex].FilePath);
        SpriteMaterial.Set("Texture", texture);

        // Add pivot to transform
        var pos = Transform.Position;
        var origin = CurrentAnimation.Origin - new Vector2(0.5f, 0.5f);
        pos -= new Vector3(origin.y, origin.x, 0) * 100f * Transform.Scale;
        pos = pos.RotateAround(Transform.Position, Transform.Rotation);
        SceneObject.Transform = new Transform(pos, Transform.Rotation, Transform.Scale);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        SceneObject?.Delete();
        SceneObject = null;
    }

    internal void UpdateSprite()
    {
        BroadcastEvents.Clear();
        if (Sprite == null) return;

        foreach (var animation in Sprite.Animations)
        {
            foreach (var frame in animation.Frames)
            {
                foreach (var tag in frame.Events)
                {
                    if (!BroadcastEvents.ContainsKey(tag))
                        BroadcastEvents[tag] = () => { };
                }
            }
        }
    }

    public void PlayAnimation(string animationName)
    {
        if (Sprite == null) return;

        var animation = Sprite.Animations.Find(a => a.Name == animationName);
        if (animation == null) return;

        CurrentAnimation = animation;
        CurrentFrameIndex = 0;
    }

    public enum ShadowRenderType
    {
        [Icon("wb_shade")]
        [Description("Render the sprite with shadows (default)")]
        On,
        [Icon("wb_twilight")]
        [Description("Render the sprite without shadows")]
        Off,
        [Icon("hide_source")]
        [Title("Shadows Only")]
        [Description("Render ONLY the sprites shadows")]
        ShadowsOnly
    }
}

// public sealed class SpriteComponent : Component, Component.ExecuteInEditor
// {
// 	[Property]
// 	public Model BaseModel
// 	{
// 		get => _baseModel;
// 		set
// 		{
// 			_baseModel = value;
// 			SceneObject?.Delete();
// 			SceneObject = null;
// 		}
// 	}
// 	Model _baseModel { get; set; } = Model.Load( "models/quad.vmdl" );
// 	[Property] public SpriteResource Sprite { get; set; }
// 	Material SpriteMaterial { get; set; }
// 	internal SceneObject SceneObject { get; set; }

// 	private int currentFrameIndex = 0;
// 	private RealTimeSince timeSinceLastFrame = 0;
// 	public string CurrentAnimationName;
// 	private bool isLooping;

// 	[Property] private float FrameRateOverride { get; set; } = 0;

// 	public bool IsFlipped { get; set; }

// 	[Property] public string DefaultAnimation { get; set; } = "idle";

// 	protected override void OnStart()
// 	{
// 		base.OnStart();

// 		PlayAnimation( DefaultAnimation, true );
// 	}

// 	protected override void OnUpdate()
// 	{
// 		if ( SceneObject == null )
// 		{
// 			SceneObject = new SceneObject( Scene.SceneWorld, BaseModel )
// 			{
// 				Flags = { IsTranslucent = true }
// 			};
// 		}

// 		//if ( Sprite == null || Sprite.Animations.Count == 0 ) return;

// 		UpdateSceneObject();
// 	}

// 	private void UpdateSceneObject()
// 	{
// 		if ( SpriteMaterial == null )
// 		{
// 			SpriteMaterial = Material.Create( "spritegraph", "materials/sprites/spritegraph" );
// 			SceneObject.SetMaterialOverride( SpriteMaterial );
// 		}

// 		var animation = Sprite.Animations.Find( a => a.AnimationName == CurrentAnimationName );

// 		//if ( animation == null ) return;

// 		var frameRate = FrameRateOverride > 0 ? FrameRateOverride : animation.Animation.FrameRate;

// 		if ( timeSinceLastFrame > 1.0f / frameRate )
// 		{
// 			currentFrameIndex++;
// 			if ( currentFrameIndex >= animation.Animation.AtlasPath.Count )
// 			{
// 				if ( isLooping )
// 				{
// 					currentFrameIndex = 0;
// 				}
// 				else
// 				{
// 					currentFrameIndex = animation.Animation.AtlasPath.Count - 1; // Stop on the last frame
// 				}
// 			}
// 			timeSinceLastFrame = 0;
// 		}

// 		var texture = Texture.Load( FileSystem.Mounted, animation.Animation.AtlasPath[currentFrameIndex] );
// 		SpriteMaterial.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteFlipped", IsFlipped );


// 		SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 	}

// 	public void PlayAnimation( string animationName, bool loop )
// 	{
// 		if ( CurrentAnimationName == animationName ) return;
// 		CurrentAnimationName = animationName;
// 		isLooping = loop;
// 		currentFrameIndex = 0; // Restart the animation
// 	}

// 	protected override void OnDestroy()
// 	{
// 		base.OnDestroy();
// 		SceneObject?.Delete();
// 		SceneObject = null;
// 	}
// }

// public sealed class StaticSpriteComponent : Component, Component.ExecuteInEditor
// {
// 	[Property] public AnimatedSpriteResource animatedSpriteResource { get; set; }
// 	[Property] public bool IsSelfIlluminated { get; set; }
// 	Material SpriteMaterial { get; set; }
// 	internal SceneObject SceneObject { get; set; }

// 	private int currentFrameIndex = 0;
// 	private RealTimeSince timeSinceLastFrame = 0;
// 	private string currentAnimationName;
// 	private bool isLooping;

// 	public bool IsFlipped { get; set; }

// 	[Property] public string DefaultAnimation { get; set; } = "idle";

// 	protected override void OnStart()
// 	{
// 		base.OnStart();

// 		PlayAnimation( DefaultAnimation, true );
// 	}

// 	protected override void OnUpdate()
// 	{
// 		if ( SceneObject == null )
// 		{
// 			SceneObject = new SceneObject( Scene.SceneWorld, Model.Load( "models/quad.vmdl" ) )
// 			{
// 				Flags = { IsTranslucent = true }
// 			};
// 		}

// 		if ( animatedSpriteResource == null ) return;

// 		UpdateSceneObject();
// 	}

// 	private void UpdateSceneObject()
// 	{
// 		if ( SpriteMaterial == null )
// 		{
// 			SpriteMaterial = Material.Create( "spritegraph", "materials/sprites/spritegraph" );
// 			SceneObject.SetMaterialOverride( SpriteMaterial );
// 		}

// 		var animation = animatedSpriteResource;

// 		if ( animation.AtlasPath.Count == 1 )
// 		{
// 			var singletexture = Texture.Load( FileSystem.Mounted, animation.AtlasPath[0] );
// 			SpriteMaterial.Set( "SpriteColor", singletexture );
// 			SceneObject.Attributes.Set( "SpriteColor", singletexture );
// 			SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 			return;
// 		}

// 		//if ( animation == null ) return;

// 		if ( timeSinceLastFrame > 1.0f / animation.FrameRate )
// 		{
// 			currentFrameIndex++;
// 			if ( currentFrameIndex >= animation.AtlasPath.Count )
// 			{
// 				if ( isLooping )
// 				{
// 					currentFrameIndex = 0;
// 				}
// 				else
// 				{
// 					currentFrameIndex = animation.AtlasPath.Count - 1; // Stop on the last frame
// 				}
// 			}
// 			timeSinceLastFrame = 0;
// 		}

// 		var texture = Texture.Load( FileSystem.Mounted, animation.AtlasPath[currentFrameIndex] );
// 		SpriteMaterial.Set( "SpriteColor", texture );
// 		SpriteMaterial.Set( "SelfIllum", IsSelfIlluminated );
// 		SceneObject.Attributes.Set( "SpriteColor", texture );
// 		SceneObject.Attributes.Set( "SpriteFlipped", IsFlipped );
// 		SceneObject.Attributes.Set( "SelfIllum", IsSelfIlluminated );

// 		SceneObject.Transform = new Transform( Transform.Position, Transform.Rotation, Transform.Scale );
// 	}

// 	public void PlayAnimation( string animationName, bool loop )
// 	{
// 		if ( currentAnimationName == animationName ) return;
// 		currentAnimationName = animationName;
// 		isLooping = loop;
// 		currentFrameIndex = 0; // Restart the animation
// 	}

// 	protected override void OnDestroy()
// 	{
// 		base.OnDestroy();
// 		SceneObject?.Delete();
// 		SceneObject = null;
// 	}
// }
