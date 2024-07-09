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
    public int Size { get; private set; }

    Texture Texture;
    int MaxFrameSize;
    static Dictionary<string, TextureAtlas> Cache = new();

    /// <summary>
    /// Returns the UV tiling for the texture atlas.
    /// </summary>
    public Vector2 GetFrameTiling()
    {
        // inset by 1 pixel to avoid bleeding
        return new Vector2(MaxFrameSize - 2, MaxFrameSize - 2) / ((float)MaxFrameSize * Size);
    }

    /// <summary>
    /// Returns the UV offset for a specific frame in the texture atlas.
    /// </summary>
    /// <param name="index">The index of the frame</param>
    public Vector2 GetFrameOffset(int index)
    {
        int x = index * MaxFrameSize % (Size * MaxFrameSize);
        int y = index * MaxFrameSize / (Size * MaxFrameSize) * MaxFrameSize;
        x += 1;
        y += 1;
        return new Vector2(x, y) / (float)(Size * MaxFrameSize);
    }

    public Texture GetTextureFromFrame(int index)
    {
        int x = index * MaxFrameSize % (Size * MaxFrameSize);
        int y = index * MaxFrameSize / (Size * MaxFrameSize) * MaxFrameSize;
        x += 1;
        y += 1;
        byte[] textureData = new byte[MaxFrameSize * MaxFrameSize * 4];
        for (int i = 0; i < MaxFrameSize; i++)
        {
            for (int j = 0; j < MaxFrameSize; j++)
            {
                var ind = (i + j * MaxFrameSize) * 4;
                var color = Texture.GetPixel(x + i, y + j);
                textureData[ind + 0] = color.r;
                textureData[ind + 1] = color.g;
                textureData[ind + 2] = color.b;
                textureData[ind + 3] = color.a;
            }
        }

        var builder = Texture.Create(MaxFrameSize, MaxFrameSize);
        builder.WithData(textureData);
        builder.WithMips(0);
        return builder.Finish();
    }

    // Cast to texture
    public static implicit operator Texture(TextureAtlas atlas)
    {
        return atlas.Texture;
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

        List<(Texture, Rect)> textures = new();
        atlas.MaxFrameSize = 0;
        foreach (var frame in animation.Frames)
        {
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
            atlas.MaxFrameSize = Math.Max(atlas.MaxFrameSize, (int)Math.Max(rect.Width, rect.Height));
        }
        atlas.MaxFrameSize += 2;

        int imageSize = atlas.Size * atlas.MaxFrameSize;
        int x = 0;
        int y = 0;
        byte[] textureData = new byte[imageSize * imageSize * 4];
        foreach (var (texture, rect) in textures)
        {
            if (x + rect.Width > imageSize)
            {
                x = 0;
                y += atlas.MaxFrameSize;
            }
            if (y + rect.Height > imageSize)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            var pixels = texture.GetPixels();

            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < rect.Height; j++)
                {
                    var index = (x + 1 + i + (y + 1 + j) * imageSize) * 4;
                    var textureIndex = (int)(rect.Left + i + (rect.Top + j) * texture.Width);
                    textureData[index] = pixels[textureIndex].r;
                    textureData[index + 1] = pixels[textureIndex].g;
                    textureData[index + 2] = pixels[textureIndex].b;
                    textureData[index + 3] = pixels[textureIndex].a;
                }
            }

            x += atlas.MaxFrameSize;
        }

        var builder = Texture.Create(imageSize, imageSize);
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
            atlas.MaxFrameSize = Math.Max(atlas.MaxFrameSize, Math.Max(texture.Width, texture.Height));
        }
        atlas.MaxFrameSize += 2;

        int imageSize = atlas.Size * atlas.MaxFrameSize;
        int x = 0;
        int y = 0;
        byte[] textureData = new byte[imageSize * imageSize * 4];
        foreach (var texture in textures)
        {
            if (x + texture.Width > imageSize)
            {
                x = 0;
                y += atlas.MaxFrameSize;
            }
            if (y + texture.Height > imageSize)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            var pixels = texture.GetPixels();

            for (int i = 0; i < texture.Width; i++)
            {
                for (int j = 0; j < texture.Height; j++)
                {
                    var index = (x + 1 + i + (y + 1 + j) * imageSize) * 4;
                    var textureIndex = i + j * texture.Width;
                    textureData[index] = pixels[textureIndex].r;
                    textureData[index + 1] = pixels[textureIndex].g;
                    textureData[index + 2] = pixels[textureIndex].b;
                    textureData[index + 3] = pixels[textureIndex].a;
                }
            }

            x += atlas.MaxFrameSize;
        }

        var builder = Texture.Create(imageSize, imageSize);
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
            atlas.MaxFrameSize = (int)Math.Max(atlas.MaxFrameSize, Math.Max(rect.Width, rect.Height));
        }
        atlas.MaxFrameSize += 2;

        var spritesheet = Texture.Load(FileSystem.Mounted, path);
        var pixels = spritesheet.GetPixels();

        int imageSize = atlas.Size * atlas.MaxFrameSize;
        byte[] textureData = new byte[imageSize * imageSize * 4];
        foreach (var rect in spriteRects)
        {
            int x = 0;
            int y = 0;
            if (x + rect.Width > imageSize)
            {
                x = 0;
                y += atlas.MaxFrameSize;
            }
            if (y + rect.Height > imageSize)
            {
                Log.Error("TextureAtlas: Texture too large for atlas");
                continue;
            }

            for (int i = 0; i < rect.Width; i++)
            {
                for (int j = 0; j < rect.Height; j++)
                {
                    var index = (x + 1 + i + (y + 1 + j) * imageSize) * 4;
                    var textureIndex = (int)(rect.Left + i + (rect.Top + j) * spritesheet.Width);
                    textureData[index] = pixels[textureIndex].r;
                    textureData[index + 1] = pixels[textureIndex].g;
                    textureData[index + 2] = pixels[textureIndex].b;
                    textureData[index + 3] = pixels[textureIndex].a;
                }
            }

            x += atlas.MaxFrameSize;
        }

        var builder = Texture.Create(imageSize, imageSize);
        builder.WithData(textureData);
        builder.WithMips(0);
        atlas.Texture = builder.Finish();

        Cache[key] = atlas;

        return atlas;
    }
}