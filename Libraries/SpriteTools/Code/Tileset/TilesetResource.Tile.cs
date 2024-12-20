using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools;

public partial class TilesetResource
{
    public class Tile
    {
        /// <summary>
        /// The unique ID for the Tile
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The index of the Tile in the Tileset
        /// </summary>
        [JsonIgnore, ReadOnly, Property]
        public int Index => Tileset?.Tiles?.ToList()?.IndexOf(this) ?? -1;

        /// <summary>
        /// The name of the Tile (if any)
        /// </summary>
        [Property]
        public string Name { get; set; } = "";

        /// <summary>
        /// The tags associated with the Tile. These are used for searching/filtering tiles or adding custom data.
        /// </summary>
        [Property]
        public TagSet Tags { get; set; }

        /// <summary>
        /// The position of the Tile in the Atlas
        /// </summary>
        [Property]
        public Vector2Int Position { get; set; }

        /// <summary>
        /// The size of the Tile in the Atlas (in Tiles)
        /// </summary>
        [Property]
        public Vector2Int Size { get; set; }

        /// <summary>
        /// The 
        /// </summary>
        [JsonIgnore, Hide, ReadOnly]
        public TilesetResource Tileset { get; internal set; }

        public Tile(Vector2Int position, Vector2Int size)
        {
            Id = Guid.NewGuid();
            Position = position;
            Size = size;
        }

        /// <summary>
        /// Creates a copy of the Tile with a new ID
        /// </summary>
        /// <returns></returns>
        public Tile Copy()
        {
            var copy = new Tile(Position, Size)
            {
                Name = Name,
                Tags = new TagSet(),
                Tileset = Tileset
            };
            foreach (var tag in Tags.TryGetAll())
            {
                copy.Tags.Add(tag);
            }
            return copy;
        }

        /// <summary>
        /// Returns the name of the Tile or a default name if none is set.
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return string.IsNullOrEmpty(Name) ? $"Tile {Position}" : Name;
        }

    }
}