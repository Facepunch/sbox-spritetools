using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Category( "2D" )]
[Icon( "emoji_emotions" )]
[Title( "2D Sprite" )]
[Tint( EditorTint.Yellow )]
public sealed class SpriteComponent : Component, Component.ExecuteInEditor
{
	/// <summary>
	/// The sprite resource that this component uses.
	/// </summary>
	[Property]
	public SpriteResource Sprite
	{
		get => _sprite;
		set
		{
			if ( _sprite == value ) return;
			_sprite = value;
			_currentAnimation = null;
			if ( _sprite != null )
			{
				PlayAnimation( StartingAnimationName, true );
			}
			else
				CurrentAnimation = null;

			UpdateSprite();
			ApplyMaterialOffset();
		}
	}
	SpriteResource _sprite;

	[Property]
	public Axis UpDirection
	{
		get => _upDirection;
		set
		{
			_upDirection = value;
			switch ( value )
			{
				case Axis.XPositive:
					_rotationOffset = Rotation.From( 0, 180, 0 );
					break;
				case Axis.XNegative:
					_rotationOffset = Rotation.From( 0, 0, 0 );
					break;
				case Axis.YPositive:
					_rotationOffset = Rotation.From( 0, -90, 0 );
					break;
				case Axis.YNegative:
					_rotationOffset = Rotation.From( 0, 90, 0 );
					break;
				case Axis.ZPositive:
					_rotationOffset = Rotation.From( 90, 0, 0 );
					break;
				case Axis.ZNegative:
					_rotationOffset = Rotation.From( -90, 0, 0 );
					break;
			}
		}
	}
	Axis _upDirection = Axis.YPositive;
	Rotation _rotationOffset = Rotation.From( 0, -90, 0 );

	/// <summary>
	/// The color tint of the Sprite.
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public Color Tint
	{
		get => _tint;
		set
		{
			if ( _tint == value ) return;
			_tint = value;
			if ( SceneObject != null )
				SceneObject.ColorTint = value;
		}
	}
	Color _tint = Color.White;

	/// <summary>
	/// The color of the sprite when it is flashing.
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public Color FlashTint
	{
		get => _flashTint;
		set
		{
			if ( _flashTint == value ) return;
			_flashTint = value;
			SpriteMaterial?.Set( "g_vFlashColor", value );
			SpriteMaterial?.Set( "g_flFlashAmount", value.a );
		}
	}
	Color _flashTint = Color.White.WithAlpha( 0 );

	/// <summary>
	/// Used to override the material with your own. Useful for custom shaders.
	/// Shader requires a texture parameter named "Texture".
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public Material MaterialOverride
	{
		get => _materialOverride;
		set
		{
			_materialOverride = value;
			SpriteMaterial = null;
		}
	}
	Material _materialOverride;
	private Material SpriteMaterial { get; set; }
	public Material Material => SpriteMaterial;

	/// <summary>
	/// The playback speed of the animation.
	/// </summary>
	[Property] public float PlaybackSpeed { get; set; } = 1.0f;

	/// <summary>
	/// Whether or not the object should scale based on the resolution of the Sprite.
	/// </summary>
	[Property] public bool UsePixelScale { get; set; } = false;


	/// <summary>
	/// Whether or not the sprite should render itself/its shadows.
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public ShadowRenderType CastShadows { get; set; } = ShadowRenderType.On;

	[Property]
	[Category( "Visuals" )]
	public SpriteFlags SpriteFlags
	{
		get => _spriteFlags;
		set
		{
			if ( _spriteFlags == value ) return;
			_spriteFlags = value;
			ApplySpriteFlags();
		}
	}
	private SpriteFlags _spriteFlags = SpriteFlags.None;

	/// <summary>
	/// A dictionary of broadcast events that this component will send (populated based on the Sprite resource)
	/// </summary>
	[Property, Hide]
	public Dictionary<string, Action<SpriteComponent>> BroadcastEvents = new();

	/// <summary>
	/// The sprite animation that is currently playing.
	/// </summary>
	[JsonIgnore]
	public SpriteAnimation CurrentAnimation
	{
		get => _currentAnimation;
		set
		{
			if ( value is null )
			{
				_currentAnimation = null;
				return;
			}
			PlayAnimation( value.Name );
		}
	}
	[JsonIgnore]
	SpriteAnimation _currentAnimation;

	[Property, Category( "Sprite" ), Title( "Current Animation" ), AnimationName]
	private string StartingAnimationName
	{
		get => CurrentAnimation?.Name ?? ( _sprite?.Animations?.FirstOrDefault()?.Name ?? "" );
		set
		{
			if ( Sprite == null ) return;
			var animation = Sprite.Animations.Find( a => a.Name.ToLowerInvariant() == value.ToLowerInvariant() );
			if ( animation == null ) return;
			PlayAnimation( animation.Name );
		}
	}

