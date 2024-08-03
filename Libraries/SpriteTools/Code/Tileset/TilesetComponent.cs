using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Category("2D")]
[Title("2D Tileset Component")]
[Icon("calendar_view_month")]
public sealed class TilesetComponent : Component, Component.ExecuteInEditor
{
	[Property, Group("Layers")]
	public List<Layer> Layers
	{
		get => _layers;
		set
		{
			_layers = value;
			foreach (var layer in _layers)
			{
				layer.TilesetComponent = this;
			}
		}
	}
	List<Layer> _layers;

	[Property, Group("Collision")]
	public bool HasCollider
	{
		get => _hasCollider;
		set
		{
			if (value == _hasCollider) return;
			_hasCollider = value;
			BuildMesh();
		}
	}
	bool _hasCollider;

	[Property, Group("Collision")]
	public float ColliderWidth
	{
		get => _colliderWidth;
		set
		{
			if (value < 0f) _colliderWidth = 0f;
			else if (value == _colliderWidth) return;
			_colliderWidth = value;
			BuildMesh();
		}
	}
	float _colliderWidth;

	private Model CollisionMesh { get; set; }
	private List<Vector3> CollisionVertices { get; set; } = new();
	private List<int[]> CollisionFaces { get; set; } = new();

	TilesetSceneObject _so;

	protected override void OnStart()
	{
		BuildMesh();
	}

	protected override void OnEnabled()
	{
		_so = new TilesetSceneObject(this, Scene.SceneWorld);
		_so.Transform = Transform.World;
		_so.Tags.SetFrom(Tags);

		if (Layers is null) return;
		foreach (var layer in Layers)
		{
			layer.TilesetComponent = this;
		}
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
		if (Layers is null) return;
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

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (CollisionMesh is not null)
		{
			using (Gizmo.Scope("tile_collisions"))
			{
				Gizmo.Draw.Color = Color.Green;
				Gizmo.Draw.LineThickness = 2f;

				foreach (var face in CollisionFaces)
				{
					for (int i = 0; i < face.Length; i++)
					{
						var a = CollisionVertices[face[i]];
						var b = CollisionVertices[face[(i + 1) % face.Length]];
						Gizmo.Draw.Line(a, b);
					}
				}
			}
		}
	}

