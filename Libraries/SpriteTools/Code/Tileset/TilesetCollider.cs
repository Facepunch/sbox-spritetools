using Sandbox;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace SpriteTools;

[Hide]
public partial class TilesetCollider : Collider, Component.ExecuteInEditor
{
	internal TilesetComponent Tileset { get; set; }

	internal bool IsDirty = true;
	private Model CollisionMesh { get; set; }
	private List<Vector3> CollisionVertices { get; set; } = new();
	private List<int[]> CollisionFaces { get; set; } = new();

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsDirty)
		{
			IsDirty = false;
			RebuildMesh();
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
	}

	internal void RebuildMesh()
	{
		if (CollisionMesh is not null)
		{
			CollisionMesh = null;
			CollisionVertices.Clear();
			CollisionFaces.Clear();
		}
		CollisionBoxes.Clear();

		if (!(Tileset?.HasCollider ?? false)) return;
		if (Tileset.Layers is null) return;

		var collisionLayer = Tileset.Layers.FirstOrDefault(x => x.IsCollisionLayer);
		if (collisionLayer is null) collisionLayer = Tileset.Layers.FirstOrDefault();
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
		var firstResource = Tileset.Layers[0].TilesetResource;
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

					AddRectangle(CollisionVertices, CollisionFaces, tiles, x, y, width, height, tileSize, Tileset.ColliderWidth, minPosition, CollisionBoxes);
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
		if (!(Tileset?.HasCollider ?? false)) yield break;

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
}