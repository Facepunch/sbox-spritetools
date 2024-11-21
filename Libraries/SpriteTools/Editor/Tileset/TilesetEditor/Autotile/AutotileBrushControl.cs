using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

public class AutotileBrushControl : Widget
{
	internal AutotileBrushListControl ParentList;
	internal AutotileBrush Brush;

	Drag dragData;
	bool draggingAbove = false;
	bool draggingBelow = false;

	Layout TileContent;
	List<AutotileTileControl> TileControls = new();

	public AutotileBrushControl(AutotileBrushListControl list, AutotileBrush brush)
	{
		ParentList = list;
		Brush = brush;

		MouseClick = () =>
		{
			ParentList.SelectBrush(this, Brush);
		};

		VerticalSizeMode = SizeMode.Flexible;

		StatusTip = $"Select Brush \"{brush.Name}\"";
		Cursor = CursorShape.Finger;

		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;

		TileContent = Layout.Add(Layout.Row());
		var tileCount = Brush.Is47Tiles ? 47 : 16;
		// if (Brush.Tiles is null)
		// {
		// 	Brush.Tiles = new AutotileBrush.Tile[tileCount];
		// 	for (int i = 0; i < tileCount; i++)
		// 	{
		// 		Brush.Tiles[i] = new AutotileBrush.Tile();
		// 	}
		// }
		for (int i = 0; i < tileCount; i++)
		{
			var tileControl = new AutotileTileControl(this, i);
			TileContent.Add(tileControl);
			TileControls.Add(tileControl);
		}

		// FixedHeight = Brush.Is47Tiles ? 128 : 64;

		IsDraggable = true;
		AcceptDrops = true;
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		if (ParentList.SelectedBrush == this)
		{
			ParentList.SelectedBrush = null;
			ParentList.SelectedTile = null;
		}
	}

	void LoadPixmap()
	{
		// var tileset = Tile.Tileset;
		// if (tileset is null) return;
		// var rect = new Rect(Tile.Position, Tile.Size);
		// rect.Position = rect.Position * tileset.TileSize + rect.Position * tileset.TileSeparation;
		// rect.Width *= tileset.TileSize.x;
		// rect.Height *= tileset.TileSize.y;
		// Pixmap = PixmapCache.Get(tileset.FilePath, rect);
	}

	protected override void OnPaint()
	{
		MaximumWidth = ParentList.Width - 12;
		if (dragData?.IsValid ?? false)
		{
			Paint.SetBrushAndPen(Theme.Black.WithAlpha(0.5f));
			Paint.DrawRect(LocalRect, 4);
		}
		else if (ParentList.SelectedBrush == this)
		{
			Paint.SetBrushAndPen(Theme.Selection.Darken(0.5f));
			Paint.DrawRect(LocalRect, 4);
		}
		else if (IsUnderMouse)
		{
			Paint.SetBrushAndPen(Theme.White.WithAlpha(0.1f));
			Paint.DrawRect(LocalRect, 4);
		}

		var brushName = string.IsNullOrEmpty(Brush.Name) ? $"Brush {ParentList.Buttons.IndexOf(this) + 1}" : Brush.Name;
		Paint.SetBrushAndPen(Color.Transparent);
		Paint.DrawTextBox(LocalRect.Shrink(6, 4), brushName, Theme.ControlText, 8, 4, TextFlag.LeftTop);

		var tileCount = Brush.Is47Tiles ? 47 : 16;
		Paint.SetBrushAndPen(Theme.Grey);
		var size = 26f;
		var padding = 3;
		var tileWidth = MathF.Floor((Width - size) / size);
		for (int i = 0; i < tileCount; i++)
		{
			var x = i % tileWidth;
			var y = MathF.Floor(i / tileWidth);
			var tileRect = new Rect(LocalRect.TopLeft + new Vector2(4, 20) + new Vector2(x * (size + padding), y * (size + padding)), new Vector2(size, size));
			if (TileControls.ElementAt(i) is AutotileTileControl tileControl)
			{
				tileControl.Position = tileRect.Position;
			}
			//Paint.DrawRect(tileRect, 2);
		}
		var maxY = 1 + MathF.Floor(tileCount / tileWidth);
		FixedHeight = 26 + (size + padding * 2f) * maxY;


		// if (Pixmap is null) LoadPixmap();
		// if (Pixmap is not null)
		// {
		// 	var pixRect = Rect.FromPoints(LocalRect.TopLeft, LocalRect.TopLeft + new Vector2(16, 16));
		// 	pixRect.Position = pixRect.Position + new Vector2(3, LocalRect.Height / 2 - 7);
		// 	Paint.Draw(pixRect, Pixmap);
		// }

		// if (Tile.Tileset.TileTextures.TryGetValue(Tile.Id, out var texture))
		// {
		// 	var pixRect = Rect.FromPoints(LocalRect.TopLeft, LocalRect.TopLeft + new Vector2(16, 16));
		// 	pixRect.Position = pixRect.Position + new Vector2(3, LocalRect.Height / 2 - 7);
		// 	var pixmap = Pixmap.FromTexture(texture);
		// 	Paint.Draw(pixRect, pixmap);
		// }

		if (draggingAbove)
		{
			Paint.SetPen(Theme.Selection, 2f, PenStyle.Dot);
			Paint.DrawLine(LocalRect.TopLeft, LocalRect.TopRight);
			draggingAbove = false;
		}
		else if (draggingBelow)
		{
			Paint.SetPen(Theme.Selection, 2f, PenStyle.Dot);
			Paint.DrawLine(LocalRect.BottomLeft, LocalRect.BottomRight);
			draggingBelow = false;
		}
	}

