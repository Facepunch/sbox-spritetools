using Editor;
using Sandbox;
using System;
using System.Collections.Generic;

namespace SpriteTools.SpritesheetImporter;

public class ImportSettings
{
	[Property, Group( "Frame Count" ), Range( 1, 1000, true, false ), Step( 1 )] public int NumberOfFrames { get; set; } = 1;
	[Property, Group( "Frame Count" ), Range( 1, 1000, true, false ), Step( 1 )] public int FramesPerRow { get; set; } = 1;
	[Property, Group( "Frame Size" ), Range( 1, 99999, true, false ), Step( 1 )] public int FrameWidth { get; set; } = 32;
	[Property, Group( "Frame Size" ), Range( 1, 99999, true, false ), Step( 1 )] public int FrameHeight { get; set; } = 32;
	[Property, Group( "Cell Offset" )] public int HorizontalCellOffset { get; set; } = 0;
	[Property, Group( "Cell Offset" )] public int VerticalCellOffset { get; set; } = 0;
	[Property, Group( "Pixel Offset" )] public int HorizontalPixelOffset { get; set; } = 0;
	[Property, Group( "Pixel Offset" )] public int VerticalPixelOffset { get; set; } = 0;
	[Property, Group( "Separation" ), Range( 0, 99999, true, false ), Step( 1 )] public int HorizontalSeparation { get; set; } = 0;
	[Property, Group( "Separation" ), Range( 0, 99999, true, false ), Step( 1 )] public int VerticalSeparation { get; set; } = 0;

	public List<Rect> GetFrames ()
	{
		var frames = new List<Rect>();

		for ( int i = 0; i < NumberOfFrames; i++ )
		{
			var x = ( i % FramesPerRow ) * ( FrameWidth + HorizontalSeparation ) + HorizontalPixelOffset + FrameWidth * HorizontalCellOffset;
			var y = ( i / FramesPerRow ) * ( FrameHeight + VerticalSeparation ) + VerticalPixelOffset + FrameHeight * VerticalCellOffset;
			frames.Add( new Rect( x, y, FrameWidth, FrameHeight ) );
		}

		return frames;
	}
}