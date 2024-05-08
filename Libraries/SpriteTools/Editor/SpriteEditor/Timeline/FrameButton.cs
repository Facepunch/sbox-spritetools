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

        MinimumSize = new Vector2(FrameSize, FrameSize);
        MaximumSize = new Vector2(FrameSize, FrameSize);
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

        // m.AddOption( "Rename", "edit", Rename );
        // m.AddOption( "Duplicate", "content_copy", DuplicateAnimationPopup );
        m.AddOption("Delete", "delete", Delete);

        m.OpenAtCursor(false);
    }

    protected override void OnPaint()
    {
        MinimumSize = new Vector2(FrameSize, FrameSize);
        MaximumSize = new Vector2(FrameSize, FrameSize);

        Paint.SetBrushAndPen(Theme.ControlBackground);
        Paint.DrawRect(LocalRect);

        if (IsCurrentFrame)
        {
            Paint.SetBrushAndPen(Theme.Selection.WithAlpha(0.5f));
            Paint.DrawRect(new Rect(LocalRect.TopLeft, LocalRect.BottomRight.WithY(4f)));
        }

        if (dragData?.IsValid ?? false)
        {
            Paint.SetBrushAndPen(Theme.Black.WithAlpha(0.5f));
            Paint.DrawRect(LocalRect);
        }

        //Log.Info( MainWindow.SelectedAnimation.Frames[FrameIndex] );
        Texture texture = Texture.Load(Sandbox.FileSystem.Mounted, MainWindow.SelectedAnimation.Frames[FrameIndex]);

        Pixmap pix = new Pixmap(texture.Width, texture.Height);
        var pixels = texture.GetPixels();
        pix.UpdateFromPixels(MemoryMarshal.AsBytes<Color32>(pixels), texture.Width, texture.Height, ImageFormat.RGBA8888);
        Paint.Draw(LocalRect.Shrink(2), pix);

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

        var oldList = new List<string>(MainWindow.SelectedAnimation.Frames);
        MainWindow.SelectedAnimation.Frames = new List<string>();

        var index = FrameIndex;
        var newIndex = index + delta;

        for (int i = 0; i < oldList.Count; i++)
        {
            if (i == index) continue;

            if (i == newIndex)
            {
                MainWindow.SelectedAnimation.Frames.Add(oldList[FrameIndex]);
            }

            MainWindow.SelectedAnimation.Frames.Add(oldList[i]);
        }

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

    void Delete()
    {
        MainWindow.SelectedAnimation.Frames.RemoveAt(FrameIndex);
        Timeline.UpdateFrameList();
    }
}