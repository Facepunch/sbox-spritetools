using Editor;
using Sandbox;
using SpriteTools.TilesetTool.Tools;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetTool;

[EditorTool]
[Title("Tileset Tool")]
[Description("Paint 2D tiles from a tileset")]
[Icon("dashboard")]
[Group("7")]
public partial class TilesetTool : EditorTool
{
	public static TilesetTool Active { get; private set; }

	[Property] public int Angle { get; set; } = 0;
	[Property] public bool HorizontalFlip { get; set; } = false;
	[Property] public bool VerticalFlip { get; set; } = false;

	public override IEnumerable<EditorTool> GetSubtools()
	{
		yield return new PaintTileTool(this);
		yield return new EraserTileTool(this);
		yield return new LineTileTool(this);
		yield return new RectangleTileTool(this);
	}

	public TilesetComponent SelectedComponent;
	public TilesetComponent.Layer SelectedLayer
	{
		get => _selectedLayer;
		set
		{
			if (_selectedLayer == value) return;

			_selectedLayer = value;
			if (value is not null)
			{
				_sceneObject?.UpdateTileset(value.TilesetResource);
				SelectedTile = value?.TilesetResource?.Tiles?.FirstOrDefault();
				if (!string.IsNullOrEmpty(_selectedLayer?.TilesetResource?.FilePath))
				{
					TilesetToolInspector.Active?.Preview?.UpdateTexture(_selectedLayer.TilesetResource.FilePath);
				}
			}
		}
	}
	TilesetComponent.Layer _selectedLayer;

	public TilesetResource.Tile SelectedTile
	{
		get => _selectedTile;
		set
		{
			if (_selectedTile == value) return;

			_selectedTile = value;
			_sceneObject.MultiTilePositions.Clear();
			UpdateInspector?.Invoke();
		}
	}
	TilesetResource.Tile _selectedTile;

	internal Action UpdateInspector;

	bool WasGridActive = true;
	Vector2Int GridSize => SelectedLayer?.TilesetResource?.TileSize ?? new Vector2Int(32, 32);

	internal TilesetPreviewObject _sceneObject;

	public override void OnEnabled()
	{
		Active = this;

		base.OnEnabled();

		AllowGameObjectSelection = false;
		Selection.Clear();
		Selection.Set(this);

		InitGrid();
		InitPreviewObject();
		UpdateComponent();

		if (SelectedComponent.IsValid())
		{
			foreach (var layer in SelectedComponent.Layers)
			{
				layer?.TilesetResource?.InternalUpdateTiles();
			}
		}
	}

	public override void OnDisabled()
	{
		Active = null;

		base.OnDisabled();

		ResetGrid();
		RemovePreviewObject();
	}

	public override void OnUpdate()
	{
		base.OnUpdate();

		if (SceneViewportWidget.LastSelected?.SceneView?.Tools?.CurrentTool?.CurrentTool is null)
		{
			_sceneObject.RenderingEnabled = false;
		}

		var state = SceneViewportWidget.LastSelected.State;
		var gridSize = GridSize * (SelectedLayer.TilesetResource?.TileScale ?? 1f);
		using (Gizmo.Scope("grid"))
		{
			Gizmo.Draw.IgnoreDepth = state.Is2D;
			Gizmo.Draw.Grid(state.GridAxis, gridSize, state.GridOpacity);
		}
	}

	void UpdateComponent()
	{
		var component = Scene.GetAllComponents<TilesetComponent>().FirstOrDefault();

		if (!component.IsValid())
		{
			var go = new GameObject()
			{
				Name = "Tileset Object"
			};
			component = go.Components.GetOrCreate<TilesetComponent>();
		}

		if (component.IsValid())
		{
			SelectedComponent = component;
			SelectedLayer = SelectedComponent?.Layers?.FirstOrDefault();
		}
	}

	internal void PlaceTile(Vector2Int position, Guid tileId, Vector2Int cellPosition, bool rebuild = true)
	{
		if (SelectedLayer is null) return;

		SelectedLayer.SetTile(position, tileId, cellPosition, Angle, HorizontalFlip, VerticalFlip);
		if (rebuild) SelectedComponent.IsDirty = true;
	}

