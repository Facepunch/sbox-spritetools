using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpritesheetImporter;

public class ImportSettings
{
    [Property, Group("Frame Count"), Range(1, 1000, 1, true, false)] public int NumberOfFrames { get; set; } = 1;
    [Property, Group("Frame Count"), Range(1, 1000, 1, true, false)] public int FramesPerRow { get; set; } = 1;
    [Property, Group("Frame Size"), Range(1, 99999, 1, true, false)] public int FrameWidth { get; set; } = 32;
    [Property, Group("Frame Size"), Range(1, 99999, 1, true, false)] public int FrameHeight { get; set; } = 32;
    [Property, Group("Cell Offset")] public int HorizontalCellOffset { get; set; } = 0;
    [Property, Group("Cell Offset")] public int VerticalCellOffset { get; set; } = 0;
    [Property, Group("Pixel Offset")] public int HorizontalPixelOffset { get; set; } = 0;
    [Property, Group("Pixel Offset")] public int VerticalPixelOffset { get; set; } = 0;
    [Property, Group("Separation"), Range(0, 99999, 1, true, false)] public int HorizontalSeparation { get; set; } = 0;
    [Property, Group("Separation"), Range(0, 99999, 1, true, false)] public int VerticalSeparation { get; set; } = 0;
}