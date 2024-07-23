using Sandbox;
using System.Collections.Generic;

namespace SpriteTools;

public sealed class TilesetComponent : Component
{
	public List<Layer> Layers { get; set; }

	public class Layer
	{
		public string Name { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLocked { get; set; }

		List<Tile> Tiles { get; set; }

		public Layer(string name = "Untitled Layer")
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;
		}
	}

	public class Tile
	{
		public TilesetResource TileResource { get; set; }
		string TileName { get; set; }
		public Transform Transform { get; set; }

		public Tile(TilesetResource tileResource, string tileName, Transform transform)
		{
			TileResource = tileResource;
			TileName = tileName;
			Transform = transform;
		}
	}

}