	internal void EraseTile(Vector2 position, bool rebuild = true)
	{
		if (SelectedLayer is null) return;

		SelectedLayer.RemoveTile((Vector2Int)position);
		if (rebuild) SelectedComponent.IsDirty = true;
	}

	void InitPreviewObject()
	{
		RemovePreviewObject();

		_sceneObject = new TilesetPreviewObject(this, Scene.SceneWorld);
		if (SelectedLayer is not null)
			_sceneObject.UpdateTileset(SelectedLayer.TilesetResource);

		_sceneObject.Flags.CastShadows = false;
		_sceneObject.Flags.IsOpaque = false;
		_sceneObject.Flags.IsTranslucent = true;
		_sceneObject.RenderingEnabled = true;
	}

	void RemovePreviewObject()
	{
		_sceneObject?.Delete();
		_sceneObject = null;
	}

	void InitGrid()
	{
		WasGridActive = SceneViewportWidget.LastSelected.State.ShowGrid;
		SceneViewportWidget.LastSelected.State.ShowGrid = false;
	}

	void ResetGrid()
	{
		SceneViewportWidget.LastSelected.State.ShowGrid = WasGridActive;
	}

	[Shortcut("tileset-tools.tileset-tool", "SHIFT+T", typeof(SceneViewportWidget))]
	public static void ActivateSubTool()
	{
		EditorToolManager.SetTool(nameof(TilesetTool));
	}

	[Shortcut("tileset-tools.rotate-left", "Q", typeof(SceneViewportWidget))]
	public static void RotateLeft()
	{
		if (Active is null) return;
		Active.Angle = (Active.Angle + 90) % 360;
	}

	[Shortcut("tileset-tools.rotate-right", "W", typeof(SceneViewportWidget))]
	public static void RotateRight()
	{
		if (Active is null) return;
		Active.Angle -= 90;
		if (Active.Angle < 0)
		{
			Active.Angle += 360;
		}
	}

	[Shortcut("tileset-tools.flip-horizontal", "A", typeof(SceneViewportWidget))]
	public static void FlipHorizontal()
	{
		if (Active is null) return;
		Active.HorizontalFlip = !Active.HorizontalFlip;
	}

	[Shortcut("tileset-tools.flip-vertical", "S", typeof(SceneViewportWidget))]
	public static void FlipVertical()
	{
		if (Active is null) return;
		Active.VerticalFlip = !Active.VerticalFlip;
	}

}

internal sealed class TilesetPreviewObject : SceneCustomObject
{
	TilesetTool Tool;
	Material Material;

	internal List<(Vector2Int, Vector2Int)> MultiTilePositions = new();

	public TilesetPreviewObject(TilesetTool tool, SceneWorld world) : base(world)
	{
		Tool = tool;
	}

	internal void UpdateTileset(TilesetResource tileset)
	{
		if (tileset is null) return;
		Material = Material.Load("materials/sprite_2d.vmat").CreateCopy();
		Material.Set("Texture", Texture.Load(Sandbox.FileSystem.Mounted, tileset.FilePath));
	}

	internal void ClearPositions()
	{
		MultiTilePositions.Clear();
	}

	internal void SetPositions(List<Vector2Int> positions, List<Vector2Int> cellPositions = null)
	{
		if (cellPositions is null)
		{
			MultiTilePositions = positions.Select(x => (x, new Vector2Int(-1, -1))).ToList();
			return;
		}

		MultiTilePositions.Clear();
		for (int i = 0; i < positions.Count; i++)
		{
			if (i >= cellPositions.Count)
				MultiTilePositions.Add((positions[i], new Vector2Int(-1, -1)));
			else
				MultiTilePositions.Add((positions[i], cellPositions[i]));
		}
	}

	internal void SetPositions(List<Vector2> positions, List<Vector2> cellPositions = null)
	{
		if (cellPositions is null)
		{
			MultiTilePositions = positions.Select(x => ((Vector2Int)x, new Vector2Int(-1, -1))).ToList();
			return;
		}

		MultiTilePositions.Clear();
		for (int i = 0; i < positions.Count; i++)
		{
			if (i >= cellPositions.Count)
				MultiTilePositions.Add(((Vector2Int)positions[i], new Vector2Int(-1, -1)));
			else
				MultiTilePositions.Add(((Vector2Int)positions[i], (Vector2Int)cellPositions[i]));
		}
	}

