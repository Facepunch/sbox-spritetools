using System;

[Flags]
public enum SpriteFlags
{
    None = 0,
    [Icon("align_horizontal_center")]
    [Title("Horizontal Flip")]
    [Description("Flip the sprite horizontally")]
    HorizontalFlip = 1 << 2,
    [Icon("align_vertical_center")]
    [Title("Vertical Flip")]
    [Description("Flip the sprite vertically")]
    VerticalFlip = 1 << 3,
}