	public void BuildMesh()
	{
		if (CollisionMesh is not null)
		{
			CollisionMesh = null;
			CollisionVertices.Clear();
			CollisionFaces.Clear();
		}

		if (!HasCollider) return;
		if (Layers is null) return;

		var collisionLayer = Layers.FirstOrDefault(x => x.IsCollisionLayer);
		if (collisionLayer is null) collisionLayer = Layers.FirstOrDefault();
		if (collisionLayer is null) return;

		var tilePositions = new Dictionary<Vector2Int, bool>();
		foreach (var tile in collisionLayer.Tiles)
		{
			tilePositions[Vector2Int.Parse(tile.Key)] = true;
		}
		if (tilePositions.Count == 0) return;

		var minPosition = tilePositions.Keys.Aggregate((min, next) => Vector2Int.Min(min, next));
		var maxPosition = tilePositions.Keys.Aggregate((max, next) => Vector2Int.Max(max, next));
		var totalSize = maxPosition - minPosition + Vector2Int.One;

		bool[,] tiles = new bool[totalSize.x, totalSize.y];
		foreach (var tile in tilePositions)
		{
			var pos = tile.Key - minPosition;
			tiles[pos.x, pos.y] = true;
		}

		// Generate mesh from tiles
		var tileSize = Layers[0].TilesetResource.TileSize;
		var mesh = new PolygonMesh();
		CollisionVertices = new List<Vector3>();
		CollisionFaces = new List<int[]>();

		var min3d = new Vector3(minPosition.x * tileSize.x, minPosition.y * tileSize.y, 0);

		bool[,] visited = new bool[totalSize.x, totalSize.y];
		for (int x = 0; x < totalSize.x; x++)
		{
			for (int y = 0; y < totalSize.y; y++)
			{

				if (tiles[x, y] && !visited[x, y])
				{
					int width = 1;
					int height = 1;

					// Check width
					while (x + width < totalSize.x && tiles[x + width, y] && !visited[x + width, y])
					{
						width++;
					}

					// Check height
					while (y + height < totalSize.y && IsRectangle(tiles, visited, x, y, width, height))
					{
						height++;
					}

					// Mark the cells of this rectangle as visited
					for (int i = 0; i < width; i++)
					{
						for (int j = 0; j < height; j++)
						{
							visited[x + i, y + j] = true;
						}
					}

					AddRectangle(CollisionVertices, CollisionFaces, tiles, x, y, width, height, tileSize, ColliderWidth, minPosition);
				}

				// if (!tiles[x, y]) continue;

				// var position = new Vector3(x * tileSize.x, y * tileSize.y, 0);
				// var size = new Vector3(tileSize.x, tileSize.y, 1);

				// var topLeft = new Vector3(position.x, position.y, position.z) + min3d;
				// var topRight = new Vector3(position.x + size.x, position.y, position.z) + min3d;
				// var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z) + min3d;
				// var bottomLeft = new Vector3(position.x, position.y + size.y, position.z) + min3d;

				// CollisionVertices.AddRange(new[] { topLeft, topRight, bottomRight, topLeft, bottomRight, bottomLeft });
				// CollisionFaces.Add(new[] { CollisionVertices.Count - 6, CollisionVertices.Count - 5, CollisionVertices.Count - 4, CollisionVertices.Count - 3, CollisionVertices.Count - 2, CollisionVertices.Count - 1 });
			}
		}

		var hVertices = mesh.AddVertices(CollisionVertices.ToArray());

		foreach (var face in CollisionFaces)
		{
			mesh.AddFace(face.Select(x => hVertices[x]).ToArray());
		}

		CollisionMesh = mesh.Rebuild();
	}

	static bool IsRectangle(bool[,] grid, bool[,] visited, int x, int y, int width, int height)
	{
		for (int i = 0; i < width; i++)
		{
			if (!grid[x + i, y + height] || visited[x + i, y + height])
			{
				return false;
			}
		}
		return true;
	}

