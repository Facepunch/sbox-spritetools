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

		public Dictionary<string, Tile> Tiles { get; set; } = new();

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
				Tiles = new()
			};

			foreach (var tile in Tiles)
			{
				layer.Tiles[tile.Key] = tile.Value.Copy();
			}

			return layer;
		}

		public void SetTile(Vector2Int position, Vector2Int cellPosition, Transform transform)
		{
			var tile = new Tile(cellPosition, transform);
			Tiles[position.ToString()] = tile;
		}

		public Tile GetTile(Vector3 position)
		{
			return Tiles[new Vector2Int((int)position.x, (int)position.y).ToString()];
		}

		public void RemoveTile(Vector2Int position)
		{
			Tiles.Remove(position.ToString());
		}
	}

	public class Tile
	{
		public Vector2Int CellPosition { get; set; }
		bool HorizontalFlip { get; set; }
		bool VerticalFlip { get; set; }
		int Rotation { get; set; }

		public Tile(Vector2Int cellPosition, Transform transform)
		{
			CellPosition = cellPosition;
			HorizontalFlip = transform.Scale.x < 0;
			VerticalFlip = transform.Scale.y < 0;
			Rotation = (int)(transform.Rotation.Yaw() / 90f);
		}

		public Transform GetTransform()
		{
			var scale = new Vector3(HorizontalFlip ? -1 : 1, VerticalFlip ? -1 : 1, 1);
			var rotation = Rotation * 90;
			return new Transform(new Vector3(CellPosition.x, CellPosition.y, 0), new Angles(0, rotation, 0), scale);
		}

		public Tile Copy()
		{
			return new Tile(CellPosition, GetTransform());
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
			var tiling = tileset.GetTiling();

			var groups = layer.Tiles.GroupBy(x => layer.TilesetResource.FilePath);
			foreach (var group in groups)
			{
				var atlas = group.Key ?? "";
				var material = GetMaterial(atlas);

				var totalTiles = group.Count();
				var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles * 6);

				foreach (var tile in group)
				{
					var transform = tile.Value.GetTransform();
					var offset = tileset.GetOffset(tile.Value.CellPosition);

					var pos = Vector2Int.Parse(tile.Key);
					var position = new Vector3(pos.x, pos.y, layerIndex) * new Vector3(tileset.TileSize.x, tileset.TileSize.y, 1);
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