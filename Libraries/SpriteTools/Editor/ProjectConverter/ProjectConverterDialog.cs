using Editor;
using Sandbox;
using System.Collections.Generic;

namespace SpriteTools.ProjectConverter;

public class ProjectConverterDialog : Dialog
{
	List<SpriteResource2D> OutdatedSprites { get; set; } = new();

	public ProjectConverterDialog ( List<SpriteResource2D> outdatedSprites )
	{
		WindowTitle = "Sprite Tools Project Converter";
		Window.Title = WindowTitle;
		OutdatedSprites = outdatedSprites;
		Window.FixedSize = new Vector2( 640, 360 );

		Layout = Layout.Column();
		Layout.Margin = 8f;
		Layout.Spacing = 8f;

		Layout.Add( new Label( $"Your project has {OutdatedSprites.Count} outdated Sprite Resources.\n\nPlease select one of the upgrade paths below:" ) );

		{
			var rowWidget = Layout.Add( new Widget( this ) );
			rowWidget.Layout = Layout.Row();
			rowWidget.Layout.Margin = 8f;
			rowWidget.Layout.Spacing = 8f;
			rowWidget.HorizontalSizeMode = SizeMode.Flexible;
			rowWidget.FixedHeight = 256;

			{
				var panel = rowWidget.Layout.Add( new Widget( this ) );
				panel.SetStyles( "background-color: #222;" );
				panel.SetSizeMode( SizeMode.Flexible, SizeMode.Flexible );
				panel.Layout = Layout.Column();
				panel.Layout.Margin = 8;

				panel.Layout.Add( new Label( $"Convert to new Sprite Tools format" ) );

				var lblDesc = panel.Layout.Add( new Label( $"If you wish to continue using Sprite Tools sprites,\nselect this option.\n\n" +
					$"The sprite resource extension has changed from\n.sprite -> .spr to prevent conflicts with the new\nin-engine sprite resource." ) );
				lblDesc.Color = Color.Gray;

				panel.Layout.AddStretchCell( 1 );
				var btn = panel.Layout.Add( new Button.Primary( "Convert to new Sprite Tools format" ) );
				btn.Clicked += ConvertToNewFormat;
			}

			{
				var panel = rowWidget.Layout.Add( new Widget( this ) );
				panel.SetStyles( "background-color: #222;" );
				panel.SetSizeMode( SizeMode.Flexible, SizeMode.Flexible );
				panel.FixedWidth = 300;
				panel.Layout = Layout.Column();
				panel.Layout.Margin = 8;

				panel.Layout.Add( new Label( $"Convert to new in-engine SpriteResource" ) );

				var lblDesc = panel.Layout.Add( new Label( $"If you wish to use the new in-engine Sprite\nResource, select this option.\n\n" +
					$"TODO: Implement once we finalize API" ) );
				lblDesc.Color = Color.Gray;


				panel.Layout.AddStretchCell( 1 );
				var btn = panel.Layout.Add( new Button.Primary( "Convert to in-engine SpriteResource" ) );
				btn.Enabled = false; // TODO: Implement conversion logic
			}
		}


		Layout.AddStretchCell( 1 );
	}

	async void ConvertToNewFormat ()
	{
		using var progress = Progress.Start( "Updating to new Sprite Tools format" );

		int index = 0;
		foreach ( var sprite in OutdatedSprites )
		{
			var relativePath = sprite.ResourcePath;
			Progress.Update( relativePath, index, OutdatedSprites.Count );
			index++;

			var usedBy = AssetSystem.FindByPath( relativePath ).GetDependants( false );
			var assetsFolder = Project.Current.GetAssetsPath();
			var filePath = System.IO.Path.Combine( assetsFolder, relativePath );
			if ( !System.IO.File.Exists( filePath ) )
				continue;

			var jsonStr = await System.IO.File.ReadAllTextAsync( filePath );
			if ( string.IsNullOrWhiteSpace( jsonStr ) )
				continue;

			System.IO.File.Delete( filePath );
			if ( System.IO.File.Exists( filePath + "_c" ) )
			{
				System.IO.File.Delete( filePath + "_c" );
			}

			var newRelativePath = System.IO.Path.ChangeExtension( relativePath, ".spr" );
			var newFilePath = System.IO.Path.ChangeExtension( filePath, ".spr" );
			System.IO.File.WriteAllText( newFilePath, jsonStr );

			foreach ( var usingAsset in usedBy )
			{
				Progress.Update( $"Updating any references to {relativePath}", index, OutdatedSprites.Count );
				var file = usingAsset.GetSourceFile( true );
				Log.Info( file );
				if ( !System.IO.File.Exists( file ) )
					continue;

				var assetStr = await System.IO.File.ReadAllTextAsync( file );
				Log.Info( assetStr );
				assetStr = assetStr.Replace( relativePath, newRelativePath );
				assetStr = assetStr.Replace( relativePath + "_c", newRelativePath + "_c" );
				Log.Info( assetStr );
				await System.IO.File.WriteAllTextAsync( file, assetStr );
			}
		}
	}
}