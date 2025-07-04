using Editor;
using Sandbox;
using SpriteTools.ProjectConverter;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools;

public static class ToolbarMenuOptions
{

	[Menu( "Editor", "Sprite Tools/Flush Texture Cache" )]
	public static void FlushTextureCache ()
	{
		TextureAtlas.ClearCache();
		TileAtlas.ClearCache();
		PixmapCache.ClearCache();
	}

	[Menu( "Editor", "Sprite Tools/Convert Project" )]
	public static void OpenConverterWindow ()
	{
		List<SpriteResource2D> outdatedSprites = new();
		foreach ( var spriteResource in ResourceLibrary.GetAll<Sandbox.SpriteResource2D>() )
		{
			var jsonStr = Editor.FileSystem.Content.ReadAllText( spriteResource.ResourcePath );
			if ( string.IsNullOrWhiteSpace( jsonStr ) )
				continue;

			var json = Json.ParseToJsonObject( jsonStr );
			if ( json.TryGetPropertyValue( "Animations", out var animationsNode ) )
			{
				foreach ( var animEntry in animationsNode.AsArray() )
				{
					if ( animEntry.AsObject().TryGetPropertyValue( "Frames", out var framesNode ) && framesNode is not null )
					{
						if ( framesNode.AsArray().Any( x => x.AsObject().TryGetPropertyValue( "FilePath", out var _ ) ) )
						{
							outdatedSprites.Add( spriteResource );
						}
					}
				}
			}
		}

		if ( outdatedSprites.Count == 0 )
		{
			Log.Info( "No outdated sprites found, nothing to convert." );
			return;
		}

		var dialog = new ProjectConverterDialog( outdatedSprites );
		dialog.Show();
		dialog.Raise();
	}


	[Event( "editor.created" )]
	[Event( "package.changed" )]
	static async void OnEditorCreated ( EditorMainWindow mainWindow )
	{
		// Give the user a sec to breathe
		await Task.Delay( 2500 );

		// Automatically open the converter window if the project needs an upgrade
		OpenConverterWindow();
	}

}