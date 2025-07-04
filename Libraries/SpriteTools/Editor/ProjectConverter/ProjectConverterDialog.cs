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
}