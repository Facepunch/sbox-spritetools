using Sandbox;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools;

public sealed class TilesetComponent : Component, Component.ExecuteInEditor
{
	[Property, Group("Layers")] public List<Layer> Layers { get; set; }

	TilesetSceneObject _so;

	protected override void OnEnabled()
	{
		_so = new TilesetSceneObject(this, Scene.SceneWorld);
		_so.Transform = Transform.World;
		_so.Tags.SetFrom(Tags);
	}

	protected override void OnDisabled()
	{
		_so?.Delete();
		_so = null;
	}

	protected override void OnTagsChanged()
	{
		_so?.Tags.SetFrom(Tags);
	}

	protected override void OnPreRender()
	{
		if (_so is null) return;
		if (Layers.Count == 0)
		{
			_so.RenderingEnabled = false;
			return;
		}

		_so.RenderingEnabled = true;
		_so.Transform = Transform.World;
		_so.Flags.CastShadows = false;
		_so.Flags.IsOpaque = false;
		_so.Flags.IsTranslucent = true;
	}

	public class Layer
	{
		public string Name { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLocked { get; set; }
		[Property, Group("Selected Layer")] public TilesetResource TilesetResource { get; set; }

		public List<Tile> Tiles { get; set; } = new();

		public Layer(string name = "Untitled Layer")
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;

			for (int i = 0; i < 10; i++)
			{
				Tiles.Add(new Tile(i, new Transform((Vector3.Random.WithZ(0) * 64).SnapToGrid(1))));
			}
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
		public int Index { get; set; }
		public Transform Transform { get; set; }

		public Tile(int index, Transform transform)
		{
			Index = index;
			Transform = transform;
		}

		public Tile Copy()
		{
			return new Tile(Index, Transform);
		}
	}

}

internal sealed class TilesetSceneObject : SceneCustomObject
{
	TilesetComponent Component;
	Dictionary<string, Material> Materials = new();

	public TilesetSceneObject(TilesetComponent component, SceneWorld world) : base(world)
	{
		Component = component;
	}

	public override void RenderSceneObject()
	{
		if (Component?.Layers is null) return;

		var layers = Component.Layers;
		if (layers.Count == 0) return;

		foreach (var layer in layers)
		{
			if (!layer.IsVisible) continue;

			int i = 0;
			var totalTiles = layer.Tiles.Count;
			var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles * 6);

			var tileset = layer.TilesetResource;
			if (tileset is null) continue;

			var groups = layer.Tiles.GroupBy(x => (x.Index >= layer.TilesetResource.Tiles.Count) ? "" : layer.TilesetResource.Tiles[x.Index].FilePath);
			foreach (var group in groups)
			{
				var atlas = group.Key ?? "";
				var material = GetMaterial(atlas);

				foreach (var tile in group)
				{
					var transform = tile.Transform;

					var tiling = tileset.GetTiling();
					var offset = tileset.GetOffset(4);

					var position = transform.Position * tileset.TileSize;
					var size = transform.Scale * tileset.TileSize;

					var topLeft = new Vector3(position.x, position.y, position.z);
					var topRight = new Vector3(position.x + size.x, position.y, position.z);
					var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z);
					var bottomLeft = new Vector3(position.x, position.y + size.y, position.z);

					var uvTopLeft = new Vector2(offset.x * tiling.x, offset.y * tiling.y);
					var uvTopRight = new Vector2((offset.x + 1) * tiling.x, offset.y * tiling.y);
					var uvBottomRight = new Vector2((offset.x + 1) * tiling.x, (offset.y + 1) * tiling.y);
					var uvBottomLeft = new Vector2(offset.x * tiling.x, (offset.y + 1) * tiling.y);

					vertex[i] = new Vertex(topLeft);
					vertex[i].TexCoord0 = uvTopLeft;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(topRight);
					vertex[i].TexCoord0 = uvTopRight;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomRight);
					vertex[i].TexCoord0 = uvBottomRight;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(topLeft);
					vertex[i].TexCoord0 = uvTopLeft;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomRight);
					vertex[i].TexCoord0 = uvBottomRight;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomLeft);
					vertex[i].TexCoord0 = uvBottomLeft;
					vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;
				}

				Graphics.Draw(vertex, totalTiles * 6, material, Attributes);
			}

		}
	}

	Material GetMaterial(string texturePath)
	{
		if (!Materials.TryGetValue(texturePath, out var material))
		{
			material = Material.Load("materials/sprite_2d.vmat").CreateCopy();
			material.Set("Texture", Texture.Load(FileSystem.Mounted, texturePath));
			Materials.Add(texturePath, material);
		}

		return material;
	}
}