using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetEditor;

public class TilesetTileControl : Widget
{
	TilesetTileListControl ParentList;
	internal TilesetResource.Tile Tile;

	internal LabelTextEntry labelText;

	Drag dragData;
	bool draggingAbove = false;
	bool draggingBelow = false;

	Pixmap Pixmap;

	public TilesetTileControl(TilesetTileListControl list, TilesetResource.Tile tile)
	{
		ParentList = list;
		Tile = tile;

		MouseClick = () =>
		{
			ParentList.SelectTile(this, Tile);
		};

		VerticalSizeMode = SizeMode.Flexible;

		StatusTip = $"Select Tile \"{Tile.Name}\"";
		Cursor = CursorShape.Finger;

		Layout = Layout.Row();
		Layout.AddSpacingCell(20);
		Layout.Margin = 4;
		Layout.Spacing = 4;

		LoadPixmap();

		var serializedObject = Tile.GetSerialized();
		serializedObject.TryGetProperty(nameof(TilesetResource.Tile.Name), out var name);
		labelText = new LabelTextEntry(name);
		labelText.EmptyValue = $"Tile {tile.Position}";
		Layout.Add(labelText);

		IsDraggable = true;
		AcceptDrops = true;
	}

	public override void OnDestroyed()
	{
		base.OnDestroyed();

		ParentList.Selected.Remove(this);
	}

	void LoadPixmap()
	{
		var tileset = Tile.Tileset;
		if (tileset is null) return;
		var rect = new Rect(Tile.Position, Tile.Size);
		rect.Position = rect.Position * tileset.TileSize + rect.Position * tileset.TileSeparation;
		rect.Width *= tileset.TileSize.x;
		rect.Height *= tileset.TileSize.y;
		Pixmap = PixmapCache.Get(tileset.FilePath, rect);
	}

	protected override void OnPaint()
	{
		if (dragData?.IsValid ?? false)
		{
			Paint.SetBrushAndPen(Theme.Black.WithAlpha(0.5f));
			Paint.DrawRect(LocalRect, 4);
		}
		else if (ParentList.Selected.Contains(this))
		{
			Paint.SetBrushAndPen(Theme.Selection.Darken(0.5f));
			Paint.DrawRect(LocalRect, 4);
		}
		else if (IsUnderMouse)
		{
			Paint.SetBrushAndPen(Theme.White.WithAlpha(0.1f));
			Paint.DrawRect(LocalRect, 4);
		}

		if (Pixmap is not null)
		{
			var pixRect = Rect.FromPoints(LocalRect.TopLeft, LocalRect.TopLeft + new Vector2(16, 16));
			pixRect.Position = pixRect.Position + new Vector2(3, LocalRect.Height / 2 - 7);
			Paint.Draw(pixRect, Pixmap);
		}

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
		labelText.Edit();
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
		if (ParentList.Selected.Contains(this))
		{
			if (ParentList.Selected.Count == ParentList.Buttons.Count)
			{
				ParentList.DeleteAll();
			}
			else
			{
				foreach (var selected in ParentList.Selected)
				{
					ParentList.DeleteTile(selected.Tile);
				}
			}
		}
		else
		{
			ParentList.DeleteTile(Tile);
		}
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
		dragData.Data.Object = Tile;
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

		var list = ParentList.SerializedProperty.GetValue<List<TilesetResource.Tile>>();
		var index = list.IndexOf(Tile);
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
		var tile = ev.Data.OfType<TilesetResource.Tile>().FirstOrDefault();

		if (tile == null || ParentList == null) return false;

		var layerList = ParentList.SerializedProperty.GetValue<List<TilesetResource.Tile>>();
		var myIndex = layerList.IndexOf(Tile);
		var otherIndex = layerList.IndexOf(tile);
		if (myIndex == -1 || otherIndex == -1) return false;

		delta = otherIndex - myIndex;
		return true;
	}
}