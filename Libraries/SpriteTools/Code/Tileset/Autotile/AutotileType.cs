namespace SpriteTools;

public enum AutotileType
{
    [Title("2x2 Set"), Icon("add")]
    [Description("(15 Tiles)")]
    Bitmask2x2Edge = 0,

    // [Title("2x2 Corner Set"), Icon("close")]
    // [Description("(15 Tiles) NOT IMPLEMENTED")]
    // Bitmask2x2Corner = 1,

    [Title("3x3 Minimal Set"), Icon("grid_3x3")]
    [Description("(47 Tiles)")]
    Bitmask3x3 = 2,

    [Title("3x3 Complete Set"), Icon("grid_on")]
    [Description("(255 Tiles)")]
    Bitmask3x3Complete = 3
}