using System;
using System.Collections.Generic;
using Sandbox;

namespace SpriteTools;

public class AutotileBrush
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool Is47Tiles { get; set; } = false;
    [Property] public string Name { get; set; }
    [Property] public List<Tile> Tiles { get; set; } = new();

    public AutotileBrush(bool is47Tiles = false)
    {
        Is47Tiles = is47Tiles;
    }

    public class Tile
    {
        [Property] public List<TileReference> Tiles { get; set; }
    }

    public class TileReference
    {
        public Guid Id { get; set; }
        public Vector2Int Position { get; set; }
        public float Weight { get; set; } = 10f;
    }
}