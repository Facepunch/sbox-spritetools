using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools.SpriteEditor.Preview;

public class Preview : Widget
{
    public MainWindow MainWindow { get; }
    private readonly RenderingWidget Rendering;

    Widget Overlay;
    WidgetWindow overlayWindowZoom;
    WidgetWindow overlayWindowPoint;

    Vector2 attachmentCreatePosition;

    public Preview(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Preview";
        WindowTitle = "Preview";
        SetWindowIcon("emoji_emotions");

        MinimumSize = new Vector2(256, 256);

        Layout = Layout.Column();

        Rendering = new RenderingWidget(MainWindow, this);
        Layout.Add(Rendering);

        Overlay = new Widget(this)
        {
            Layout = Layout.Row(),
            TranslucentBackground = true,
            NoSystemBackground = true,
            WindowFlags = WindowFlags.FramelessWindowHint | WindowFlags.Tool
        };
        overlayWindowZoom = new WidgetWindow(this);
        overlayWindowZoom.Parent = Overlay;
        overlayWindowZoom.Layout = Layout.Row();
        overlayWindowZoom.Layout.Spacing = 4;
        overlayWindowZoom.Layout.Margin = 4;
        var btnZoomOut = overlayWindowZoom.Layout.Add(new IconButton("zoom_out"));
        btnZoomOut.OnClick = () =>
        {
            Rendering.Zoom(-250);
        };
        btnZoomOut.ToolTip = "Zoom Out";
        btnZoomOut.StatusTip = "Zoom Out View";
        var btnZoomIn = overlayWindowZoom.Layout.Add(new IconButton("zoom_in"));
        btnZoomIn.OnClick = () =>
        {
            Rendering.Zoom(250);
        };
        btnZoomIn.ToolTip = "Zoom In";
        btnZoomIn.StatusTip = "Zoom In View";
        overlayWindowZoom.Layout.AddSeparator();
        var btnFit = overlayWindowZoom.Layout.Add(new IconButton("zoom_out_map"));
        btnFit.OnClick = () =>
        {
            Rendering.Fit();
        };
        btnFit.ToolTip = "Fit to Screen";
        btnFit.StatusTip = "Fit View to Screen";
        overlayWindowZoom.WindowTitle = "Zoom Controls";

        Overlay.Layout.Add(overlayWindowZoom);

        overlayWindowPoint = new WidgetWindow(this);
        overlayWindowPoint.Parent = Overlay;
        overlayWindowPoint.Layout = Layout.Column();
        overlayWindowPoint.Layout.Margin = 4;
        overlayWindowPoint.WindowTitle = "Point Controls";

        var row1 = overlayWindowPoint.Layout.AddRow();
        var btnTopLeft = row1.Add(new TextureModifyButton(this, "Align Top-Left", "Images/grid-align-top-left.png", () => SetOrigin(new Vector2(0, 0f))));
        var btnTopMiddle = row1.Add(new TextureModifyButton(this, "Align Top-Center", "Images/grid-align-top-center.png", () => SetOrigin(new Vector2(0.5f, 0f))));
        var btnTopRight = row1.Add(new TextureModifyButton(this, "Align Top-Right", "Images/grid-align-top-right.png", () => SetOrigin(new Vector2(1f, 0f))));

        var row2 = overlayWindowPoint.Layout.AddRow();
        var btnMiddleLeft = row2.Add(new TextureModifyButton(this, "Align Middle-Left", "Images/grid-align-middle-left.png", () => SetOrigin(new Vector2(0f, 0.5f))));
        var btnMiddleCenter = row2.Add(new TextureModifyButton(this, "Align Middle-Center", "Images/grid-align-middle-center.png", () => SetOrigin(new Vector2(0.5f, 0.5f))));
        var btnMiddleRight = row2.Add(new TextureModifyButton(this, "Align Middle-Right", "Images/grid-align-middle-right.png", () => SetOrigin(new Vector2(1f, 0.5f))));

        var row3 = overlayWindowPoint.Layout.AddRow();
        var btnBottomLeft = row3.Add(new TextureModifyButton(this, "Align Bottom-Left", "Images/grid-align-bottom-left.png", () => SetOrigin(new Vector2(0, 1f))));
        var btnBottomCenter = row3.Add(new TextureModifyButton(this, "Align Bottom-Center", "Images/grid-align-bottom-center.png", () => SetOrigin(new Vector2(0.5f, 1f))));
        var btnBottomRight = row3.Add(new TextureModifyButton(this, "Align Bottom-Right", "Images/grid-align-bottom-right.png", () => SetOrigin(new Vector2(1f, 1f))));
        Overlay.Layout.Add(overlayWindowPoint);

        Overlay.Show();
        UpdateTexture();
        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.OnTextureUpdate += UpdateTexture;

        MainWindow.Moved += DoLayout;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= UpdateTexture;
        MainWindow.Moved -= DoLayout;
    }

