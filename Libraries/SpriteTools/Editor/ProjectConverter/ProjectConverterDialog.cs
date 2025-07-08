using Editor;
using Sandbox;
using System.Collections.Generic;

namespace SpriteTools.ProjectConverter;

public class ProjectConverterDialog : Dialog
{
	List<Sandbox.SpriteResource> OutdatedSprites { get; set; } = new();

	public ProjectConverterDialog ( List<Sandbox.SpriteResource> outdatedSprites )
	{
		WindowTitle = "Sprite Tools Project Converter";
		Window.Title = WindowTitle;
		OutdatedSprites = outdatedSprites;
		Window.FixedSize = new Vector2( 640, 360 );

		Layout = Layout.Column();
		Layout.Margin = 8f;
		Layout.Spacing = 8f;

		Layout.Add( new Label( $"Your project has {OutdatedSprites.Count} outdated Sprite Resource(s).\n\nPlease select one of the upgrade paths below:" ) );

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

				var lblDesc = panel.Layout.Add( new Label( $"\nThe sprite resource extension has changed from\n.sprite -> .spr to prevent conflicts with the new\nin-engine sprite resource." ) );
				lblDesc.Color = Color.Gray;

				var lblWarn = panel.Layout.Add( new Label( $"\nIf your code loads any .sprite resources via a string\nyou may have to update those yourself as the code\nupgrader is going to miss any manually constructed\nstrings." ) );
				lblWarn.Color = Theme.Red;

				panel.Layout.AddStretchCell( 1 );

				var btn1 = panel.Layout.Add( new Button.Primary( "Update Sprite Resources .sprite -> .spr" ) );
				btn1.Clicked += () =>
				{
					ConvertToNewFormat();
					btn1.Enabled = false;
				};

				panel.Layout.AddSpacingCell( 4 );

				var btn2 = panel.Layout.Add( new Button.Primary( "Replace .sprite -> .spr file paths in Code" ) );
				btn2.Clicked += () =>
				{
					ConvertCodeToNewFormat();
					btn2.Enabled = false;
				};
			}

			{
				var panel = rowWidget.Layout.Add( new Widget( this ) );
				panel.SetStyles( "background-color: #222;" );
				panel.SetSizeMode( SizeMode.Flexible, SizeMode.Flexible );
				panel.FixedWidth = 300;
				panel.Layout = Layout.Column();
				panel.Layout.Margin = 8;

				panel.Layout.Add( new Label( $"Convert to new in-engine SpriteResource" ) );

				var lblDesc = panel.Layout.Add( new Label( $"\nTODO: Implement once we finalize API" ) );
				lblDesc.Color = Color.Gray;


				panel.Layout.AddStretchCell( 1 );
				var btn = panel.Layout.Add( new Button.Primary( "Convert to in-engine SpriteResource" ) );
				btn.Enabled = false; // TODO: Implement conversion logic
			}
		}


		Layout.AddStretchCell( 1 );
	}

	bool convertingToNewFormat = false;
	async void ConvertToNewFormat ()
	{
		if ( convertingToNewFormat ) return;

		using var progress = Progress.Start( "Updating to new Sprite Tools format" );
		convertingToNewFormat = true;

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

			convertingToNewFormat = false;
		}
	}

	async void ConvertCodeToNewFormat ()
	{
		if ( convertingToNewFormat ) return;

		using var progress = Progress.Start( "Updating code to new Sprite Tools format" );
		convertingToNewFormat = true;

		var codePath = Project.Current.GetCodePath();
		var codeFiles = System.IO.Directory.GetFiles( codePath, "*.cs", System.IO.SearchOption.AllDirectories );
		int index = 0;
		foreach ( var file in codeFiles )
		{
			Progress.Update( $"Updating file {file}", index, codeFiles.Length );
			index++;

			if ( !System.IO.File.Exists( file ) )
				continue;

			var codeStr = await System.IO.File.ReadAllTextAsync( file );
			if ( string.IsNullOrWhiteSpace( codeStr ) )
				continue;

			foreach ( var sprite in OutdatedSprites )
			{
				var relativePath = sprite.ResourcePath;
				var newRelativePath = System.IO.Path.ChangeExtension( relativePath, ".spr" );
				codeStr = codeStr.Replace( relativePath, newRelativePath );
				codeStr = codeStr.Replace( relativePath + "_c", newRelativePath + "_c" );
			}

			await System.IO.File.WriteAllTextAsync( file, codeStr );
		}

		convertingToNewFormat = false;
	}
}