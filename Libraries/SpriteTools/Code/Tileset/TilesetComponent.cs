using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading;

namespace SpriteTools;

[Category("2D")]
[Title("2D Tileset Component")]
[Icon("calendar_view_month")]
public sealed class TilesetComponent : Collider, Component.ExecuteInEditor
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
			RebuildMesh();
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
			RebuildMesh();
		}
	}
	float _colliderWidth;

	public bool IsDirty = false;

	private Model CollisionMesh { get; set; }
	private List<Vector3> CollisionVertices { get; set; } = new();
	private List<int[]> CollisionFaces { get; set; } = new();

	TilesetSceneObject _so;

	protected override void OnStart()
	{
		base.OnStart();

		RebuildMesh();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

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
		base.OnDisabled();

		_so?.Delete();
		_so = null;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsDirty)
		{
			IsDirty = false;
			RebuildMesh();
		}
	}

	protected override void OnTagsChanged()
	{
		base.OnTagsChanged();

		_so?.Tags.SetFrom(Tags);
	}

	protected override void OnPreRender()
	{
		base.OnPreRender();

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

		if (!Gizmo.IsSelected) return;

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

				Gizmo.Draw.Color = Color.Yellow;
				var bbox = CollisionMesh.Bounds;
				if (bbox.Size.Length < 0.1f) bbox = new BBox(bbox.Center - Vector3.One * 0.1f, bbox.Center + Vector3.One * 0.1f);
				Gizmo.Draw.LineBBox(bbox);
			}
		}
	}

	void RebuildMesh()
	{
		if (CollisionMesh is not null)
		{
			CollisionMesh = null;
			CollisionVertices.Clear();
			CollisionFaces.Clear();
		}
		CollisionBoxes.Clear();

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
		var firstResource = Layers[0].TilesetResource;
		var tileSize = (Vector2)firstResource.GetTileSize();
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

					AddRectangle(CollisionVertices, CollisionFaces, tiles, x, y, width, height, tileSize, ColliderWidth, minPosition, CollisionBoxes);
				}
			}
		}

		var hVertices = mesh.AddVertices(CollisionVertices.ToArray());
		var faceMat = Material.Load("materials/dev/reflectivity_30.vmat");

		foreach (var face in CollisionFaces)
		{
			var faceIndex = mesh.AddFace(face.Select(x => hVertices[x]).ToArray());
			mesh.SetFaceMaterial(faceIndex, faceMat);
		}
		mesh.Transform = Transform.World;

		CollisionMesh = mesh.Rebuild();
		RebuildImmediately();
		RebuildBounds();
	}

	void RebuildBounds()
	{
		var minPosition = Vector3.One * float.MaxValue;
		var maxPosition = Vector3.One * float.MinValue;

		foreach (var layer in Layers)
		{
			var size = layer.TilesetResource.GetCurrentTileSize();
			foreach (var tile in layer.Tiles)
			{
				var pos = Vector2Int.Parse(tile.Key);
				var position = new Vector3(pos.x * size.x, pos.y * size.y, 0);

				minPosition = Vector3.Min(minPosition, position);
				maxPosition = Vector3.Max(maxPosition, position + new Vector3(size.x, size.y, 0));
			}
		}

		minPosition.z -= 2;
		_so.Bounds = new BBox(minPosition, maxPosition).Translate(Transform.Position);
	}

	List<BBox> CollisionBoxes = new();
	protected override IEnumerable<PhysicsShape> CreatePhysicsShapes(PhysicsBody targetBody)
	{
		if (!HasCollider) yield break;

		if (CollisionBoxes.Count > 0)
		{
			foreach (var box in CollisionBoxes)
			{
				var shape = targetBody.AddBoxShape(box, Rotation.Identity, true);
				yield return shape;
			}
		}
		else
		{
			if (CollisionMesh is null) yield break;
			if (CollisionMesh.Physics is null) yield break;

			var bodyTransform = targetBody.Transform.ToLocal(Transform.World);

			foreach (var part in CollisionMesh.Physics.Parts)
			{
				var bx = bodyTransform.ToWorld(part.Transform);

				foreach (var mesh in part.Meshes)
				{
					var shape = targetBody.AddShape(mesh, bx, false, true);
					shape.Surface = mesh.Surface;
					shape.Surfaces = mesh.Surfaces;
					yield return shape;
				}
			}
		}
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

	static void AddRectangle(List<Vector3> vertices, List<int[]> faces, bool[,] grid, int x, int y, int width, int height, Vector2 tileSize, float depth, Vector2Int minPosition, List<BBox> boxes)
	{
		int startIndex = vertices.Count;
		float currentDepth = MathF.Abs(depth);
		float z = currentDepth / 2f;

		// Top Face
		var v0 = new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y) * tileSize.x, z);
		var v1 = new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y) * tileSize.y, z);
		var v2 = new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y + height) * tileSize.y, z);
		var v3 = new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y + height) * tileSize.y, z);
		AddFace(vertices, faces, v0, v1, v2, v3);

		if (depth == 0) return;

		// Bottom Face
		z -= currentDepth;
		var v4 = new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y) * tileSize.y, z);
		var v5 = new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y) * tileSize.y, z);
		var v6 = new Vector3((minPosition.x + x + width) * tileSize.x, (minPosition.y + y + height) * tileSize.y, z);
		var v7 = new Vector3((minPosition.x + x) * tileSize.x, (minPosition.y + y + height) * tileSize.y, z);
		AddFace(vertices, faces, v4, v5, v6, v7);

		boxes.Add(new BBox(v0, v6));

		// Add indices for the sides if not inner
		if (IsExposedFace(grid, x, y, 1, height, -1, 0)) // Left
		{
			AddFace(vertices, faces, v0, v3, v7, v4);
		}
		if (IsExposedFace(grid, x + width - 1, y, 1, height, 1, 0)) // Right
		{
			AddFace(vertices, faces, v2, v1, v5, v6);
		}
		if (IsExposedFace(grid, x, y, width, 1, 0, -1)) // Front
		{
			AddFace(vertices, faces, v1, v0, v4, v5);
		}
		if (IsExposedFace(grid, x, y + height - 1, width, 1, 0, 1)) // Back
		{
			AddFace(vertices, faces, v3, v2, v6, v7);
		}
	}

	static void AddFace(List<Vector3> vertices, List<int[]> faces, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		var startIndex = vertices.Count;
		vertices.AddRange(new Vector3[] { a, b, c, d });
		faces.Add(new int[] { startIndex, startIndex + 1, startIndex + 2 });
		faces.Add(new int[] { startIndex + 2, startIndex + 3, startIndex + 0 });
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

		public void SetTile(Vector2Int position, Guid tileId, Vector2Int cellPosition, int angle, bool flipX, bool flipY)
		{
			var tile = new Tile(tileId, cellPosition, angle, flipX, flipY);
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

		public Tile(Guid tileId, Vector2Int cellPosition, int rotation, bool flipX, bool flipY)
		{
			TileId = tileId;
			CellPosition = cellPosition;
			HorizontalFlip = flipX;
			VerticalFlip = flipY;
			Rotation = rotation;
		}

		public Tile Copy()
		{
			return new Tile(TileId, CellPosition, Rotation, HorizontalFlip, VerticalFlip);
		}
	}

}

