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

	List<TilesetComponent.Tile> Tiles = new();

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
		var gridSize = GridSize;
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

		SelectedLayer.SetTile(position, tileId, cellPosition, new Transform(0, Rotation.Identity, 1));
		if (rebuild) SelectedComponent.BuildMesh();
	}

	internal void EraseTile(Vector2 position, bool rebuild = true)
	{
		if (SelectedLayer is null) return;

		SelectedLayer.RemoveTile((Vector2Int)position);
		if (rebuild) SelectedComponent.BuildMesh();
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

}

internal sealed class TilesetPreviewObject : SceneCustomObject
{
	TilesetTool Tool;
	Material Material;

	internal List<Vector2Int> MultiTilePositions = new();

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

	internal void SetPositions(List<Vector2Int> positions)
	{
		MultiTilePositions = positions;
	}

	internal void SetPositions(List<Vector2> positions)
	{
		MultiTilePositions = positions.Select(x => new Vector2Int((int)x.x, (int)x.y)).ToList();
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

		var tileSize = tileset.TileSize;
		var tiling = tileset.GetTiling() * selectedTile.Size;
		var offset = tileset.GetOffset(selectedTile.Position);
		offset.y = -offset.y - tiling.y;
		var scale = selectedTile.Size;

		var positions = MultiTilePositions.ToList();
		if (positions.Count == 0) positions.Add(Vector2Int.Zero);

		int i = 0;
		var vertexCount = positions.Count * 6;
		var vertex = ArrayPool<Vertex>.Shared.Rent(vertexCount);

		foreach (var pos in positions)
		{
			var position = new Vector3(pos.x * tileSize.x, pos.y * tileSize.y, 0);
			var size = tileSize * scale;

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
	}

	[Shortcut("tileset-tools.tileset-tool", "SHIFT+T")]
	public static void ActivateSubTool()
	{
		EditorToolManager.SetTool(nameof(TilesetTool));
	}
}