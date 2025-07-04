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
		OutdatedSprites = outdatedSprites;

		Layout = Layout.Column();
		Layout.Margin = 8f;
		Layout.Spacing = 8f;

		Layout.Add( new Label( OutdatedSprites.Count.ToString() ) );
	}
}