using Sandbox;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SpriteTools
{
    public partial class TilesetResource
    {
        public class Tile : IJsonConvert
        {
            public Guid Id { get; set; }

            [JsonIgnore, ReadOnly, Property]
            public int Index => Tileset?.Tiles?.IndexOf(this) ?? -1;

            [Property]
            public string Name { get; set; } = "";

            [Property]
            public TagSet Tags { get; set; }

            [Property]
            public Vector2Int Position { get; set; }

            [Property]
            public Vector2Int Size { get; set; }

            [JsonIgnore, Hide, ReadOnly]
            public TilesetResource Tileset;

            public Tile(Vector2Int position, Vector2Int size)
            {
                Id = Guid.NewGuid();
                Position = position;
                Size = size;
            }

            public Tile Copy()
            {
                var copy = new Tile(Position, Size)
                {
                    Name = Name
                };
                return copy;
            }

            public string GetName()
            {
                return string.IsNullOrEmpty(Name) ? $"Tile {Position}" : Name;
            }

            public static object JsonRead(ref Utf8JsonReader reader, Type targetType)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    reader.Read();

                    int tilesetId = 0;
                    Guid tileId = Guid.Empty;

                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var name = reader.GetString();
                            reader.Read();

                            if (name == "tileset")
                            {
                                tilesetId = int.Parse(reader.GetString());
                                reader.Read();
                                continue;
                            }
                            else if (name == "tile")
                            {
                                tileId = Guid.Parse(reader.GetString());
                                reader.Read();
                                continue;
                            }

                            reader.Read();
                            continue;
                        }

                        reader.Read();
                    }

                    if (tilesetId != 0 && tileId != Guid.Empty)
                    {
                        var allTilesets = ResourceLibrary.GetAll<TilesetResource>();
                        foreach (var tileset in allTilesets)
                        {
                            if (tileset.ResourceId == tilesetId)
                            {
                                return tileset.TileMap[tileId];
                            }
                        }
                    }

                }
                return null;
            }

            public static void JsonWrite(object value, Utf8JsonWriter writer)
            {
                if (value is not TilesetResource.Tile tile)
                    throw new NotImplementedException();

                if (tile is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteStartObject();
                {
                    writer.WritePropertyName("tileset");
                    writer.WriteNumberValue(tile.Tileset.ResourceId);

                    writer.WritePropertyName("tile");
                    writer.WriteStringValue(tile.Id.ToString());
                }
                writer.WriteEndObject();
            }
        }
    }
}