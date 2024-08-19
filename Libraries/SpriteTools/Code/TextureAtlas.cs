using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SpriteTools;

/// <summary>
/// A class that combines multiple textures into a single texture.
/// </summary>
public class TextureAtlas
{
    public int Size { get; private set; } = 1;

    Texture Texture;
    Vector2 MaxFrameSize = Vector2.Zero;
    Dictionary<int, Texture> FrameCache = new();
    static Dictionary<string, TextureAtlas> Cache = new();

    /// <summary>
    /// Returns the aspect ratio of a frame from the texture atlas.
    /// </summary>
    public float AspectRatio => (MaxFrameSize.y == 0) ? ((Texture.Height == 0) ? 1 : ((float)Texture.Width / Texture.Height)) : (MaxFrameSize.x / MaxFrameSize.y);

    /// <summary>
    /// Returns the UV tiling for the texture atlas.
    /// </summary>
    public Vector2 GetFrameTiling()
    {
        if (MaxFrameSize.x == 0 || MaxFrameSize.y == 0)
        {
            return Vector2.One;
        }

        // inset by 1 pixel to avoid bleeding
        return (MaxFrameSize - Vector2.One * 2f) / (MaxFrameSize * (float)Size);
    }

    /// <summary>
    /// Returns the UV offset for a specific frame in the texture atlas.
    /// </summary>
    /// <param name="index">The index of the frame</param>
    public Vector2 GetFrameOffset(int index)
    {
        if (MaxFrameSize.x == 0 || MaxFrameSize.y == 0)
        {
            return Vector2.Zero;
        }

        int x = index * (int)MaxFrameSize.x % (Size * (int)MaxFrameSize.x);
        int y = index * (int)MaxFrameSize.y / (Size * (int)MaxFrameSize.y) * (int)MaxFrameSize.y;
        x += 1;
        y += 1;
        return new Vector2(x, y) / (Size * MaxFrameSize);
    }

    public Texture GetTextureFromFrame(int index)
    {
        if (FrameCache.TryGetValue(index, out var cachedTexture))
        {
            return cachedTexture;
        }

        int x = index * (int)MaxFrameSize.x % (Size * (int)MaxFrameSize.x);
        int y = index * (int)MaxFrameSize.y / (Size * (int)MaxFrameSize.y) * (int)MaxFrameSize.y;
        x += 1;
        y += 1;
        byte[] textureData = new byte[(int)(MaxFrameSize.x * MaxFrameSize.y * 4)];
        for (int i = 0; i < MaxFrameSize.x; i++)
        {
            for (int j = 0; j < MaxFrameSize.y; j++)
            {
                var ind = (i + j * (int)MaxFrameSize.x) * 4;
                var color = Texture.GetPixel(x + i, y + j);
                textureData[ind + 0] = color.r;
                textureData[ind + 1] = color.g;
                textureData[ind + 2] = color.b;
                textureData[ind + 3] = color.a;
            }
        }

        var builder = Texture.Create((int)MaxFrameSize.x, (int)MaxFrameSize.y);
        builder.WithData(textureData);
        builder.WithMips(0);
        builder.WithMultisample(0);
        var texture = builder.Finish();
        FrameCache[index] = texture;
        return texture;
    }

    // Cast to texture
    public static implicit operator Texture(TextureAtlas atlas)
    {
        return atlas?.Texture ?? null;
    }


    //////////////////////////// STATIC METHODS //////////////////////////// 