	[JsonIgnore, Property, Category( "Sprite" ), HideIf( "HasBroadcastEvents", false )]
	BroadcastControls _broadcastEvents = new();

	[Property, Category( "Sprite" )]
	public bool CreateAttachPoints
	{
		get => _createAttachPoints;
		set
		{
			if ( _createAttachPoints != value )
			{
				_createAttachPoints = value;

				AttachPoints.Clear();
				if ( value )
				{
					BuildAttachPoints();
				}
			}
		}
	}
	bool _createAttachPoints = false;
	Dictionary<string, GameObject> AttachPoints = new();

	/// <summary>
	/// Invoked when a broadcast event is triggered.
	/// </summary>
	[Property, Group( "Sprite" )]
	public Action<string> OnBroadcastEvent { get; set; }

	/// <summary>
	/// Invoked when an animation reaches the last frame.
	/// </summary>
	[Property, Group( "Sprite" )]
	public Action<string> OnAnimationComplete { get; set; }

	/// <summary>
	/// The current texture atlas that the sprite is using.
	/// </summary>
	public TextureAtlas CurrentTexture { get; set; }

	/// <summary>
	/// Whether or not the sprite has any broadcast events.
	/// </summary>
	public bool HasBroadcastEvents => BroadcastEvents.Count > 0;

	public BBox Bounds
	{
		get
		{
			var ratio = CurrentTexture?.AspectRatio ?? 1;
			var size = new Vector2( 50, 50 );
			if ( UsePixelScale )
			{
				var scl = CurrentTexture.FrameSize.x < CurrentTexture.FrameSize.x ? CurrentTexture.FrameSize.y : CurrentTexture.FrameSize.y;
				size *= new Vector2( scl, scl ) / 100f;
			}
			BBox bbox = new BBox( new Vector3( -size.x, -size.y * ratio, -0.1f ), new Vector3( size.x, size.y * ratio, 0.1f ) );
			var origin = ( CurrentAnimation?.Origin ?? new Vector2( 0.5f, 0.5f ) ) - new Vector2( 0.5f, 0.5f );
			bbox = bbox.Translate( new Vector3( origin.y, origin.x, 0 ) * new Vector3( -size.x * 2f, -size.y * 2f, 1f ) );
			return bbox;
		}
	}

	/// <summary>
	/// The current frame index of the animation playing.
	/// </summary>
	public int CurrentFrameIndex
	{
		get => _currentFrameIndex;
		set
		{
			_currentFrameIndex = value;
			if ( CurrentAnimation is not null )
			{
				if ( _currentFrameIndex >= CurrentAnimation.Frames.Count )
					_currentFrameIndex = 0;
				ApplyMaterialOffset();
			}
		}
	}
	private int _currentFrameIndex = 0;
	private float _timeSinceLastFrame = 0;
	private bool _isPingPonging = false;
	private bool _flipHorizontal = false;
	private bool _flipVertical = false;

	internal SceneObject SceneObject { get; set; }

	protected override void OnStart ()
	{
		base.OnStart();

		if ( Sprite is null ) return;
		if ( Sprite.Animations.Count > 0 )
		{
			var anim = Sprite.Animations.FirstOrDefault( x => x.Name.ToLowerInvariant() == StartingAnimationName );
			if ( anim is null )
				anim = Sprite.Animations.FirstOrDefault();
			PlayAnimation( anim.Name );
		}

		if ( SpriteMaterial is null )
		{
			if ( MaterialOverride != null )
				SpriteMaterial = MaterialOverride.CreateCopy();
			else
				SpriteMaterial = Material.Create( "spritemat", "shaders/sprite_2d.shader" );

			SpriteMaterial?.Set( "g_vFlashColor", _flashTint );
			SpriteMaterial?.Set( "g_flFlashAmount", _flashTint.a );
		}

		UpdateSprite();
		UpdateSceneObject();
		ApplySpriteFlags();
		FlashTint = _flashTint;
	}

	protected override void OnAwake ()
	{
		base.OnAwake();

		SceneObject ??= new SceneObject( Scene.SceneWorld, Model.Load( "models/sprite_quad_1_sided.vmdl" ) )
		{
			Flags = { IsTranslucent = true },
			ColorTint = Tint
		};
	}

