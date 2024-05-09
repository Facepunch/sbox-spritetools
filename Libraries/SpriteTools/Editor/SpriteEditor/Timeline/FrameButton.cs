using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpriteTools.SpriteEditor.Timeline;

internal class FrameButton : Widget
{
    MainWindow MainWindow;
    Timeline Timeline;
    public int FrameIndex;

    public bool IsCurrentFrame => MainWindow.CurrentFrameIndex == FrameIndex;

    Drag dragData;
    bool draggingAbove = false;
    bool draggingBelow = false;

    public static float FrameSize = 64f;

    public FrameButton(Timeline timeline, MainWindow window, int index) : base(null)
    {
        Timeline = timeline;
        MainWindow = window;
        FrameIndex = index;

        Layout = Layout.Row();
        Layout.Margin = 4;

        MinimumSize = new Vector2(FrameSize, FrameSize + 16f);
        MaximumSize = new Vector2(FrameSize, FrameSize + 16f);
        HorizontalSizeMode = SizeMode.Ignore;
        VerticalSizeMode = SizeMode.Ignore;

        // var serializedObject = Animation.GetSerialized();
        // serializedObject.TryGetProperty( nameof( SpriteAnimation.Name ), out var name );
        // labelText = new LabelTextEntry( MainWindow, name );

        // Layout.Add( labelText );

        IsDraggable = true;
        AcceptDrops = true;
        MainWindow.OnTextureUpdate += Update;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= Update;
    }

    protected override void OnContextMenu(ContextMenuEvent e)
    {
        base.OnContextMenu(e);

        var m = new Menu(this);

        m.AddOption("Add Broadcast Message", "wifi_tethering", AddEventPopup);
        var optionClear = m.AddOption("Clear Broadcast Messages", "portable_wifi_off", ClearBroadcastEvents);
        optionClear.Enabled = MainWindow.SelectedAnimation.Frames[FrameIndex].Events.Count > 0;
        m.AddOption("Duplicate", "content_copy", Duplicate);
        m.AddOption("Delete", "delete", Delete);

        m.OpenAtCursor(false);
    }

    protected override void OnPaint()
    {
        MinimumSize = new Vector2(FrameSize, FrameSize + 16f);
        MaximumSize = new Vector2(FrameSize, FrameSize + 16f);

        Paint.SetBrushAndPen(Theme.ControlBackground.Lighten(0.5f));
        Paint.DrawRect(LocalRect);

        Paint.SetBrushAndPen(Theme.ControlBackground);
        Paint.DrawRect(new Rect(LocalRect.TopLeft.WithY(16f), LocalRect.BottomRight + Vector2.Down * 16f).Shrink(4));
        if (IsCurrentFrame)
        {
            Paint.SetBrushAndPen(Theme.Selection.WithAlpha(0.5f));
            Paint.DrawRect(new Rect(LocalRect.TopLeft, LocalRect.BottomRight.WithY(16f)));
        }

        Paint.SetPen(Theme.White);
        var rect = new Rect(LocalRect.TopLeft, LocalRect.BottomRight.WithY(16f));
        Paint.DrawText(rect, (FrameIndex + 1).ToString(), TextFlag.Center);

        if (dragData?.IsValid ?? false)
        {
            Paint.SetBrushAndPen(Theme.Black.WithAlpha(0.5f));
            Paint.DrawRect(LocalRect);
        }

        //Log.Info( MainWindow.SelectedAnimation.Frames[FrameIndex] );
        Texture texture = Texture.Load(Sandbox.FileSystem.Mounted, MainWindow.SelectedAnimation.Frames[FrameIndex].FilePath);

        Pixmap pix = new Pixmap(texture.Width, texture.Height);
        var pixels = texture.GetPixels();
        pix.UpdateFromPixels(MemoryMarshal.AsBytes<Color32>(pixels), texture.Width, texture.Height, ImageFormat.RGBA8888);
        Paint.Draw(new Rect(LocalRect.TopLeft + Vector2.Up * 16f, LocalRect.BottomRight - Vector2.Up * 16f).Shrink(4), pix);

        if (MainWindow.SelectedAnimation.Frames[FrameIndex].Events.Count > 0)
        {
            var tagRect = new Rect(LocalRect.BottomLeft + Vector2.Down * 20f, new Vector2(Width, 20f)).Shrink(4);
            Paint.SetBrushAndPen(Theme.Yellow.WithAlpha(0.5f));
            Paint.DrawRect(tagRect);

            string events = string.Join(", ", MainWindow.SelectedAnimation.Frames[FrameIndex].Events);

            Paint.SetFont("Poppins", 7, 1000, false);
            Paint.SetPen(Theme.Black);
            tagRect.Position += 1;
            Paint.DrawText(tagRect, events, TextFlag.Center);
            tagRect.Position -= 1;
            Paint.SetPen(Theme.White);
            Paint.DrawText(tagRect, events, TextFlag.Center);
        }

        base.OnPaint();

        if (draggingAbove)
        {
            Paint.SetPen(Theme.Selection, 2f, PenStyle.Dot);
            Paint.DrawLine(LocalRect.TopLeft, LocalRect.BottomLeft);
            draggingAbove = false;
        }
        else if (draggingBelow)
        {
            Paint.SetPen(Theme.Selection, 2f, PenStyle.Dot);
            Paint.DrawLine(LocalRect.TopRight, LocalRect.BottomRight);
            draggingBelow = false;
        }
    }


