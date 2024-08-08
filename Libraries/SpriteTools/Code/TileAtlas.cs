using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;

namespace SpriteTools;

/// <summary>
/// A class that re-packs a tileset with 1px borders to avoid bleeding.
/// </summary>
public class TileAtlas
{
    Texture Texture;
    Vector2 OriginalTileSize;
    Vector2Int TileSize;
    Vector2Int TileCounts;


    public static Dictionary<TilesetResource, TileAtlas> Cache = new();

    public Vector2 GetTiling()
    {
        return (Vector2)OriginalTileSize / Texture.Size;
    }

    public Vector2 GetOffset(Vector2Int cellPosition)
    {
        return new Vector2(cellPosition.x * TileSize.x + 1, cellPosition.y * TileSize.y + 1) / Texture.Size;
    }

    public static TileAtlas FromTileset(TilesetResource tilesetResource)
    {
        if (Cache.ContainsKey(tilesetResource))
        {
            return Cache[tilesetResource];
        }

        var path = tilesetResource.FilePath;
        var texture = Texture.Load(FileSystem.Mounted, path);
        var atlas = new TileAtlas();

        var tileSize = tilesetResource.TileSize;
        atlas.TileSize = tileSize + Vector2Int.One * 2;
        atlas.OriginalTileSize = tileSize;

        var hTiles = tilesetResource.Tiles.Max(x => x.Position.x + x.Size.x);
        var vTiles = tilesetResource.Tiles.Max(x => x.Position.y + x.Size.y);
        atlas.TileCounts = new Vector2Int(hTiles, vTiles);

        var textureSize = new Vector2Int(hTiles * (tileSize.x + 2), vTiles * (tileSize.y + 2));

        byte[] textureData = new byte[textureSize.x * textureSize.y * 4];
        for (int i = 0; i < textureSize.x; i++)
        {
            for (int j = 0; j < textureSize.y; j++)
            {
                var ind = (j * textureSize.x + i) * 4;
                textureData[ind] = 0;
                textureData[ind + 1] = 0;
                textureData[ind + 2] = 0;
                textureData[ind + 3] = 0;
            }
        }

        var pixels = texture.GetPixels();

        foreach (var tile in tilesetResource.Tiles)
        {
            for (int n = 0; n < tile.Size.x; n++)
            {
                for (int m = 0; m < tile.Size.y; m++)
                {
                    var cellPos = tile.Position + new Vector2Int(n, m);

                    var tSize = tileSize * tile.Size;
                    var tPos = cellPos * atlas.TileSize + Vector2Int.One;
                    var sampleX = cellPos.x * tileSize.x;
                    var sampleY = cellPos.y * tileSize.y;
                    for (int i = -1; i <= tSize.x; i++)
                    {
                        for (int j = -1; j <= tSize.y; j++)
                        {
                            var sampleInd = (int)((sampleY + Math.Clamp(j, 0, tSize.y - 1)) * texture.Size.x + sampleX + Math.Clamp(i, 0, tSize.x - 1));
                            var color = pixels[sampleInd];
                            var ind = ((tPos.y + j) * textureSize.x + tPos.x + i) * 4;
                            textureData[ind + 0] = color.r;
                            textureData[ind + 1] = color.g;
                            textureData[ind + 2] = color.b;
                            textureData[ind + 3] = color.a;
                        }
                    }


                }
            }

        }

        var builder = Texture.Create(textureSize.x, textureSize.y);
        builder.WithData(textureData);
        builder.WithMips(0);
        atlas.Texture = builder.Finish();

        Cache[tilesetResource] = atlas;

        return atlas;
    }

    // Cast to texture
    public static implicit operator Texture(TileAtlas atlas)
    {
        return atlas?.Texture ?? null;
    }

    public static void ClearCache()
    {
        Cache.Clear();
    }
}