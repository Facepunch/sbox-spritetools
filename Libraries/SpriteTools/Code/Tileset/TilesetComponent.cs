using Sandbox;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools;

public sealed class TilesetComponent : Component
{
	[Property, Group("Layers")] public List<Layer> Layers { get; set; }

	public class Layer
	{
		public string Name { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLocked { get; set; }
		public TilesetResource TilesetResource { get; set; }

		public List<Tile> Tiles { get; set; } = new();

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

		public void AddTile(Tile tile)
		{
			Tiles.Add(tile);
		}
	}

	public class Tile
	{
		public string TileName { get; set; }
		public Transform Transform { get; set; }

		public Tile(string tileName, Transform transform)
		{
			TileName = tileName;
			Transform = transform;
		}

		public Tile Copy()
		{
			return new Tile(TileName, Transform);
		}
	}

}

internal sealed class TilesetSceneObject : SceneCustomObject
{
	TilesetComponent Component;
	Material material;

	public TilesetSceneObject(TilesetComponent component, SceneWorld world) : base(world)
	{
		Component = component;

		material = Material.FromShader("materials/sprite_2d.vmat");
	}

	public override void RenderSceneObject()
	{
		if (Component?.Layers is null) return;

		var layers = Component.Layers;
		if (layers.Count == 0) return;

		var viewerPosition = Graphics.CameraPosition;
		var totalTiles = layers.Sum(x => x.Tiles.Count);
		var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles * 4);

		int i = 0;
		foreach (var layer in layers)
		{
			if (!layer.IsVisible) continue;

			var tileset = layer.TilesetResource;

			foreach (var tile in layer.Tiles)
			{
				var tileName = tile.TileName;
				var transform = tile.Transform;

				var texture = Texture.Load(tileset.Atlas);
				if (texture is null) continue;

				var tiling = tileset.GetTiling();
				var offset = tileset.GetOffset(4);

				var position = transform.Position;
				var size = transform.Scale;

				var topLeft = new Vector3(position.x - size.x / 2, position.y - size.y / 2, position.z);
				var topRight = new Vector3(position.x + size.x / 2, position.y - size.y / 2, position.z);
				var bottomRight = new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z);
				var bottomLeft = new Vector3(position.x - size.x / 2, position.y + size.y / 2, position.z);

				var uvTopLeft = new Vector2(offset.x * tiling.x, offset.y * tiling.y);
				var uvTopRight = new Vector2((offset.x + 1) * tiling.x, offset.y * tiling.y);
				var uvBottomRight = new Vector2((offset.x + 1) * tiling.x, (offset.y + 1) * tiling.y);
				var uvBottomLeft = new Vector2(offset.x * tiling.x, (offset.y + 1) * tiling.y);

				vertex[i++] = new Vertex(topLeft);
				vertex[i++] = new Vertex(topRight);
				vertex[i++] = new Vertex(bottomRight);
				vertex[i++] = new Vertex(bottomLeft);
			}
		}

		Graphics.Draw(vertex, totalTiles * 4, material, Attributes);
	}
}