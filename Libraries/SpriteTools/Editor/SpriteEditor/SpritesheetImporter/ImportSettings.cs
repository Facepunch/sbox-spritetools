using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.SpritesheetImporter;

public class ImportSettings
{
    [Property] public int NumberOfFrames { get; set; } = 1;
    [Property] public int FramesPerRow { get; set; } = 1;
}