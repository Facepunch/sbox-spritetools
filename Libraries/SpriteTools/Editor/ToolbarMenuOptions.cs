using Sandbox;
using Editor;
using System.Threading.Tasks;

namespace SpriteTools;

public static class ToolbarMenuOptions
{

    [Menu("Editor", "Sprite Tools/Flush Texture Cache")]
    public static void FlushTextureCache()
    {
        TextureAtlas.ClearCache();
        TileAtlas.ClearCache();
        PixmapCache.ClearCache();
    }

}