	void Rename()
	{

	}

	void DeleteLayerPopup()
	{
		var popup = new PopupWidget(ParentList);
		popup.Layout = Layout.Column();
		popup.Layout.Margin = 16;
		popup.Layout.Spacing = 8;

		popup.Layout.Add(new Label($"Are you sure you want to delete this Layer?"));

		var button = new Button.Primary("Delete");


		button.MouseClick = () =>
		{
			Delete();
			popup.Visible = false;
		};

		popup.Layout.Add(button);

		var bottomBar = popup.Layout.AddRow();
		bottomBar.AddStretchCell();
		bottomBar.Add(button);

		var popupPos = new Vector2(Editor.Application.CursorPosition.x - 250, Editor.Application.CursorPosition.y);
		popup.Position = popupPos;
		popup.Visible = true;
	}

	void Delete()
	{
		ParentList.DeleteBrush(Brush);
	}

	protected override void OnContextMenu(ContextMenuEvent e)
	{
		base.OnContextMenu(e);

		var m = new Menu(this);

		m.AddOption("Rename", "edit", Rename);
		m.AddOption("Delete", "delete", Delete);

		m.OpenAtCursor(false);
	}

	protected override void OnDragStart()
	{
		base.OnDragStart();

		dragData = new Drag(this);
		dragData.Data.Object = Brush;
		dragData.Execute();
	}

	public override void OnDragHover(DragEvent ev)
	{
		base.OnDragHover(ev);

		if (!TryDragOperation(ev, out var dragDelta))
		{
			draggingAbove = false;
			draggingBelow = false;
			return;
		}

		draggingAbove = dragDelta > 0;
		draggingBelow = dragDelta < 0;
	}

	public override void OnDragDrop(DragEvent ev)
	{
		base.OnDragDrop(ev);

		if (!TryDragOperation(ev, out var delta)) return;

		var list = ParentList.SerializedProperty.GetValue<List<AutotileBrush>>();
		var index = list.IndexOf(Brush);
		var movingIndex = index + delta;
		var layer = list[movingIndex];

		list.RemoveAt(movingIndex);
		list.Insert(index, layer);

		ParentList.SerializedProperty.SetValue(list);
		ParentList.UpdateList();
	}

	bool TryDragOperation(DragEvent ev, out int delta)
	{
		delta = 0;
		var brush = ev.Data.OfType<AutotileBrush>().FirstOrDefault();

		if (brush == null || ParentList == null) return false;

		var layerList = ParentList.SerializedProperty.GetValue<List<AutotileBrush>>();
		var myIndex = layerList.IndexOf(Brush);
		var otherIndex = layerList.IndexOf(brush);
		if (myIndex == -1 || otherIndex == -1) return false;

		delta = otherIndex - myIndex;
		return true;
	}
}