	protected override void OnEnabled ()
	{
		base.OnEnabled();

		if ( SceneObject.IsValid() )
		{
			SceneObject.RenderingEnabled = true;
			SceneObject.ColorTint = Tint;
			SceneObject.Tags.SetFrom( GameObject.Tags );
		}

		if ( CreateAttachPoints )
			BuildAttachPoints();
	}

	protected override void OnDisabled ()
	{
		base.OnDisabled();

		if ( SceneObject.IsValid() )
			SceneObject.RenderingEnabled = false;
	}

	protected override void DrawGizmos ()
	{
		base.DrawGizmos();
		if ( Game.IsPlaying ) return;
		if ( Sprite is null ) return;

		Gizmo.Transform = Gizmo.Transform.WithRotation( WorldRotation * _rotationOffset );
		var bbox = Bounds;
		Gizmo.Hitbox.BBox( bbox );

		if ( Gizmo.IsHovered || Gizmo.IsSelected )
		{
			bbox.Mins.z = 0;
			bbox.Maxs.z = 0.0f;
			Gizmo.Draw.Color = Gizmo.IsSelected ? Color.White : Color.Orange;
			Gizmo.Draw.LineBBox( bbox );
		}
	}

	protected override void OnTagsChanged ()
	{
		base.OnTagsChanged();

		if ( SceneObject.IsValid() )
		{
			SceneObject.Tags.SetFrom( GameObject.Tags );
		}
	}

	internal void UpdateSceneObject ()
	{
		if ( !SceneObject.IsValid() ) return;

		SceneObject.RenderingEnabled = true;
		SceneObject.Flags.ExcludeGameLayer = CastShadows == ShadowRenderType.ShadowsOnly;
		SceneObject.Flags.CastShadows = CastShadows != ShadowRenderType.Off;

		if ( CurrentAnimation == null )
		{
			SceneObject.Transform = WorldTransform;
			SceneObject.RenderingEnabled = false;
			return;
		}

		AdvanceFrame();

		// var texture = Texture.Load(FileSystem.Mounted, CurrentAnimation.Frames[CurrentFrameIndex].FilePath);
		// if (texture is not null)
		//     SpriteMaterial.Set("Texture", texture);

		// Add pivot to transform
		var pos = WorldPosition;
		var rot = WorldRotation * _rotationOffset;
		var scale = WorldScale * new Vector3( 1f, 1f * ( CurrentTexture?.AspectRatio ?? 1f ), 1f );
		if ( UsePixelScale )
		{
			var _frameSize = CurrentTexture?.FrameSize ?? new Vector2( 100, 100 );
			var scl = _frameSize.x < _frameSize.x ? _frameSize.y : _frameSize.y;
			scale *= ( new Vector3( scl, scl, 1f ) ) / 100f;
		}
		var origin = CurrentAnimation.Origin - new Vector2( 0.5f, 0.5f );
		pos -= new Vector3( origin.y, origin.x, 0 ) * 100f * scale;
		pos = pos.RotateAround( WorldPosition, rot );
		SceneObject.Transform = new Transform( pos, rot, scale );
	}

	internal void UpdateAttachments ()
	{
		if ( AttachPoints is not null && AttachPoints.Count > 0 )
		{
			foreach ( var attachment in AttachPoints )
			{
				var transform = GetAttachmentTransform( attachment.Key );

				attachment.Value.LocalPosition = transform.Position;
				attachment.Value.LocalRotation = transform.Rotation;
			}
		}
	}

	void ApplyMaterialOffset ()
	{
		if ( CurrentTexture is null ) return;
		if ( SpriteMaterial is null ) return;
		var offset = CurrentTexture.GetFrameOffset( CurrentFrameIndex );
		var tiling = CurrentTexture.GetFrameTiling();
		if ( _flipHorizontal )
		{
			offset.x = -offset.x - tiling.x;
		}
		if ( _flipVertical )
		{
			offset.y = -offset.y - tiling.y;
		}
		SpriteMaterial.Set( "g_vTiling", tiling );
		SpriteMaterial.Set( "g_vOffset", offset );
		UpdateAttachments();
	}

	void ApplySpriteFlags ()
	{
		_flipHorizontal = _spriteFlags.HasFlag( SpriteFlags.HorizontalFlip );
		_flipVertical = _spriteFlags.HasFlag( SpriteFlags.VerticalFlip );
		var targetModel = _spriteFlags.HasFlag( SpriteFlags.DrawBackface ) ? "models/sprite_quad_2_sided.vmdl" : "models/sprite_quad_1_sided.vmdl";
		if ( SceneObject is not null && SceneObject.Model.ResourcePath != targetModel )
			SceneObject.Model = Model.Load( targetModel );
		ApplyMaterialOffset();
	}

