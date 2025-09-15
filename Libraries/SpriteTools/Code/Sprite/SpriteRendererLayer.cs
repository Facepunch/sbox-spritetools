using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Hide]
[Category( "2D" )]
[Icon( "emoji_emotions" )]
[Title( "2D Sprite - LAYER" )]
[Tint( EditorTint.Yellow )]
public sealed class SpriteRendererLayer : Component, Component.ExecuteInEditor
{
	/// <summary>
	/// The sprite resource that this component uses.
	/// </summary>
	[Property]
	public Sprite Sprite
	{
		get => _sprite;
		set
		{
			_sprite = value;
			ApplySprite();
		}
	}
	Sprite _sprite;

	[Property]
	public SpriteComponent.Axis UpDirection
	{
		get => _upDirection;
		set
		{
			_upDirection = value;
			switch ( value )
			{
				case SpriteComponent.Axis.XPositive:
					_rotationOffset = Rotation.From( 0, 180, 0 );
					break;
				case SpriteComponent.Axis.XNegative:
					_rotationOffset = Rotation.From( 0, 0, 0 );
					break;
				case SpriteComponent.Axis.YPositive:
					_rotationOffset = Rotation.From( 0, -90, 0 );
					break;
				case SpriteComponent.Axis.YNegative:
					_rotationOffset = Rotation.From( 0, 90, 0 );
					break;
				case SpriteComponent.Axis.ZPositive:
					_rotationOffset = Rotation.From( 90, 0, 0 );
					break;
				case SpriteComponent.Axis.ZNegative:
					_rotationOffset = Rotation.From( -90, 0, 0 );
					break;
			}
			ApplyRotation();
		}
	}
	SpriteComponent.Axis _upDirection = SpriteComponent.Axis.YPositive;
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
			ApplyColor();
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
			ApplyColor();
		}
	}
	Color _flashTint = Color.White.WithAlpha( 0 );

	/// <summary>
	/// The playback speed of the animation.
	/// </summary>
	[Property]
	public float PlaybackSpeed
	{
		get => _playbackSpeed;
		set
		{
			_playbackSpeed = value;
			ApplyPlaybackSpeed();
		}
	}
	private float _playbackSpeed = 1.0f;

	/// <summary>
	/// Whether or not the object should scale based on the resolution of the Sprite.
	/// </summary>
	[Property] public bool UsePixelScale { get; set; } = false;

	/// <summary>
	/// Whether or not the sprite should render itself/its shadows.
	/// </summary>
	[Property]
	[Category( "Visuals" )]
	public SpriteComponent.ShadowRenderType CastShadows
	{
		get => _castShadows;
		set
		{
			_castShadows = value;
			ApplyShadows();
		}
	}
	SpriteComponent.ShadowRenderType _castShadows = SpriteComponent.ShadowRenderType.On;

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
	public Sprite.Animation CurrentAnimation
	{
		get => _spriteRenderer?.CurrentAnimation;
		set
		{
			if ( _spriteRenderer.IsValid() )
			{
				PlayAnimation( value.Name );
			}
		}
	}

	[Property, Category( "Sprite" ), Title( "Current Animation" ), Editor( "sprite_animation_name" )]
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

	public BBox Bounds
	{
		get
		{
			var size = new Vector2( 50, 50 );
			BBox bbox = new BBox( new Vector3( -size.x, -size.y, -0.1f ), new Vector3( size.x, size.y, 0.1f ) );
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
		get => _spriteRenderer?.CurrentFrameIndex ?? 0;
		set
		{
			if ( _spriteRenderer.IsValid() )
			{
				_spriteRenderer.CurrentFrameIndex = value;
			}
		}
	}

	private SpriteRenderer _spriteRenderer;

	protected override void OnAwake ()
	{
		base.OnAwake();

		if ( !_spriteRenderer.IsValid() )
		{
			CreateSpriteRenderer();
		}
	}

	//protected override void OnStart ()
	//{
	//	base.OnStart();

	//	if ( Sprite is null ) return;
	//	if ( Sprite.Animations.Count > 0 )
	//	{
	//		var anim = Sprite.Animations.FirstOrDefault( x => x.Name.ToLowerInvariant() == StartingAnimationName );
	//		if ( anim is null )
	//			anim = Sprite.Animations.FirstOrDefault();
	//		PlayAnimation( anim.Name );
	//	}

	//	if ( SpriteMaterial is null )
	//	{
	//		if ( MaterialOverride != null )
	//			SpriteMaterial = MaterialOverride.CreateCopy();
	//		else
	//			SpriteMaterial = Material.Create( "spritemat", "shaders/sprite_2d.shader" );

	//		SpriteMaterial?.Set( "g_vFlashColor", _flashTint );
	//		SpriteMaterial?.Set( "g_flFlashAmount", _flashTint.a );
	//	}

	//	UpdateSprite();
	//	UpdateSceneObject();
	//	ApplySpriteFlags();
	//	FlashTint = _flashTint;
	//}

	protected override void OnEnabled ()
	{
		base.OnEnabled();

		if ( _spriteRenderer.IsValid() )
		{
			_spriteRenderer.Enabled = true;
		}
	}

	protected override void OnDisabled ()
	{
		base.OnDisabled();

		if ( _spriteRenderer.IsValid() )
		{
			_spriteRenderer.Enabled = false;
		}
	}

	protected override void OnDestroy ()
	{
		base.OnDestroy();

		if ( _spriteRenderer.IsValid() )
		{
			_spriteRenderer.GameObject?.Destroy();
			_spriteRenderer = null;
		}
	}

	protected override void OnPreRender ()
	{
		base.OnPreRender();

		if ( LocalScale.z == 1 && LocalScale.y != 1 )
		{
			LocalScale = new Vector3( LocalScale.x, LocalScale.y, LocalScale.y );
		}
		else if ( LocalScale.x == 1 && LocalScale.z == 1 && LocalScale.y != 1 )
		{
			LocalScale = new Vector3( LocalScale.y, LocalScale.y, LocalScale.y );
		}
	}

	protected override void DrawGizmos ()
	{
		base.DrawGizmos();
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

	private void ApplySprite ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		_spriteRenderer.Sprite = _sprite;
	}

	private void ApplyColor ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		var color = _tint;
		if ( _flashTint.a > 0 )
		{
			var intensity = _flashTint.a * 1000;
			color = Color.Lerp( color, _flashTint.WithAlpha( color.a ), _flashTint.a );
			color.r *= intensity;
			color.g *= intensity;
			color.b *= intensity;
			color.a = intensity;
		}
		_spriteRenderer.Color = color;
	}

	private void ApplySpriteFlags ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		_spriteRenderer.FlipHorizontal = _spriteFlags.HasFlag( SpriteFlags.HorizontalFlip );
		_spriteRenderer.FlipVertical = _spriteFlags.HasFlag( SpriteFlags.VerticalFlip );
	}

	private void ApplyRotation ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		_spriteRenderer.LocalRotation = _rotationOffset * new Angles( -90, 0, 0 );
	}

	private void ApplyShadows ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		_spriteRenderer.Shadows = _castShadows != SpriteComponent.ShadowRenderType.Off;
	}

	private void ApplyPlaybackSpeed ()
	{
		if ( !_spriteRenderer.IsValid() )
			return;

		_spriteRenderer.PlaybackSpeed = _playbackSpeed;
	}

	private void CreateSpriteRenderer ()
	{
		var childObject = new GameObject();
		childObject.Parent = GameObject;
		childObject.Flags |= GameObjectFlags.NotSaved | GameObjectFlags.Hidden;
		_spriteRenderer = childObject.AddComponent<SpriteRenderer>();
		_spriteRenderer.Billboard = SpriteRenderer.BillboardMode.None;
		_spriteRenderer.TextureFilter = Sandbox.Rendering.FilterMode.Point;
		_spriteRenderer.Enabled = false;
		_spriteRenderer.Size = 100;
		_spriteRenderer.IsSorted = true;
		_spriteRenderer.Shadows = false;

		ApplySprite();
		ApplyColor();
		ApplySpriteFlags();
		ApplyRotation();
		ApplyShadows();
		ApplyPlaybackSpeed();
	}

	/// <summary>
	/// Plays an animation from the current Sprite by it's name.
	/// </summary>
	/// <param name="animationName">The name of the animation</param>
	/// <param name="force">Whether or not the animation should be forced. If true this will restart the animation from frame index 0 even if the specified animation is equal to the current animation.</param>
	public void PlayAnimation ( string animationName, bool force = false )
	{
		if ( _spriteRenderer?.CurrentAnimation?.Name == animationName )
			return;

		_spriteRenderer?.PlayAnimation( animationName );
	}
}
