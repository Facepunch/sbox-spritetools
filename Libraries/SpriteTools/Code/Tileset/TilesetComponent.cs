using Sandbox;
using System.Collections.Generic;

namespace SpriteTools;

public sealed class TilesetComponent : Component
{
	[Property, Group("Layers")] public List<Layer> Layers { get; set; }

	public class Layer
	{
		public string Name { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLocked { get; set; }

		List<Tile> Tiles { get; set; } = new();

		public Layer(string name = "Untitled Layer")
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;
		}

		public Layer Copy()
		{
			var layer = new Layer(Name)
			{
				IsVisible = IsVisible,
				IsLocked = IsLocked,
				Tiles = new List<Tile>()
			};

			foreach (var tile in Tiles)
			{
				layer.Tiles.Add(tile.Copy());
			}

			return layer;

		}
	}

	public class Tile
	{
		public TilesetResource TileResource { get; set; }
		public string TileName { get; set; }
		public Transform Transform { get; set; }

		public Tile(TilesetResource tileResource, string tileName, Transform transform)
		{
			TileResource = tileResource;
			TileName = tileName;
			Transform = transform;
		}

		public Tile Copy()
		{
			return new Tile(TileResource, TileName, Transform);
		}
	}

}