    /// <summary>
    /// Returns a cached texture atlas given a sprite animation. Creates one if not in the cache. Returns null if there was an error and the atlas could not be created.
    /// </summary>
    /// <param name="animation">The sprite animation to create the atlas from</param>
    public static TextureAtlas FromAnimation(SpriteAnimation animation)
    {
        var key = "anim." + animation.Name + ".";
        foreach (var frame in animation.Frames)
        {
            key += frame.FilePath + frame.SpriteSheetRect.ToString() + ".";
        }
        if (Cache.TryGetValue(key, out var cachedAtlas))
        {
            return cachedAtlas;
        }

        var atlas = new TextureAtlas();
        atlas.Size = (int)Math.Ceiling(Math.Sqrt(animation.Frames.Count));

        if (animation.Frames.Count == 1)
        {
            var frame = animation.Frames[0];
            if (frame is null) return null;
            if (frame.SpriteSheetRect.Width == 0 && frame.SpriteSheetRect.Height == 0)
            {
                if (!FileSystem.Mounted.FileExists(frame.FilePath))
                {
                    Log.Error($"TextureAtlas: Texture file not found: {frame.FilePath}");
                    return null;
                }

                var texture = Texture.Load(FileSystem.Mounted, frame.FilePath);
                atlas.Texture = texture;
                return atlas;
            }
        }

        List<(Texture, Rect)> textures = new();
        atlas.MaxFrameSize = 0;
        foreach (var frame in animation.Frames)
        {
            if (frame is null) continue;
            if (!FileSystem.Mounted.FileExists(frame.FilePath))
            {
                Log.Error($"TextureAtlas: Texture file not found: {frame.FilePath}");
                continue;
            }
            var texture = Texture.Load(FileSystem.Mounted, frame.FilePath);
            var rect = frame.SpriteSheetRect;
            if (rect.Width == 0 || rect.Height == 0)
            {
                rect = new Rect(0, 0, texture.Width, texture.Height);
            }
            textures.Add((texture, rect));
            atlas.MaxFrameSize = new Vector2(
                Math.Max(atlas.MaxFrameSize.x, Math.Max(rect.Width, atlas.MaxFrameSize.x)),
                Math.Max(atlas.MaxFrameSize.y, Math.Max(rect.Height, atlas.MaxFrameSize.y))
            );
        }
        atlas.MaxFrameSize += 2;

        Vector2 imageSize = atlas.Size * atlas.MaxFrameSize;
        int x = 0;
        int y = 0;
        int size = (int)(imageSize.x * imageSize.y * 4);
        if (size == 0)
        {
            return null;
        }
        byte[] textureData = new byte[size];
        foreach (var (texture, rect) in textures)
        {
            if (x + rect.Width > imageSize.x)
            {
                x = 0;
                y += (int)atlas.MaxFrameSize.y;
            }
            if (y + rect.Height > imageSize.y)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            try
            {

                var pixels = texture.GetPixels();

                for (int i = 0; i < rect.Width; i++)
                {
                    for (int j = 0; j < rect.Height; j++)
                    {
                        var index = (x + 1 + i + (y + 1 + j) * (int)imageSize.x) * 4;
                        var textureIndex = (int)(rect.Left + i + (rect.Top + j) * texture.Width);
                        textureData[index] = pixels[textureIndex].r;
                        textureData[index + 1] = pixels[textureIndex].g;
                        textureData[index + 2] = pixels[textureIndex].b;
                        textureData[index + 3] = pixels[textureIndex].a;
                    }
                }
            }
            catch (Exception e) { Log.Info(e); }

            x += (int)atlas.MaxFrameSize.x;
        }

        var builder = Texture.Create((int)imageSize.x, (int)imageSize.y);
        builder.WithData(textureData);
        builder.WithMips(0);
        atlas.Texture = builder.Finish();

        Cache[key] = atlas;

        return atlas;
    }

    /// <summary>
    /// Returns a cached texture atlas given a list of texture paths. Creates one if not in the cache. Returns null if there was an error and the atlas could not be created.
    /// </summary>
    /// <param name="texturePaths">A list containing a path to each frame</param>
    public static TextureAtlas FromTextures(List<string> texturePaths)
    {
        var key = string.Join(",", texturePaths.OrderBy(x => x));
        if (Cache.TryGetValue(key, out var cachedAtlas))
        {
            return cachedAtlas;
        }

        var atlas = new TextureAtlas();
        atlas.Size = (int)Math.Ceiling(Math.Sqrt(texturePaths.Count));

        List<Texture> textures = new();
        atlas.MaxFrameSize = 0;
        foreach (var path in texturePaths)
        {
            if (!FileSystem.Mounted.FileExists(path))
            {
                Log.Error($"TextureAtlas: Texture file not found: {path}");
                continue;
            }
            var texture = Texture.Load(FileSystem.Mounted, path);
            textures.Add(texture);
            atlas.MaxFrameSize = new Vector2(
                Math.Max(atlas.MaxFrameSize.x, texture.Width),
                Math.Max(atlas.MaxFrameSize.y, texture.Height)
            );
        }
        atlas.MaxFrameSize += 2;

        Vector2 imageSize = atlas.Size * atlas.MaxFrameSize;
        int x = 0;
        int y = 0;
        byte[] textureData = new byte[(int)(imageSize.x * imageSize.y * 4)];
        foreach (var texture in textures)
        {
            if (x + texture.Width > imageSize.x)
            {
                x = 0;
                y += (int)atlas.MaxFrameSize.y;
            }
            if (y + texture.Height > imageSize.y)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            var pixels = texture.GetPixels();

            for (int i = 0; i < texture.Width; i++)
            {
                for (int j = 0; j < texture.Height; j++)
                {
                    var index = (x + 1 + i + (y + 1 + j) * (int)imageSize.x) * 4;
                    var textureIndex = i + j * texture.Width;
                    textureData[index] = pixels[textureIndex].r;
                    textureData[index + 1] = pixels[textureIndex].g;
                    textureData[index + 2] = pixels[textureIndex].b;
                    textureData[index + 3] = pixels[textureIndex].a;
                }
            }

            x += (int)atlas.MaxFrameSize.x;
        }

        var builder = Texture.Create((int)imageSize.x, (int)imageSize.y);
        builder.WithData(textureData);
        builder.WithMips(0);
        atlas.Texture = builder.Finish();

        Cache[key] = atlas;

        return atlas;
    }

