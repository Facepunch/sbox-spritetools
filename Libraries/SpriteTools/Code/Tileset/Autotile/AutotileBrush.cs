using System;
using System.Collections.Generic;
using Sandbox;

namespace SpriteTools;

public class AutotileBrush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Is47Tiles { get; set; } = false;
    [Property] public string Name { get; set; }
    [Property] public Tile[] Tiles { get; set; }

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

    public class Tile
    {
        // [InlineEditor, WideMode(HasLabel = false)]
        [Property] public List<TileReference> Tiles { get; set; }
    }

    public class TileReference
    {
        public Guid Id { get; set; }
        public Vector2Int Position { get; set; }
        public float Weight { get; set; } = 10f;
    }
}