	void AdvanceFrame ()
	{
		var frameCount = CurrentAnimation.Frames.Count;
		if ( frameCount <= 1 ) return;
		var currentPlayback = PlaybackSpeed * ( _isPingPonging ? -1 : 1 );
		var frameRate = ( 1f / ( ( currentPlayback == 0 ) ? 0 : ( CurrentAnimation.FrameRate * Math.Abs( currentPlayback ) ) ) );
		_timeSinceLastFrame += ( ( Game.IsPlaying ) ? Time.Delta : RealTime.Delta );
		if ( _timeSinceLastFrame < frameRate ) return;
		if ( !( CurrentAnimation.LoopMode != SpriteResource.LoopMode.None || ( currentPlayback > 0 && CurrentFrameIndex < frameCount - 1 ) || ( currentPlayback < 0 && CurrentFrameIndex > 0 ) ) ) return;

		var loopStart = CurrentAnimation.GetLoopStart();
		var loopEnd = CurrentAnimation.GetLoopEnd();
		if ( currentPlayback > 0 )
		{
			var frame = CurrentFrameIndex;
			frame++;
			if ( frame > loopEnd && CurrentAnimation.LoopMode != SpriteResource.LoopMode.None )
			{
				switch ( CurrentAnimation.LoopMode )
				{
					case SpriteResource.LoopMode.PingPong:
						_isPingPonging = !_isPingPonging;
						frame = Math.Max( loopEnd - 1, loopStart );
						break;
					case SpriteResource.LoopMode.Forward:
						_isPingPonging = false;
						frame = loopStart;
						break;
				}
			}
			else if ( frame >= frameCount - 1 && Game.IsPlaying )
			{
				_queuedAnimations.Add( CurrentAnimation.Name );
			}
			CurrentFrameIndex = frame;
		}
		else if ( currentPlayback < 0 )
		{
			var frame = CurrentFrameIndex;
			frame--;
			if ( frame < loopStart && CurrentAnimation.LoopMode != SpriteResource.LoopMode.None )
			{
				switch ( CurrentAnimation.LoopMode )
				{
					case SpriteResource.LoopMode.PingPong:
						_isPingPonging = !_isPingPonging;
						frame = Math.Min( loopStart + 1, loopEnd );
						break;
					case SpriteResource.LoopMode.Forward:
						_isPingPonging = false;
						frame = loopEnd;
						break;
				}
			}
			else if ( frame <= 0 && Game.IsPlaying )
			{
				OnAnimationComplete?.Invoke( CurrentAnimation.Name );
			}
			CurrentFrameIndex = frame;
		}

		_timeSinceLastFrame = 0;
		var currentFrame = CurrentAnimation.Frames[CurrentFrameIndex];
		foreach ( var tag in currentFrame.Events )
		{
			QueueEvent( tag );
		}
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();
		SceneObject?.Delete();
		SceneObject = null;
	}

	internal void UpdateSprite ()
	{
		if ( Sprite == null )
		{
			BroadcastEvents.Clear();
			CurrentAnimation = null;
			return;
		}

		if ( SpriteMaterial is not null && CurrentTexture is not null )
		{
			SpriteMaterial.Set( "Texture", CurrentTexture );
			ApplyMaterialOffset();
			SceneObject.SetMaterialOverride( SpriteMaterial );
		}

		List<string> keysToRemove = BroadcastEvents.Keys.ToList();

		foreach ( var animation in Sprite.Animations )
		{
			foreach ( var frame in animation.Frames )
			{
				foreach ( var tag in frame.Events )
				{
					if ( keysToRemove.Contains( tag ) )
						keysToRemove.Remove( tag );
					if ( !BroadcastEvents.ContainsKey( tag ) )
						BroadcastEvents[tag] = ( _ ) => { };
				}
			}
		}


		foreach ( var key in keysToRemove )
		{
			BroadcastEvents.Remove( key );
		}
	}