    protected override void OnDragStart()
    {
        base.OnDragStart();

        if (MainWindow.Playing) return;

        dragData = new Drag(this);
        dragData.Data.Object = this;
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

        MainWindow.PushUndo($"Re-Order {MainWindow.SelectedAnimation.Name} Frames");

        var index = FrameIndex;
        var movingIndex = index + delta;
        var frame = MainWindow.SelectedAnimation.Frames[movingIndex];

        MainWindow.SelectedAnimation.Frames.RemoveAt(movingIndex);
        MainWindow.SelectedAnimation.Frames.Insert(index, frame);

        MainWindow.PushRedo();

        Timeline.UpdateFrameList();
    }

    bool TryDragOperation(DragEvent ev, out int delta)
    {
        delta = 0;
        var draggingButton = ev.Data.OfType<FrameButton>().FirstOrDefault();
        var otherIndex = draggingButton?.FrameIndex ?? -1;

        if (otherIndex < 0 || MainWindow.SelectedAnimation == null || FrameIndex == otherIndex)
        {
            return false;
        }

        if (FrameIndex == -1 || otherIndex == -1)
        {
            return false;
        }

        delta = otherIndex - FrameIndex;
        return true;
    }

    protected override void OnMouseClick(MouseEvent e)
    {
        base.OnMouseClick(e);

        MainWindow.CurrentFrameIndex = FrameIndex;
    }

    void AddEventPopup()
    {
        var popup = new PopupWidget(MainWindow);
        popup.Layout = Layout.Column();
        popup.Layout.Margin = 16;
        popup.Layout.Spacing = 8;

        popup.Layout.Add(new Label($"What would you like to name the event?"));

        var entry = new LineEdit(popup);
        var button = new Button.Primary("Create");

        button.MouseClick = () =>
        {
            AddBroadcastEvent(entry.Text, FrameIndex);
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

    void AddBroadcastEvent(string name, int frame)
    {
        if (!MainWindow.SelectedAnimation.Frames[FrameIndex].Events.Contains(name))
        {
            MainWindow.PushUndo($"Add Broadcast Event {name}");
            MainWindow.SelectedAnimation.Frames[FrameIndex].Events.Add(name);
            MainWindow.PushRedo();

            MainWindow.OnAnimationChanges?.Invoke();
        }
    }

    void ClearBroadcastEvents()
    {
        MainWindow.PushUndo($"Clear Broadcast Events");
        MainWindow.SelectedAnimation.Frames[FrameIndex].Events.Clear();
        MainWindow.PushRedo();
        MainWindow.OnAnimationChanges?.Invoke();
    }

    void Duplicate()
    {
        MainWindow.PushUndo($"Duplicate {MainWindow.SelectedAnimation.Name} Frame");
        MainWindow.SelectedAnimation.Frames.Insert(FrameIndex, MainWindow.SelectedAnimation.Frames[FrameIndex]);
        Timeline.UpdateFrameList();
        MainWindow.PushRedo();
    }

    void Delete()
    {
        MainWindow.PushUndo($"Delete {MainWindow.SelectedAnimation.Name} Frame");
        MainWindow.SelectedAnimation.Frames.RemoveAt(FrameIndex);
        Timeline.UpdateFrameList();
        MainWindow.PushRedo();
    }
}