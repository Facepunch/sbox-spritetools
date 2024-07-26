using System.Collections.Generic;
using System.Runtime.InteropServices;
using Editor;
using Sandbox;

namespace SpriteTools;

public static class PixmapCache
{
    static Dictionary<string, Pixmap> _cache = new Dictionary<string, Pixmap>();

    public static Pixmap Get(string filePath, Rect rect)
    {
        var key = filePath + "?" + rect.ToString();
        if (_cache.TryGetValue(key, out var cachedPixmap))
        {
            return cachedPixmap;
        }

        var texture = Texture.Load(Sandbox.FileSystem.Mounted, filePath);
        if (rect.Width == 0 || rect.Height == 0)
        {
            rect = new Rect(0, 0, texture.Width, texture.Height);
        }

        var pixmap = new Pixmap((int)rect.Width, (int)rect.Height);
        var pixels = texture.GetPixels();
        List<Color32> span = new();
        for (int y = (int)rect.Top; y < (int)rect.Bottom; y++)
        {
            for (int x = (int)rect.Left; x < (int)rect.Right; x++)
            {
                var i = x + y * texture.Width;
                span.Add(new Color32(pixels[i].b, pixels[i].g, pixels[i].r, pixels[i].a));
            }
        }
        pixmap.UpdateFromPixels(MemoryMarshal.AsBytes<Color32>(span.ToArray()), (int)rect.Width, (int)rect.Height);
        _cache[key] = pixmap;
        return pixmap;
    }

    public static void ClearCache()
    {
        _cache.Clear();
    }
}