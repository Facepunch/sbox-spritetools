using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.TilesetTool;

public class TilesetLayerControl : Widget
{
    TilesetLayerListControl ParentList;
    TilesetComponent.Layer Layer;

    LabelTextEntry labelText;

    Drag dragData;
    bool draggingAbove = false;
    bool draggingBelow = false;

    internal Widget icoCollisionLayer;

    public TilesetLayerControl(TilesetLayerListControl list, TilesetComponent.Layer layer)
    {
        ParentList = list;
        Layer = layer;

        VerticalSizeMode = SizeMode.Flexible;

        StatusTip = $"Select Layer \"{Layer.Name}\"";
        Cursor = CursorShape.Finger;

        Layout = Layout.Row();
        Layout.Margin = 4;
        Layout.Spacing = 4;

        var serializedObject = Layer.GetSerialized();
        serializedObject.TryGetProperty(nameof(TilesetComponent.Layer.Name), out var name);
        labelText = new LabelTextEntry(name);
        Layout.Add(labelText);

        icoCollisionLayer = new Widget(this)
        {
            FixedSize = new Vector2(16, 16),
            OnPaintOverride = () =>
            {
                Paint.SetPen(Color.Transparent);
                Paint.SetBrush(Pixmap.FromTexture(Texture.Load(Editor.FileSystem.Mounted, "images/collision-bounce.png")));
                Paint.Scale(0.5f, 0.5f);
                Paint.DrawRect(LocalRect);
                Paint.Scale(1, 1);

                return true;
            },
            ToolTip = "Collision Layer",
            Visible = false
        };
        Layout.Add(icoCollisionLayer);

        var btnVisible = new IconButton(Layer.IsVisible ? "visibility" : "visibility_off");
        btnVisible.ToolTip = "Toggle Visibility";
        btnVisible.StatusTip = "Toggle Visibility of Layer " + Layer.Name;
        btnVisible.OnClick += () =>
        {
            Layer.IsVisible = !Layer.IsVisible;
            btnVisible.Icon = Layer.IsVisible ? "visibility" : "visibility_off";
        };
        Layout.Add(btnVisible);

        var btnLock = new IconButton(Layer.IsLocked ? "lock" : "lock_open");
        btnLock.ToolTip = "lock";
        btnLock.StatusTip = "Lock Layer " + Layer.Name;
        btnLock.OnClick += () =>
        {
            Layer.IsLocked = !Layer.IsLocked;
            btnLock.Icon = Layer.IsLocked ? "lock" : "lock_open";
        };
        Layout.Add(btnLock);

        IsDraggable = true;
        AcceptDrops = true;
    }

    protected override void OnPaint()
    {
        if (dragData?.IsValid ?? false)
        {
            Paint.SetBrushAndPen(Theme.Black.WithAlpha(0.5f));
            Paint.DrawRect(LocalRect, 4);
        }
        else if (TilesetTool.Active?.SelectedLayer == Layer)
        {
            Paint.SetBrushAndPen(Theme.Selection.Darken(0.5f));
            Paint.DrawRect(LocalRect, 4);
        }
        else if (IsUnderMouse)
        {
            Paint.SetBrushAndPen(Theme.White.WithAlpha(0.1f));
            Paint.DrawRect(LocalRect, 4);
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

    void SetAsCollisionLayer()
    {
        var list = ParentList.SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        foreach (var layer in list)
        {
            layer.IsCollisionLayer = false;
        }
        Layer.IsCollisionLayer = true;

        foreach (var layerControl in ParentList.LayerControls)
        {
            layerControl.icoCollisionLayer.Visible = layerControl.Layer.IsCollisionLayer;
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
        ParentList.DeleteLayer(Layer);
    }

    void DuplicateLayerPopup()
    {
        var popup = new PopupWidget(ParentList);
        popup.Layout = Layout.Column();
        popup.Layout.Margin = 16;
        popup.Layout.Spacing = 8;

        popup.Layout.Add(new Label($"What would you like to name the duplicated Layer?"));

        var entry = new LineEdit(popup);
        entry.Text = $"{Layer.Name} 2";
        var button = new Button.Primary("Duplicate");

        button.MouseClick = () =>
        {
            Duplicate(entry.Text);
            popup.Visible = false;
        };

        entry.ReturnPressed += button.MouseClick;

        popup.Layout.Add(entry);

        var bottomBar = popup.Layout.AddRow();
        bottomBar.AddStretchCell();
        bottomBar.Add(button);

        popup.Position = Editor.Application.CursorPosition;
        popup.Visible = true;

        entry.Focus();
    }

    void Duplicate(string name)
    {
        var list = ParentList.SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        var index = list.IndexOf(Layer);
        var layer = Layer.Copy();
        layer.Name = name;
        list.Insert(index + 1, layer);
        ParentList.SerializedProperty.SetValue(list);
        ParentList.UpdateList();
    }

    protected override void OnContextMenu(ContextMenuEvent e)
    {
        base.OnContextMenu(e);

        var m = new Menu(this);

        var list = ParentList.SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        var collisionLayer = list.FirstOrDefault(x => x.IsCollisionLayer);
        var firstLayer = list.FirstOrDefault();
        if (!Layer.IsCollisionLayer && (collisionLayer is not null || Layer != firstLayer))
        {
            m.AddOption("Set as Collision Layer", "touch_app", SetAsCollisionLayer);
            m.AddSeparator();
        }
        m.AddOption("Rename", "edit", Rename);
        m.AddOption("Duplicate", "content_copy", DuplicateLayerPopup);
        m.AddOption("Delete", "delete", Delete);

        m.OpenAtCursor(false);
    }

    protected override void OnDragStart()
    {
        base.OnDragStart();

        dragData = new Drag(this);
        dragData.Data.Object = Layer;
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

        var list = ParentList.SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        var index = list.IndexOf(Layer);
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
        var layer = ev.Data.OfType<TilesetComponent.Layer>().FirstOrDefault();

        if (layer == null || ParentList == null) return false;

        var layerList = ParentList.SerializedProperty.GetValue<List<TilesetComponent.Layer>>();
        var myIndex = layerList.IndexOf(Layer);
        var otherIndex = layerList.IndexOf(layer);
        if (myIndex == -1 || otherIndex == -1) return false;

        delta = otherIndex - myIndex;
        return true;
    }
}