    void SetOrigin(Vector2 origin)
    {
        if (MainWindow.SelectedAnimation is null) return;
        MainWindow.SelectedAnimation.Origin = origin;
    }

    void UpdateTexture()
    {
        if (MainWindow.Sprite is null) return;
        if (MainWindow.SelectedAnimation is null) return;
        if (MainWindow.SelectedAnimation.Frames.Count <= 0) return;
        var frame = MainWindow.SelectedAnimation.Frames[MainWindow.CurrentFrameIndex];
        if (frame is null) return;
        var texture = Texture.Load(Sandbox.FileSystem.Mounted, frame.FilePath);
        if (texture is null) return;
        Rendering.SetTexture(texture, frame.SpriteSheetRect);
    }

    protected override void DoLayout()
    {
        base.DoLayout();

        if (Overlay.IsValid() && Rendering.IsValid())
        {
            Overlay.Position = Rendering.ScreenPosition;
            Overlay.Size = Rendering.Size + 1;

            if (overlayWindowPoint.Visible)
            {
                overlayWindowPoint.AdjustSize();
                overlayWindowPoint.AlignToParent(TextFlag.LeftTop, 4);
            }

            overlayWindowZoom.AdjustSize();
            overlayWindowZoom.AlignToParent(TextFlag.RightTop, 4);
        }
    }

    protected override void OnVisibilityChanged(bool visible)
    {
        base.OnVisibilityChanged(visible);

        if (Overlay is not null)
        {
            Overlay.Visible = visible;
        }
    }

    protected override void OnKeyRelease(KeyEvent e)
    {
        base.OnKeyRelease(e);

        if (e.Key == KeyCode.Left)
        {
            MainWindow.FramePrevious();
        }
        else if (e.Key == KeyCode.Right)
        {
            MainWindow.FrameNext();
        }
    }

    void CreateAttachmentPopup()
    {
        var popup = new PopupWidget(MainWindow);
        popup.Layout = Layout.Column();
        popup.Layout.Margin = 16;
        popup.Layout.Spacing = 8;

        popup.Layout.Add(new Label($"What would you like to name the attachment point?"));

        var entry = new LineEdit(popup);
        var button = new Button.Primary("Create");

        button.MouseClick = () =>
        {
            if (!string.IsNullOrEmpty(entry.Text) && !MainWindow.SelectedAnimation.Attachments.Any(a => a.Name.ToLowerInvariant() == entry.Text.ToLowerInvariant()))
            {
                CreateAttachment(entry.Text);
            }
            else
            {
                ShowNamingError(entry.Text);
            }
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

    void CreateAttachment(string name)
    {
        MainWindow.PushUndo("Add Attachment Point " + name);
        var tr = Rendering.World.Trace.Ray(Rendering.Camera.GetRay(attachmentCreatePosition), 5000f).Run();
        var pos = tr.EndPosition.WithZ(0f);
        var attachPos = new Vector2(pos.y, pos.x);
        attachPos = (attachPos / 100f) + (Vector2.One * 0.5f);
        var attachment = new SpriteAttachment(name);
        attachment.Points.Add(attachPos);
        MainWindow.SelectedAnimation.Attachments.Add(attachment);
        // MainWindow.SelectedAnimation.Frames[MainWindow.CurrentFrameIndex].AttachmentPoints[name] = attachPos;
        MainWindow.PushRedo();
    }

    static void ShowNamingError(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            var confirm = new PopupWindow("Invalid name ''", "You cannot give an attachment point an empty name", "OK");
            confirm.Show();

            return;
        }

        var confirm2 = new PopupWindow("Invalid name", $"An attachment point named '{name}' already exists", "OK");
        confirm2.Show();
    }

    protected override void OnContextMenu(ContextMenuEvent e)
    {
        base.OnContextMenu(e);

        attachmentCreatePosition = e.LocalPosition;

        var m = new Menu(this);

        m.AddOption("Add Attach Point", "push_pin", CreateAttachmentPopup);

        m.OpenAtCursor(false);
    }


    private class TextureModifyButton : Widget
    {
        private readonly string Icon;
        private Pixmap pixmap;

        public TextureModifyButton(Widget parent, string tooltip, string icon, Action onClick) : base(parent)
        {
            Icon = icon;
            ToolTip = tooltip;
            FixedSize = 26;
            Cursor = CursorShape.Finger;
            MouseClick = onClick;
            pixmap = Pixmap.FromFile(Editor.FileSystem.Content.GetFullPath(Icon));
        }

        protected override void OnPaint()
        {
            base.OnPaint();

            if (Paint.HasMouseOver)
            {
                Paint.ClearPen();
                var bg = Theme.ControlBackground.Lighten(0.3f);
                Paint.SetBrush(bg);
                Paint.DrawRect(LocalRect, Theme.ControlRadius);
            }
            Paint.Draw(LocalRect.Shrink(5), pixmap);
        }
    }
}