	static void AddRectangle(List<Vector3> vertices, List<int[]> faces, bool[,] grid, int x, int y, int width, int height, Vector2Int tileSize, float depth, Vector2Int minPosition)
	{
		int startIndex = vertices.Count;

		// Bottom face (y == 0)
		vertices.Add(new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y) * tileSize.x, 0));
		vertices.Add(new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y) * tileSize.y, 0));
		vertices.Add(new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y + height) * tileSize.y, 0));
		vertices.Add(new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y + height) * tileSize.y, 0));

		// Top face (y == depth)
		if (depth != 0)
		{
			vertices.Add(new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y) * tileSize.y, depth));
			vertices.Add(new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y) * tileSize.y, depth));
			vertices.Add(new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y + height) * tileSize.y, depth));
			vertices.Add(new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y + height) * tileSize.y, depth));
		}

		// Add indices for two triangles per face (bottom and top) if not inner
		faces.Add(new int[]{
			startIndex, startIndex + 2, startIndex + 1,
			startIndex, startIndex + 3, startIndex + 2
		});
		if (depth != 0) // Top
		{
			faces.Add(new int[]{
				startIndex + 4, startIndex + 5, startIndex + 6,
				startIndex + 4, startIndex + 6, startIndex + 7
			});
		}

		// Add indices for the sides if not inner
		if (depth == 0) return;
		if (IsExposedFace(grid, x, y, 1, height, -1, 0)) // Left
		{
			faces.Add(new int[]{
				startIndex, startIndex + 4, startIndex + 7,
				startIndex, startIndex + 7, startIndex + 3
			});
		}
		if (IsExposedFace(grid, x + width - 1, y, 1, height, 1, 0)) // Right
		{
			faces.Add(new int[]{
				startIndex + 1, startIndex + 2, startIndex + 6,
				startIndex + 1, startIndex + 6, startIndex + 5
			});
		}
		if (IsExposedFace(grid, x, y, width, 1, 0, -1)) // Front
		{
			faces.Add(new int[]{
				startIndex, startIndex + 1, startIndex + 5,
				startIndex, startIndex + 5, startIndex + 4
			});
		}
		if (IsExposedFace(grid, x, y + height - 1, width, 1, 0, 1)) // Back
		{
			faces.Add(new int[]{
				startIndex + 3, startIndex + 7, startIndex + 6,
				startIndex + 3, startIndex + 6, startIndex + 2
			});
		}
	}

	static bool IsExposedFace(bool[,] grid, int x, int y, int width, int height, int dx, int dy)
	{
		int rows = grid.GetLength(0);
		int cols = grid.GetLength(1);

		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				int nx = x + i + dx;
				int ny = y + j + dy;

				if (nx < 0 || nx >= rows || ny < 0 || ny >= cols || !grid[nx, ny])
				{
					return true;
				}
			}
		}
		return false;
	}

	public class Layer
	{
		public string Name { get; set; }
		public bool IsVisible { get; set; }
		public bool IsLocked { get; set; }
		public bool IsCollisionLayer { get; set; }
		[Property, Group("Selected Layer")] public TilesetResource TilesetResource { get; set; }

		public Dictionary<string, Tile> Tiles { get; set; }

		[JsonIgnore, Hide] public TilesetComponent TilesetComponent { get; set; }

		public Layer(string name = "Untitled Layer")
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;
			Tiles = new();
		}

		public Layer Copy()
		{
			var layer = new Layer(Name)
			{
				IsVisible = IsVisible,
				IsLocked = IsLocked,
				Tiles = new(),
				IsCollisionLayer = false,
				TilesetComponent = TilesetComponent,
			};

			foreach (var tile in Tiles)
			{
				layer.Tiles[tile.Key] = tile.Value.Copy();
			}

			return layer;
		}

		public void SetTile(Vector2Int position, Guid tileId, Vector2Int cellPosition, Transform transform)
		{
			var tile = new Tile(tileId, cellPosition, transform);
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
		public Guid TileId { get; set; } = Guid.NewGuid();
		public Vector2Int CellPosition { get; set; }
		public bool HorizontalFlip { get; set; }
		public bool VerticalFlip { get; set; }
		public int Rotation { get; set; }
		public Vector2Int BakedPosition { get; set; }

		public Tile() { }

		public Tile(Guid tileId, Vector2Int cellPosition, Transform transform)
		{
			TileId = tileId;
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
			return new Tile(TileId, CellPosition, GetTransform());
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
			var tilemap = tileset.TileMap;

			var groups = layer.Tiles.GroupBy(x => layer.TilesetResource.FilePath);
			foreach (var group in groups)
			{
				var atlas = group.Key ?? "";
				var material = GetMaterial(atlas);

				var totalTiles = group.Count();
				var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles * 6);

				foreach (var tile in group)
				{
					Vector2Int offsetPos = Vector2Int.Zero;
					if (tile.Value.TileId == default) offsetPos = tile.Value.BakedPosition;
					else
					{
						if (!tilemap.ContainsKey(tile.Value.TileId)) continue;
						offsetPos = tilemap[tile.Value.TileId].Position;
					}
					var transform = tile.Value.GetTransform();
					var offset = tileset.GetOffset(offsetPos);
					if (tile.Value.HorizontalFlip)
						offset.x = -offset.x - tiling.x;
					if (!tile.Value.VerticalFlip)
						offset.y = -offset.y - tiling.y;

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