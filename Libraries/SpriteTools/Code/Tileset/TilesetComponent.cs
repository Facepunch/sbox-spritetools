using Sandbox;
using System.Collections.Generic;

namespace SpriteTools;

public sealed class TilesetComponent : Component
{
	List<Tile> Tiles { get; set; } = new List<Tile>();



	public class Tile
	{
		public TilesetResource TileResource { get; set; }
		public string Atlas { get; set; }
		public Vector2 Offset { get; set; }
		public Vector2 Tiling { get; set; }
		public Vector3 Position { get; set; }
		public Rotation Rotation { get; set; }
		public Vector3 Scale { get; set; }

		public SceneObject SceneObject;

		public Tile(string atlas, Vector2 offset, Vector2 tiling, Vector3 position, Rotation rotation, Vector3 scale, TilesetResource tileResource)
		{
			Atlas = atlas;
			Offset = offset;
			Tiling = tiling;
			Position = position;
			Rotation = rotation;
			Scale = scale;
			TileResource = tileResource;
		}
	}

}