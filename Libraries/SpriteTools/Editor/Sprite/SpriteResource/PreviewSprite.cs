using Editor;
using Editor.Assets;
using Sandbox;
using System;
using System.Threading.Tasks;

namespace SpriteTools;

[AssetPreview( "sprite" )]
class PreviewSprite : AssetPreview
{
	SpriteResource spriteResource;
	SpriteComponent spriteComponent;

	public override bool IsAnimatedPreview => true;

	public PreviewSprite ( Asset asset ) : base( asset )
	{
		if ( asset.TryLoadResource<SpriteResource>( out var sprite ) )
		{
			spriteResource = sprite;
		}
	}

	public override Task InitializeAsset ()
	{
		using ( Scene.Push() )
		{
			PrimaryObject = new GameObject();
			PrimaryObject.WorldTransform = Transform.Zero;

			Camera.WorldRotation = Rotation.Identity;
			Camera.WorldPosition = Vector3.Forward * -200;
			Camera.Orthographic = true;
			Camera.OrthographicHeight = 50;

			spriteComponent = PrimaryObject.AddComponent<SpriteComponent>();
			spriteComponent.Sprite = spriteResource;
			spriteComponent.UpDirection = SpriteComponent.Axis.ZPositive;
			spriteComponent.WorldRotation = new Angles( 0, 180, 0 );
			spriteComponent.UsePixelScale = true;

			UpdateCamera();
		}

		return Task.CompletedTask;
	}

	public override void UpdateScene ( float cycle, float timeStep )
	{
		using ( Scene.Push() )
		{
			UpdateCamera();
		}

		TickScene( timeStep );
	}

	void UpdateCamera ()
	{
		if ( !spriteComponent.IsValid() )
			return;

		var _imageSize = spriteComponent?.CurrentTexture?.FrameSize ?? 50;
		var _origin = ( ( spriteComponent?.CurrentAnimation?.Origin ?? Vector2.Zero ) - 0.5f ) * _imageSize;
		Camera.WorldPosition = ( Vector3.Forward * -200 ) + new Vector3( _origin.x, 0, _origin.y );

		var _s = MathF.Max( _imageSize.x, _imageSize.y );
		Camera.OrthographicHeight = ( _s > 0 ) ? _s : 50;
	}

	public void SetAnimation ( string name )
	{
		if ( !spriteComponent.IsValid() ) return;
		if ( string.IsNullOrEmpty( name ) ) return;
		spriteComponent.PlayAnimation( name );
	}

}
