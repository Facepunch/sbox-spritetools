using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Category("2D")]
[Title("2D Tileset Component")]
[Icon("calendar_view_month")]
public partial class TilesetComponent : Collider, Component.ExecuteInEditor
{
	/// <summary>
	/// The Layers within the TilesetComponent
	/// </summary>
	[Group("Layers")]
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

	/// <summary>
	/// The distance between layers, this is useful when you want to position things between layers.
	/// </summary>
	[Property, Group("Layers"), ShowIf(nameof(TilesetComponent.HasMultipleLayers), true)]
	public float LayerDistance { get; set; } = 1f;
	bool HasMultipleLayers => Layers.Count > 1;

	/// <summary>
	/// Whether or not the component should generate a collider based on the specified Collision Layer
	/// </summary>
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

	/// <summary>
	/// The width of the generated collider
	/// </summary>
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
	List<TilesetSceneObject> _sos = new();

	protected override void OnStart()
	{
		base.OnStart();

		RebuildMesh();
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		if (Layers is null) return;
		foreach (var layer in Layers)
		{
			layer.TilesetComponent = this;
		}
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		foreach (var _so in _sos)
		{
			_so.Delete();
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		_sos ??= new();
		Layers ??= new();
		if (Layers.Count != _sos.Count)
		{
			RebuildSceneObjects();
		}

		if (IsDirty)
		{
			IsDirty = false;
			RebuildMesh();
		}
	}

	protected override void OnTagsChanged()
	{
		base.OnTagsChanged();

		foreach (var _so in _sos)
			_so?.Tags.SetFrom(Tags);
	}

	protected override void OnPreRender()
	{
		base.OnPreRender();

		if (Layers is null) return;
		if (Layers.Count == 0)
		{
			return;
		}

		foreach (var _so in _sos)
		{
			if (!_so.IsValid()) continue;
			_so.RenderingEnabled = true;
			_so.Transform = Transform.World;
			_so.Flags.CastShadows = false;
			_so.Flags.IsOpaque = false;
			_so.Flags.IsTranslucent = true;
		}
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
			}
		}

		foreach (var _so in _sos)
		{
			if (!_so.IsValid()) continue;
			using (Gizmo.Scope("tileset"))
			{
				Gizmo.Draw.Color = Color.Yellow;
				Gizmo.Draw.LineThickness = 1f;
				Gizmo.Draw.LineBBox(_so.Bounds);
			}
		}
	}

	void RebuildSceneObjects()
	{
		foreach (var _so in _sos)
		{
			_so.Delete();
		}

		_sos = new List<TilesetSceneObject>();
		foreach (var layer in Layers)
		{
			_sos.Add(new TilesetSceneObject(this, Scene.SceneWorld, layer));
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
			tilePositions[tile.Key] = true;
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

	public Layer GetLayerFromName(string name)
	{
		return Layers.FirstOrDefault(x => x.Name == name);
	}

	public Layer GetLayerFromIndex(int index)
	{
		if (index < 0 || index >= Layers.Count) return null;
		return Layers[index];
	}

	public class Layer
	{
		/// <summary>
		/// The name of the Layer
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Whether or not this Layer is currently being rendered
		/// </summary>
		public bool IsVisible { get; set; }

		/// <summary>
		/// Whether or not this Layer is locked. Locked Layers will ignore any attempted changes
		/// </summary>
		public bool IsLocked { get; set; }

		/// <summary>
		/// Whether or not this Layer dictates the collision mesh
		/// </summary>
		public bool IsCollisionLayer { get; set; }

		/// <summary>
		/// The Tileset that this Layer uses
		/// </summary>
		[Property, Group("Selected Layer")] public TilesetResource TilesetResource { get; set; }

		/// <summary>
		/// A dictionary of all Tiles in the layer by their position
		/// </summary>
		public Dictionary<Vector2Int, Tile> Tiles { get; set; }

		/// <summary>
		/// The TilesetComponent that this Layer belongs to
		/// </summary>
		[JsonIgnore, Hide] public TilesetComponent TilesetComponent { get; set; }

		public Layer(string name = "Untitled Layer")
		{
			Name = name;
			IsVisible = true;
			IsLocked = false;
			Tiles = new();
		}

		/// <summary>
		/// Returns an exact copy of the Layer
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Set a tile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="position"></param>
		/// <param name="tileId"></param>
		/// <param name="cellPosition"></param>
		/// <param name="angle"></param>
		/// <param name="flipX"></param>
		/// <param name="flipY"></param>
		/// <param name="rebuild"></param>
		public void SetTile(Vector2Int position, Guid tileId, Vector2Int cellPosition = default, int angle = 0, bool flipX = false, bool flipY = false, bool rebuild = true)
		{
			if (IsLocked) return;
			var tile = new Tile(tileId, cellPosition, angle, flipX, flipY);
			Tiles[position] = tile;
			if (rebuild && TilesetComponent.IsValid())
				TilesetComponent.IsDirty = true;
		}

		/// <summary>
		/// Get the Tile at the specified position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Tile GetTile(Vector2Int position)
		{
			return Tiles[position];
		}

		/// <summary>
		/// Get the Tile at the specified position
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		public Tile GetTile(Vector3 position)
		{
			return Tiles[new Vector2Int((int)position.x, (int)position.y)];
		}

		/// <summary>
		/// Remove the Tile at the specified position. Will fail if IsLocked is true.
		/// </summary>
		/// <param name="position"></param>
		public void RemoveTile(Vector2Int position)
		{
			if (IsLocked) return;
			Tiles.Remove(position);
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
	TilesetComponent.Layer Layer;

	public TilesetSceneObject(TilesetComponent component, SceneWorld world, TilesetComponent.Layer layer) : base(world)
	{
		Component = component;
		Layer = layer;

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

		if (Layer?.IsVisible != true) return;

		int i = 0;
		int layerIndex = layers.IndexOf(Layer);

		{
			var tileset = Layer.TilesetResource;
			if (tileset is null) return;
			var tilemap = tileset.TileMap;

			var combo = GetMaterial(tileset);
			if (combo.Item1 is null || combo.Item2 is null) return;

			var tiling = combo.Item1.GetTiling();
			var totalTiles = Layer.Tiles.Where(x => x.Value.TileId == default || tilemap.ContainsKey(x.Value.TileId));
			var vertex = ArrayPool<Vertex>.Shared.Rent(totalTiles.Count() * 6);

			var minPosition = new Vector3(int.MaxValue, int.MaxValue, int.MaxValue);
			var maxPosition = new Vector3(int.MinValue, int.MinValue, int.MinValue);

			foreach (var tile in Layer.Tiles)
			{
				var pos = tile.Key;
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
				var position = new Vector3(pos.x, pos.y, layerIndex) * new Vector3(size.x, size.y, Component.LayerDistance);

				minPosition = Vector3.Min(minPosition, position);
				maxPosition = Vector3.Max(maxPosition, position);

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

			var siz = tileset.GetTileSize();
			maxPosition += new Vector3(siz.x, siz.y, 0);
			Bounds = new BBox(minPosition, maxPosition + Vector3.Down * 0.01f);


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
		var texture = TileAtlas.FromTileset(resource);

		if (Materials.TryGetValue(resource, out var combo))
		{
			combo.Item2.Set("Texture", texture);
		}
		else
		{
			var material = Material.Load("materials/sprite_2d.vmat").CreateCopy();
			material.Set("Texture", texture);
			combo.Item1 = texture;
			combo.Item2 = material;
			Materials.Add(resource, combo);
		}

		return combo;
	}
}