	/// <summary>
	/// Get the global transform of an attachment point. Returns Transform.World if the attachment point does not exist.
	/// </summary>
	/// <param name="attachmentName">The name of the attach point</param>
	public Transform GetAttachmentTransform ( string attachmentName )
	{
		if ( AttachPoints.ContainsKey( attachmentName ) )
		{
			var ratio = CurrentTexture.AspectRatio;
			var attachPos = CurrentAnimation.GetAttachmentPosition( attachmentName, CurrentFrameIndex );
			var origin = CurrentAnimation.Origin - new Vector2( 0.5f, 0.5f );
			var rot = Rotation.Identity;
			var pos = ( new Vector3( attachPos.y, attachPos.x, 0 ) - ( Vector3.One.WithZ( 0 ) / 2f ) - new Vector3( origin.y, origin.x, 0 ) ) * 100f;
			pos *= new Vector3( 1f, ratio, 1f );
			pos = pos.RotateAround( Vector3.Zero, _rotationOffset );

			if ( SpriteFlags.HasFlag( SpriteFlags.HorizontalFlip ) ) rot *= Rotation.From( 180, 0, 0 );
			if ( SpriteFlags.HasFlag( SpriteFlags.VerticalFlip ) ) rot *= Rotation.From( 0, 0, 180 );
			pos = pos.RotateAround( origin / 2f * new Vector2( 100, 100 * ratio ), rot );

			return new Transform( pos, rot, Vector3.One );
		}
		return Transform.World;
	}

	/// <summary>
	/// Plays an animation from the current Sprite by it's name.
	/// </summary>
	/// <param name="animationName">The name of the animation</param>
	/// <param name="force">Whether or not the animation should be forced. If true this will restart the animation from frame index 0 even if the specified animation is equal to the current animation.</param>
	public void PlayAnimation ( string animationName, bool force = false )
	{
		if ( Sprite == null ) return;
		if ( !force && string.Equals( _currentAnimation?.Name, animationName, StringComparison.OrdinalIgnoreCase ) ) return;

		var animation = Sprite.Animations.FirstOrDefault( a => string.Equals( a.Name, animationName, StringComparison.OrdinalIgnoreCase ) );
		if ( animation == null )
		{
			Log.Warning( $"Could not find animation \"{animationName}\" in sprite \"{Sprite.ResourceName}\"." );
			return;
		}

		_currentAnimation = animation;
		_currentFrameIndex = 0;
		_timeSinceLastFrame = 0;
		_isPingPonging = false;

		CurrentTexture = TextureAtlas.FromAnimation( animation );
		SpriteMaterial?.Set( "Texture", CurrentTexture );
		ApplyMaterialOffset();
	}

	List<string> _queuedEvents = new();
	List<string> _queuedAnimations = new();
	void QueueEvent ( string tag )
	{
		_queuedEvents.Add( tag );
	}

	internal void RunBroadcastQueue ()
	{
		if ( _queuedEvents.Count > 0 )
		{
			foreach ( var tag in _queuedEvents )
			{
				BroadcastEvent( tag );
			}
			_queuedEvents.Clear();
		}

		if ( _queuedAnimations.Count > 0 )
		{
			foreach ( var anim in _queuedAnimations )
			{
				OnAnimationComplete?.Invoke( anim );
			}
			_queuedAnimations.Clear();
		}
	}

	void BroadcastEvent ( string tag )
	{
		OnBroadcastEvent?.Invoke( tag );
		if ( BroadcastEvents.ContainsKey( tag ) )
			BroadcastEvents[tag]?.Invoke( this );
	}

	internal void BuildAttachPoints ()
	{
		if ( Sprite is null ) return;
		var attachments = Sprite.GetAttachmentNames();
		foreach ( var attachment in attachments )
		{
			var go = GameObject.Children.FirstOrDefault( x => x.Name == attachment );
			if ( go is null )
			{
				go = Scene.CreateObject();
				go.Parent = GameObject;
			}

			AttachPoints[attachment] = go;
			go.Flags |= GameObjectFlags.Bone;
			go.Name = attachment;
		}
	}

	public enum Axis
	{
		[Title( "+X" )]
		XPositive,
		[Title( "-X" )]
		XNegative,
		[Title( "+Y" )]
		YPositive,
		[Title( "-Y" )]
		YNegative,
		[Title( "+Z" )]
		ZPositive,
		[Title( "-Z" )]
		ZNegative
	}

	public enum ShadowRenderType
	{
		[Icon( "wb_shade" )]
		[Description( "Render the sprite with shadows (default)" )]
		On,
		[Icon( "wb_twilight" )]
		[Description( "Render the sprite without shadows" )]
		Off,
		[Icon( "hide_source" )]
		[Title( "Shadows Only" )]
		[Description( "Render ONLY the sprites shadows" )]
		ShadowsOnly
	}

	public class BroadcastControls { }
	public class AnimationNameAttribute : Attribute
	{
		public string Parameter { get; set; } = "Sprite";
	}

	public override int ComponentVersion => 1;

	[JsonUpgrader( typeof( SpriteComponent ), 1 )]
	static void Upgrader_v1 ( JsonObject json )
	{
		if ( !json.ContainsKey( "UpDirection" ) )
		{
			json["UpDirection"] = (int)Axis.XNegative;
		}
	}
}
