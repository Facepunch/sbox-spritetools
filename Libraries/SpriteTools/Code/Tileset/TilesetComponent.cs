using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools;

[Category("2D")]
[Title("2D Tileset Component")]
[Icon("calendar_view_month")]
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
				Tiles.Add(new Tile(new Vector2Int(Random.Shared.Int(10), Random.Shared.Int(10)), new Transform((Vector3.Random.WithZ(0) * 64).SnapToGrid(1))));
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

		public bool SetTile(Vector2Int cellPosition, Transform transform)
		{
			var tile = Tiles.FirstOrDefault(x => x.Transform.Position == transform.Position);
			if (tile is not null)
			{
				if (tile.CellPosition == cellPosition) return false;
				tile.CellPosition = cellPosition;
				return true;
			}
			else
			{
				Tiles.Add(new Tile(cellPosition, transform));
				return true;
			}
		}

		public Tile GetTile(Vector3 position)
		{
			return Tiles.FirstOrDefault(x => x.Transform.Position == position);
		}
	}

	public class Tile
	{
		public Vector2Int CellPosition { get; set; }
		public Transform Transform { get; set; }

		public Tile(Vector2Int cellPosition, Transform transform)
		{
			CellPosition = cellPosition;
			Transform = transform;
		}

		public Tile Copy()
		{
			return new Tile(CellPosition, Transform);
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

		var layers = Component.Layers.ToList();
		layers.Reverse();
		if (layers.Count == 0) return;

		foreach (var layer in layers)
		{
			if (!layer.IsVisible) continue;

			int i = 0;
			int layerIndex = layers.IndexOf(layer);

			var tileset = layer.TilesetResource;
			if (tileset is null) continue;

			var groups = layer.Tiles.GroupBy(x => layer.TilesetResource.FilePath);
			foreach (var group in groups)
			{
				var atlas = group.Key ?? "";
				var material = GetMaterial(atlas);

				var totalTiles = group.Count();
				var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles * 6);

				foreach (var tile in group)
				{
					var transform = tile.Transform;

					var tiling = tileset.GetTiling();
					var offset = tileset.GetOffset(tile.CellPosition);

					var position = transform.Position.WithZ(layerIndex) * new Vector3(tileset.TileSize.x, tileset.TileSize.y, 1);
					var size = transform.Scale * tileset.TileSize;

					var topLeft = new Vector3(position.x, position.y, position.z);
					var topRight = new Vector3(position.x + size.x, position.y, position.z);
					var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z);
					var bottomLeft = new Vector3(position.x, position.y + size.y, position.z);

					var uvTopLeft = new Vector2(offset.x, offset.y);
					var uvTopRight = new Vector2(offset.x + tiling.x, offset.y);
					var uvBottomRight = new Vector2(offset.x + tiling.x, offset.y + tiling.y);
					var uvBottomLeft = new Vector2(offset.x, offset.y + tiling.y);

					vertex[i] = new Vertex(topLeft);
					vertex[i].TexCoord0 = uvTopLeft;
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(topRight);
					vertex[i].TexCoord0 = uvTopRight;
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomRight);
					vertex[i].TexCoord0 = uvBottomRight;
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(topLeft);
					vertex[i].TexCoord0 = uvTopLeft;
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomRight);
					vertex[i].TexCoord0 = uvBottomRight;
					vertex[i].Color = Color.White;
					vertex[i].Normal = Vector3.Up;
					i++;

					vertex[i] = new Vertex(bottomLeft);
					vertex[i].TexCoord0 = uvBottomLeft;
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