    /// <summary>
    /// Returns a cached texture atlas given a spritesheet path and a list of sprite rects. Creates one if not in the cache. Returns null if there was an error and the atlas could not be created.
    /// </summary>
    /// <param name="path">The path to the spritesheet texture</param>
    /// <param name="spriteRects">A list of rectangles representing the position of each sprite in the spritesheet</param>
    public static TextureAtlas FromSpritesheet(string path, List<Rect> spriteRects)
    {
        var key = path + string.Join(",", spriteRects.OrderBy(x => x));
        if (Cache.TryGetValue(key, out var cachedAtlas))
        {
            return cachedAtlas;
        }

        var atlas = new TextureAtlas();
        atlas.Size = (int)Math.Ceiling(Math.Sqrt(spriteRects.Count));

        if (!FileSystem.Mounted.FileExists(path))
        {
            Log.Error($"TextureAtlas: Texture file not found: {path}");
            return null;
        }

        foreach (var rect in spriteRects)
        {
            atlas.MaxFrameSize = new Vector2(
                Math.Max(atlas.MaxFrameSize.x, rect.Width),
                Math.Max(atlas.MaxFrameSize.y, rect.Height)
            );
        }
        atlas.MaxFrameSize += 2;

        var spritesheet = Texture.Load(FileSystem.Mounted, path);
        var pixels = spritesheet.GetPixels();

        Vector2 imageSize = atlas.Size * atlas.MaxFrameSize;
        byte[] textureData = new byte[(int)(imageSize.x * imageSize.y * 4)];
        foreach (var rect in spriteRects)
        {
            int x = 0;
            int y = 0;
            if (x + rect.Width > imageSize.x)
            {
                x = 0;
                y += (int)atlas.MaxFrameSize.x;
            }
            if (y + rect.Height > imageSize.y)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < rect.Height; j++)
                {
                    var index = (x + 1 + i + (y + 1 + j) * (int)imageSize.x) * 4;
                    var textureIndex = (int)(rect.Left + i + (rect.Top + j) * spritesheet.Width);
                    textureData[index] = pixels[textureIndex].r;
                    textureData[index + 1] = pixels[textureIndex].g;
                    textureData[index + 2] = pixels[textureIndex].b;
                    textureData[index + 3] = pixels[textureIndex].a;
                }
            }

            x += (int)atlas.MaxFrameSize.x;
        }

        var builder = Texture.Create((int)imageSize.x, (int)imageSize.y);
        builder.WithData(textureData);
        builder.WithMips(0);
        atlas.Texture = builder.Finish();

        Cache[key] = atlas;

        return atlas;
    }

    /// <summary>
    /// Clears the cache of texture atlases. If a path is provided, only the atlases that contain that path will be removed.
    /// </summary>
    /// <param name="path">The path to remove from the cache</param>
    public static void ClearCache(string path = "")
    {
        if (path.StartsWith("/")) path = path.Substring(1);
        if (string.IsNullOrEmpty(path))
        {
            Cache.Clear();
        }
        else
        {
            Cache = Cache.Where(x => !x.Key.Contains(path)).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}