	internal void SetPositions(List<(Vector2Int, Vector2Int)> positions)
	{
		MultiTilePositions = positions;
	}

	public override void RenderSceneObject()
	{
		if (Material is null) return;

		var selectedTile = Tool?.SelectedTile;
		if (selectedTile is null) return;

		var layer = Tool?.SelectedLayer;
		if (layer is null) return;

		var tileset = selectedTile.Tileset;
		if (tileset is null) return;

		var tileSize = tileset.GetTileSize();
		var scale = Vector2Int.One;
		// if (TilesetTool.Active.CurrentTool is PaintTileTool) scale = selectedTile.Size;
		var tiling = tileset.GetTiling() * scale;

		var positions = MultiTilePositions.ToList();
		if (positions.Count == 0) positions.Add((Vector2Int.Zero, new Vector2Int(-1, -1)));

		int i = 0;
		var vertexCount = positions.Count * 6;
		var vertex = ArrayPool<Vertex>.Shared.Rent(vertexCount);

		foreach (var pos in positions)
		{
			var offsetPos = pos.Item1;
			var tilePosition = (pos.Item2.x == -1 || pos.Item2.y == -1) ? selectedTile.Position : pos.Item2;

			if (Tool.Angle == 90)
				offsetPos = new Vector2Int(-offsetPos.y, offsetPos.x);
			else if (Tool.Angle == 180)
				offsetPos = new Vector2Int(-offsetPos.x, -offsetPos.y);
			else if (Tool.Angle == 270)
				offsetPos = new Vector2Int(offsetPos.y, -offsetPos.x);
			var offset = tileset.GetOffset(tilePosition);

			var position = new Vector3(offsetPos.x * tileSize.x, offsetPos.y * tileSize.y, 0) - new Vector3(0, (scale.y - 1) * tileSize.y, 0);
			var size = tileSize * scale;

			var topLeft = new Vector3(position.x, position.y, position.z);
			var topRight = new Vector3(position.x + size.x, position.y, position.z);
			var bottomRight = new Vector3(position.x + size.x, position.y + size.y, position.z);
			var bottomLeft = new Vector3(position.x, position.y + size.y, position.z);


			if (Tool.HorizontalFlip) offset.x = -offset.x - tiling.x;
			if (!Tool.VerticalFlip) offset.y = -offset.y - tiling.y;

			var uvTopLeft = new Vector2(offset.x, offset.y);
			var uvTopRight = new Vector2(offset.x + tiling.x, offset.y);
			var uvBottomRight = new Vector2(offset.x + tiling.x, offset.y + tiling.y);
			var uvBottomLeft = new Vector2(offset.x, offset.y + tiling.y);

			if (Tool.Angle == 90)
			{
				var tempUv = uvTopLeft;
				uvTopLeft = uvBottomLeft;
				uvBottomLeft = uvBottomRight;
				uvBottomRight = uvTopRight;
				uvTopRight = tempUv;
			}
			else if (Tool.Angle == 180)
			{
				var tempUv = uvTopLeft;
				uvTopLeft = uvBottomRight;
				uvBottomRight = tempUv;
				tempUv = uvTopRight;
				uvTopRight = uvBottomLeft;
				uvBottomLeft = tempUv;
			}
			else if (Tool.Angle == 270)
			{
				var tempUv = uvTopLeft;
				uvTopLeft = uvTopRight;
				uvTopRight = uvBottomRight;
				uvBottomRight = uvBottomLeft;
				uvBottomLeft = tempUv;
			}

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

			vertex[i] = new Vertex(topLeft);
			vertex[i].TexCoord0 = uvTopLeft;
			vertex[i].TexCoord1 = new Vector4(0, 0, 0, 0);
			vertex[i].Color = Color.White;
			vertex[i].Normal = Vector3.Up;
			i++;
		}

		Graphics.Draw(vertex, vertexCount, Material, Attributes);
		ArrayPool<Vertex>.Shared.Return(vertex);
	}
}