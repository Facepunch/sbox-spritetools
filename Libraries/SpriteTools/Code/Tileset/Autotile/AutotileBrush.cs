using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Sandbox;

namespace SpriteTools;

public class AutotileBrush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Is47Tiles { get; set; } = false;
    [Property, Placeholder("Autotile Brush")] public string Name { get; set; }
    public Tile[] Tiles { get; set; }

    public AutotileBrush() : this(false) { }

    public AutotileBrush(bool is47Tiles = false)
    {
        Is47Tiles = is47Tiles;

        var tileCount = is47Tiles ? 47 : 16;
        Tiles = new Tile[tileCount];
        for (int i = 0; i < tileCount; i++)
        {
            Tiles[i] = new Tile();
        }
    }

    public TilesetResource.Tile GetTileFromBitmask(int bitmask)
    {
        if (bitmask < 0)
            return null;

        return null;
    }

    public class Tile
    {
        // [InlineEditor, WideMode(HasLabel = false)]
        [Property] public List<TileReference> Tiles { get; set; }
    }

    public class TileReference
    {
        [Hide] public TilesetResource Tileset { get; set; }
        public Guid Id { get; set; }
        public Vector2Int Position { get; set; }
        public float Weight { get; set; } = 10f;

        public TileReference()
        {
            Id = Guid.NewGuid();
        }

        public TileReference(Guid guid)
        {
            Id = guid;
        }
    }
}