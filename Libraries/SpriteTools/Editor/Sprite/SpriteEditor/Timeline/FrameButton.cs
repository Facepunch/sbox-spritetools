using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SpriteTools.SpriteEditor.Timeline;

public class FrameButton : Widget
{
    MainWindow MainWindow;
    Timeline Timeline;
    public int FrameIndex;
    Pixmap Pixmap;

    public bool IsCurrentFrame => MainWindow.CurrentFrameIndex == FrameIndex;

    Drag dragData;
    bool draggingAbove = false;
    bool draggingBelow = false;

    public static float FrameSize = 64f;
    public static List<FrameButton> Selected = new();
    static int lastSelectedIndex = 0;

    public FrameButton(Timeline timeline, MainWindow window, int index) : base(null)
    {
        Timeline = timeline;
        MainWindow = window;
        FrameIndex = index;
        Cursor = CursorShape.Finger;

        Layout = Layout.Row();
        Layout.Margin = 4;

        MinimumSize = new Vector2(FrameSize, FrameSize + 16f);
        MaximumSize = new Vector2(FrameSize, FrameSize + 16f);
        HorizontalSizeMode = SizeMode.Ignore;
        VerticalSizeMode = SizeMode.Ignore;

        // Get the texture for the frame
        var frame = MainWindow.SelectedAnimation.Frames[FrameIndex];
        var rect = frame.SpriteSheetRect;
        Pixmap = PixmapCache.Get(frame.FilePath, rect);

        StatusTip = $"Frame {FrameIndex + 1} - {frame.FilePath}";
        if (frame.SpriteSheetRect.Width != 0 && frame.SpriteSheetRect.Height != 0)
        {
            StatusTip += " - (" + frame.SpriteSheetRect + ")";
        }

        IsDraggable = true;
        AcceptDrops = true;
        MainWindow.OnTextureUpdate += Update;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= Update;
        Selected.Remove(this);
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
        bool isSelected = Selected.Contains(this);

        MinimumSize = new Vector2(FrameSize, FrameSize + 16f);
        MaximumSize = new Vector2(FrameSize, FrameSize + 16f);

        Paint.SetBrushAndPen(Theme.ControlBackground.Lighten(isSelected ? 2f : (IsUnderMouse ? 1f : 0.5f)));
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

        var pixRect = new Rect(LocalRect.TopLeft + Vector2.Up * 16f, LocalRect.BottomRight - Vector2.Up * 16f).Shrink(4);
        var aspectRatio = Pixmap.Width / (float)Pixmap.Height;
        if (aspectRatio > 1f)
        {
            pixRect.Height = pixRect.Height / aspectRatio;
            pixRect.Top -= (pixRect.Height - pixRect.Width) / 2f;
            pixRect.Bottom += (pixRect.Height - pixRect.Width) / 2f;
        }
        else
        {
            pixRect.Width = pixRect.Width / ((float)Pixmap.Height / Pixmap.Width);
            pixRect.Left -= (pixRect.Width - pixRect.Height) / 2f;
            pixRect.Right -= (pixRect.Width - pixRect.Height) / 2f;
        }
        Paint.Draw(pixRect, Pixmap);

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

        Move(delta);

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

        if (!MainWindow.Playing)
            MainWindow.CurrentFrameIndex = FrameIndex;

        bool has = Selected.Contains(this);
        bool shifting = e.HasShift && Selected.Count > 0;

        if (!e.HasCtrl && !e.HasShift)
        {
            Selected.Clear();
        }
        else if (shifting)
        {
            Selected.Clear();
            int start = Math.Min(lastSelectedIndex, FrameIndex);
            int end = Math.Min(Math.Max(lastSelectedIndex, FrameIndex), Timeline.Buttons.Count - 1);

            for (int i = start; i <= end; i++)
            {
                Selected.Add(Timeline.Buttons[i]);
            }
        }

        if (!shifting)
        {
            if (has)
                Selected.Remove(this);
            else
                Selected.Add(this);

            lastSelectedIndex = FrameIndex;
        }
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

    void Move(int delta)
    {
        MainWindow.PushUndo($"Re-Order {MainWindow.SelectedAnimation.Name} Frames");

        var index = FrameIndex;
        var movingIndex = index + delta;
        var frame = MainWindow.SelectedAnimation.Frames[movingIndex];

        MainWindow.SelectedAnimation.Frames.RemoveAt(movingIndex);
        MainWindow.SelectedAnimation.Frames.Insert(index, frame);

        foreach (var attachment in MainWindow.SelectedAnimation.Attachments)
        {
            if (attachment.Points.Count == 0) continue;

            var maxIndex = Math.Max(index, movingIndex);
            if (maxIndex >= attachment.Points.Count)
            {
                for (int i = attachment.Points.Count; i <= maxIndex; i++)
                {
                    attachment.Points.Add(attachment.Points.Last());
                }
            }

            var point = attachment.Points[movingIndex];
            attachment.Points.RemoveAt(movingIndex);
            attachment.Points.Insert(index, point);
        }

        MainWindow.PushRedo();
    }

    void Duplicate()
    {
        MainWindow.PushUndo($"Duplicate {MainWindow.SelectedAnimation.Name} Frame");

        MainWindow.SelectedAnimation.Frames.Insert(FrameIndex, MainWindow.SelectedAnimation.Frames[FrameIndex].Copy());

        foreach (var attachment in MainWindow.SelectedAnimation.Attachments)
        {
            if (attachment.Points.Count == 0) continue;

            if (FrameIndex < attachment.Points.Count)
            {
                attachment.Points.Insert(FrameIndex, attachment.Points[FrameIndex]);
            }
        }

        Timeline.UpdateFrameList();

        MainWindow.PushRedo();
    }

    void Delete()
    {
        if (Selected.Count > 1)
        {
            DeleteSelected();
            return;
        }

        MainWindow.PushUndo($"Delete {MainWindow.SelectedAnimation.Name} Frame");

        MainWindow.SelectedAnimation.Frames.RemoveAt(FrameIndex);

        foreach (var attachment in MainWindow.SelectedAnimation.Attachments)
        {
            if (attachment.Points.Count == 0) continue;

            if (FrameIndex < attachment.Points.Count)
            {
                attachment.Points.RemoveAt(FrameIndex);
            }
        }

        Timeline.UpdateFrameList();

        MainWindow.PushRedo();
    }

    void DeleteSelected()
    {
        MainWindow.PushUndo($"Delete {Selected.Count} Frames from {MainWindow.SelectedAnimation.Name}");

        Selected = Selected.OrderBy(x => x.FrameIndex).ToList();
        for (int i = Selected.Count - 1; i >= 0; i--)
        {
            var button = Selected[i];
            MainWindow.SelectedAnimation.Frames.RemoveAt(button.FrameIndex);

            foreach (var attachment in MainWindow.SelectedAnimation.Attachments)
            {
                if (attachment.Points.Count == 0) continue;

                if (button.FrameIndex < attachment.Points.Count)
                {
                    attachment.Points.RemoveAt(button.FrameIndex);
                }
            }
        }

        Timeline.UpdateFrameList();

        MainWindow.PushRedo();
    }
}