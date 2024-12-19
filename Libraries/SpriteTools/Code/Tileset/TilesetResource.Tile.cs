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
    [JsonConverter(typeof(JsonConvertReference))]
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
        
        public JsonObject Serialize()
        {
            var json = new JsonObject
            {
                { "Id", Id.ToString() },
                { "Name", Name },
                { "Position", Position.ToString() },
                { "Size", Size.ToString() }
            };

            if (Tags != null)
            {
                json["Tags"] = string.Join(",", Tags.TryGetAll());
            }
            else
            {
                json["Tags"] = "";
            }

            return json;
        }

        public void Deserialize(JsonObject obj)
        {
            Id = Guid.Parse(obj["Id"].GetValue<string>());
            Name = obj["Name"].GetValue<string>();
            Position = Vector2Int.Parse(obj["Position"].GetValue<string>());
            Size = Vector2Int.Parse(obj["Size"].GetValue<string>());

            Tags = new TagSet();
            if (obj["Tags"] is not null)
            {
                var tagString = obj["Tags"].GetValue<string>();
                if (!string.IsNullOrEmpty(tagString))
                {
                    var tags = tagString.Split(',');
                    foreach (var tag in tags)
                    {
                        Tags.Add(tag);
                    }
                }
            }
        }

        public class JsonConvertReference : JsonConverter<Tile>
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert == typeof(Tile);
            }

            public override Tile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                                tilesetId = reader.GetInt32();
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

            public override void Write(Utf8JsonWriter writer, Tile value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                {
                    writer.WritePropertyName("tileset");
                    writer.WriteNumberValue(value.Tileset.ResourceId);

                    writer.WritePropertyName("tile");
                    writer.WriteStringValue(value.Id.ToString());
                }
                writer.WriteEndObject();
            }
        }

        public class JsonConvert : JsonConverter<Tile>
        {

            public override bool CanConvert(Type objectType)
            {
                Log.Info($"CanConvert: {objectType}");
                return objectType == typeof(Tile);
            }

            public override Tile Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Log.Info($"Attempting to read in converter...");
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    reader.Read();

                    Guid id = Guid.Empty;
                    string name = "";
                    TagSet tags = new TagSet();
                    Vector2Int position = Vector2Int.Zero;
                    Vector2Int size = Vector2Int.One;

                    while (reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            var propertyName = reader.GetString();
                            reader.Read();

                            if (propertyName == "Id")
                            {
                                id = Guid.Parse(reader.GetString());
                                reader.Read();
                                continue;
                            }
                            else if (propertyName == "Name")
                            {
                                name = reader.GetString();
                                reader.Read();
                                continue;
                            }
                            else if (propertyName == "Tags")
                            {
                                if (reader.TokenType == JsonTokenType.Null)
                                {
                                    tags = new TagSet();
                                }
                                else
                                {
                                    var tagConverter = new TagSet.JsonConvert();
                                    tags = tagConverter.Read(ref reader, typeof(TagSet), options);
                                }
                                reader.Read();
                                continue;
                            }
                            else if (propertyName == "Position")
                            {
                                var positionString = reader.GetString();
                                position = Vector2Int.Parse(positionString);
                                reader.Read();
                                continue;
                            }
                            else if (propertyName == "Size")
                            {
                                var sizeString = reader.GetString();
                                size = Vector2Int.Parse(sizeString);
                                reader.Read();
                                continue;
                            }

                            reader.Read();
                            continue;
                        }

                        reader.Read();
                    }

                    if (id != Guid.Empty)
                    {
                        var tile = new TilesetResource.Tile(position, size)
                        {
                            Id = id,
                            Name = name,
                            Tags = tags
                        };
                        return tile;
                    }
                }


                return null;
            }

            public override void Write(Utf8JsonWriter writer, Tile tile, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                {
                    writer.WritePropertyName("Id");
                    writer.WriteStringValue(tile.Id.ToString());

                    writer.WritePropertyName("Name");
                    writer.WriteStringValue(tile.Name);

                    writer.WritePropertyName("Tags");
                    var tagConverter = new TagSet.JsonConvert();
                    tagConverter.Write(writer, tile.Tags, options);

                    writer.WritePropertyName("Position");
                    writer.WriteStringValue(tile.Position.ToString());

                    writer.WritePropertyName("Size");
                    writer.WriteStringValue(tile.Size.ToString());
                }
                writer.WriteEndObject();
            }
        }
    }
}