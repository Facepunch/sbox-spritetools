using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class ImportSettings
{
    [Property, Group("Frame Count")] public int NumberOfFrames { get; set; } = 1;
    [Property, Group("Frame Count")] public int FramesPerRow { get; set; } = 1;
    [Property, Group("Frame Size")] public int FrameWidth { get; set; } = 32;
    [Property, Group("Frame Size")] public int FrameHeight { get; set; } = 32;
    [Property, Group("Cell Offset")] public int HorizontalCellOffset { get; set; } = 0;
    [Property, Group("Cell Offset")] public int VerticalCellOffset { get; set; } = 0;
    [Property, Group("Pixel Offset")] public int HorizontalPixelOffset { get; set; } = 0;
    [Property, Group("Pixel Offset")] public int VerticalPixelOffset { get; set; } = 0;
    [Property, Group("Separation")] public int HorizontalSeparation { get; set; } = 0;
    [Property, Group("Separation")] public int VerticalSeparation { get; set; } = 0;
}