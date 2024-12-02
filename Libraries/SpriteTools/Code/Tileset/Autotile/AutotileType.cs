namespace SpriteTools;

public enum AutotileType
{
    [Title("2x2 Edge Set"), Icon("add")]
    [Description("(15 Tiles) NOT IMPLEMENTED")]
    Bitmask2x2Edge,

    [Title("2x2 Corner Set"), Icon("close")]
    [Description("(15 Tiles) NOT IMPLEMENTED")]
    Bitmask2x2Corner,

    [Title("3x3 Minimal"), Icon("grid_3x3")]
    [Description("(47 Tiles)")]
    Bitmask3x3,

    [Title("3x3 Complete"), Icon("grid_on")]
    [Description("(255 Tiles) NOT IMPLEMENTED")]
    Bitmask3x3Complete
}