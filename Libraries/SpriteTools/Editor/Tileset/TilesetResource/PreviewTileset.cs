using Editor;
using Editor.Assets;
using Sandbox;
using System.Threading.Tasks;

namespace SpriteTools;

[AssetPreview( "tileset" )]
class PreviewTileset : AssetPreview
{
	internal Texture texture;

	public override bool IsAnimatedPreview => false;

	public PreviewTileset ( Asset asset ) : base( asset )
	{
		if ( !asset.TryLoadResource<TilesetResource>( out var tileset ) )
			return;

		var filePath = tileset?.FilePath;
		if ( filePath is null || !Editor.FileSystem.Content.FileExists( filePath ) )
			return;

		var image = Texture.LoadFromFileSystem( tileset?.FilePath, Editor.FileSystem.Content );
		if ( image is not null )
		{
			texture = image;
		}
	}

	public override Task InitializeAsset ()
	{
		using ( Scene.Push() )
		{
			PrimaryObject = new GameObject();
			PrimaryObject.WorldTransform = Transform.Zero;

			if ( texture is not null )
			{
				var sprite = PrimaryObject.AddComponent<SpriteRenderer>();
				sprite.Texture = texture;

				var aspect = (float)texture.Width / (float)texture.Height;
				sprite.Size = new Vector2( 16 * aspect, 16 );

				if ( aspect > 1 )
				{
					sprite.Size = new Vector2( 16, 16 / aspect );
				}
			}

			Camera.Orthographic = true;
			Camera.OrthographicHeight = 16;
		}

		return Task.CompletedTask;
	}

	public override void UpdateScene ( float cycle, float timeStep )
	{
		base.UpdateScene( cycle, timeStep );

		Camera.Orthographic = true;
		Camera.OrthographicHeight = 16;
		Camera.WorldPosition = Vector3.Forward * -200;
		Camera.WorldRotation = Rotation.LookAt( Vector3.Forward );
	}
}
