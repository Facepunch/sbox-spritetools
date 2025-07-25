using Editor;
using Sandbox;
using SpriteTools.ProjectConverter;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
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
		List<string> outdatedSprites = new();

		// Loop over all files names .sprite in the project since ResourceLibrary.GetAll<Sprite>() might not catch all of them
		var allFiles = Editor.FileSystem.Content.FindFile( "", "*.sprite", true );
		foreach ( var file in allFiles )
		{
			var jsonStr = Editor.FileSystem.Content.ReadAllText( file );
			if ( string.IsNullOrWhiteSpace( jsonStr ) )
				continue;

			var json = Json.ParseToJsonObject( jsonStr );
			if ( json.TryGetPropertyValue( "Animations", out var animationsNode ) )
			{
				foreach ( var animEntry in animationsNode.AsArray() )
				{
					if ( animEntry.AsObject().TryGetPropertyValue( "Frames", out var framesNode ) && framesNode is not null )
					{
						if ( framesNode.AsArray().Any( x => x is JsonObject && x.AsObject().TryGetPropertyValue( "FilePath", out var _ ) ) )
						{
							outdatedSprites.Add( file );
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
		dialog.HasMaximizeButton = false;
		dialog.Window.HasMaximizeButton = false;
		dialog.Show();
		dialog.Raise();
	}


	[Event( "editor.created" )]
	[Event( "package.changed" )]
	static async void OnEditorCreated ( EditorMainWindow mainWindow )
	{
		// Give the user a sec to breathe
		await Task.Delay( 5000 );

		// Automatically open the converter window if the project needs an upgrade
		OpenConverterWindow();
	}

}