internal sealed class TilesetSceneObject : SceneCustomObject
{
	TilesetComponent Component;
	Dictionary<TilesetResource, (TileAtlas, Material)> Materials = new();
	Material MissingMaterial;

	public TilesetSceneObject(TilesetComponent component, SceneWorld world) : base(world)
	{
		Component = component;

		MissingMaterial = Material.Load("materials/sprite_2d.vmat").CreateCopy();
		MissingMaterial.Set("Texture", Texture.Load("images/missing-tile.png"));
	}

	public override void RenderSceneObject()
	{
		if (Component?.Layers is null) return;

		var layers = Component.Layers.ToList();
		layers.Reverse();
		if (layers.Count == 0) return;

		Dictionary<Vector2Int, TilesetComponent.Tile> missingTiles = new();

		foreach (var layer in layers)
		{
			if (!layer.IsVisible) continue;

			int i = 0;
			int layerIndex = layers.IndexOf(layer);

			var tileset = layer.TilesetResource;
			if (tileset is null) continue;
			var tilemap = tileset.TileMap;

			var combo = GetMaterial(tileset);
			var tiling = combo.Item1.GetTiling();
			var totalTiles = layer.Tiles.Where(x => x.Value.TileId == default || tilemap.ContainsKey(x.Value.TileId));
			var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles.Count() * 6);

			foreach (var tile in layer.Tiles)
			{
				var pos = Vector2Int.Parse(tile.Key);
				Vector2Int offsetPos = Vector2Int.Zero;
				if (tile.Value.TileId == default) offsetPos = tile.Value.BakedPosition;
				else
				{
					if (!tilemap.ContainsKey(tile.Value.TileId))
					{
						missingTiles[pos] = tile.Value;
						continue;
					}
					offsetPos = tilemap[tile.Value.TileId].Position;
				}
				var offset = combo.Item1.GetOffset(offsetPos + tile.Value.CellPosition);
				if (tile.Value.HorizontalFlip)
					offset.x = -offset.x - tiling.x;
				if (!tile.Value.VerticalFlip)
					offset.y = -offset.y - tiling.y;


				var size = tileset.GetTileSize();
				var position = new Vector3(pos.x, pos.y, layerIndex) * new Vector3(size.x, size.y, 1);

				var topLeft = new Vector3(position.x, position.y, position.z);
				var topRight = new Vector3(position.x + size.x, position.y, position.z);
				var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z);
				var bottomLeft = new Vector3(position.x, position.y + size.y, position.z);

				var uvTopLeft = new Vector2(offset.x, offset.y);
				var uvTopRight = new Vector2(offset.x + tiling.x, offset.y);
				var uvBottomRight = new Vector2(offset.x + tiling.x, offset.y + tiling.y);
				var uvBottomLeft = new Vector2(offset.x, offset.y + tiling.y);

				if (tile.Value.Rotation == 90)
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvBottomLeft;
					uvBottomLeft = uvBottomRight;
					uvBottomRight = uvTopRight;
					uvTopRight = tempUv;
				}
				else if (tile.Value.Rotation == 180)
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvBottomRight;
					uvBottomRight = tempUv;
					tempUv = uvTopRight;
					uvTopRight = uvBottomLeft;
					uvBottomLeft = tempUv;
				}
				else if (tile.Value.Rotation == 270)
				{
					var tempUv = uvTopLeft;
					uvTopLeft = uvTopRight;
					uvTopRight = uvBottomRight;
					uvBottomRight = uvBottomLeft;
					uvBottomLeft = tempUv;
				}

				vertex[i] = new Vertex(topLeft);
				vertex[i].TexCoord0 = uvTopLeft;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex(topRight);
				vertex[i].TexCoord0 = uvTopRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex(bottomRight);
				vertex[i].TexCoord0 = uvBottomRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex(topLeft);
				vertex[i].TexCoord0 = uvTopLeft;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex(bottomRight);
				vertex[i].TexCoord0 = uvBottomRight;
				vertex[i].Normal = Vector3.Up;
				i++;

				vertex[i] = new Vertex(bottomLeft);
				vertex[i].TexCoord0 = uvBottomLeft;
				vertex[i].Normal = Vector3.Up;
				i++;
			}

			Graphics.Draw(vertex, totalTiles.Count() * 6, combo.Item2, Attributes);
			ArrayPool<Vertex>.Shared.Return(vertex);
		}

		if (missingTiles.Count > 0)
		{
			var uvTopLeft = new Vector2(0, 0);
			var uvTopRight = new Vector2(1, 0);
			var uvBottomRight = new Vector2(1, 1);
			var uvBottomLeft = new Vector2(0, 1);

			foreach (var tile in missingTiles)
			{
				var material = MissingMaterial;
				var pos = tile.Key;
				var size = Component.Layers[0].TilesetResource.TileSize;
				var position = new Vector3(pos.x, pos.y, 0) * new Vector3(size.x, size.y, 1);

				var topLeft = new Vector3(position.x, position.y, position.z);
				var topRight = new Vector3(position.x + size.x, position.y, position.z);
				var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z);
				var bottomLeft = new Vector3(position.x, position.y + size.y, position.z);

				var vertex = new Vertex[]
				{
				new Vertex(topLeft) { TexCoord0 = uvTopLeft, Normal = Vector3.Up },
				new Vertex(topRight) { TexCoord0 = uvTopRight, Normal = Vector3.Up },
				new Vertex(bottomRight) { TexCoord0 = uvBottomRight, Normal = Vector3.Up },
				new Vertex(topLeft) { TexCoord0 = uvTopLeft, Normal = Vector3.Up },
				new Vertex(bottomRight) { TexCoord0 = uvBottomRight, Normal = Vector3.Up },
				new Vertex(bottomLeft) { TexCoord0 = uvBottomLeft, Normal = Vector3.Up },
				};

				Graphics.Draw(vertex, 6, material, Attributes);
			}
		}
	}

	(TileAtlas, Material) GetMaterial(TilesetResource resource)
	{
		if (!Materials.TryGetValue(resource, out var combo))
		{
			var texture = TileAtlas.FromTileset(resource);
			var material = Material.Load("materials/sprite_2d.vmat").CreateCopy();
			material.Set("Texture", texture);
			combo.Item1 = texture;
			combo.Item2 = material;
			Materials.Add(resource, combo);
		}

		return combo;
	}
}