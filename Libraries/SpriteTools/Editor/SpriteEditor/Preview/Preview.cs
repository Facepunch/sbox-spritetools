using Editor;
using Sandbox;
using System.Linq;

namespace SpriteTools.SpriteEditor.Preview;

public class Preview : Widget
{
    public MainWindow MainWindow { get; }
    private readonly RenderingWidget Rendering;

    Widget Overlay;
    WidgetWindow overlayWindow;

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
        overlayWindow = new WidgetWindow(this);
        overlayWindow.Parent = Overlay;
        overlayWindow.Layout = Layout.Row();
        overlayWindow.Layout.Spacing = 4;
        overlayWindow.Layout.Margin = 4;
        var btnZoomOut = overlayWindow.Layout.Add(new IconButton("zoom_out"));
        btnZoomOut.OnClick = () =>
        {
            Rendering.Zoom(-250);
        };
        btnZoomOut.ToolTip = "Zoom Out";
        btnZoomOut.StatusTip = "Zoom Out View";
        var btnZoomIn = overlayWindow.Layout.Add(new IconButton("zoom_in"));
        btnZoomIn.OnClick = () =>
        {
            Rendering.Zoom(250);
        };
        btnZoomIn.ToolTip = "Zoom In";
        btnZoomIn.StatusTip = "Zoom In View";
        overlayWindow.Layout.AddSeparator();
        var btnFit = overlayWindow.Layout.Add(new IconButton("zoom_out_map"));
        btnFit.OnClick = () =>
        {
            Rendering.Fit();
        };
        btnFit.ToolTip = "Fit to Screen";
        btnFit.StatusTip = "Fit View to Screen";
        overlayWindow.WindowTitle = "Zoom Controls";

        Overlay.Layout.Add(overlayWindow);
        Overlay.Show();

        UpdateTexture();
        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.OnTextureUpdate += UpdateTexture;
        MainWindow.OnAnimationSelected += UpdateWindowTitle;

        MainWindow.Moved += DoLayout;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= UpdateTexture;
        MainWindow.OnAnimationSelected -= UpdateWindowTitle;
        MainWindow.Moved -= DoLayout;
    }

    void UpdateWindowTitle()
    {
        if (MainWindow.SelectedAnimation is null)
        {
            WindowTitle = "Preview";
            return;
        }
        WindowTitle = $"Preview - {MainWindow.SelectedAnimation.Name}";
    }

    void UpdateTexture()
    {
        if (MainWindow.Sprite is null) return;
        if (string.IsNullOrEmpty(MainWindow.CurrentTexturePath)) return;

        var texture = Texture.Load(Sandbox.FileSystem.Mounted, MainWindow.CurrentTexturePath);
        Rendering.PreviewMaterial.Set("Texture", texture);
        Rendering.TextureSize = new Vector2(texture.Width, texture.Height);
        Rendering.TextureRect.SetMaterialOverride(Rendering.PreviewMaterial);
    }

    protected override void DoLayout()
    {
        base.DoLayout();

        if (Overlay.IsValid() && Rendering.IsValid())
        {
            Overlay.Position = Rendering.ScreenPosition;
            Overlay.Size = Rendering.Size + 1;

            overlayWindow.AdjustSize();
            overlayWindow.AlignToParent(TextFlag.RightTop, 4);
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
        MainWindow.SelectedAnimation.Attachments.Add(new SpriteAttachment(name));
        MainWindow.SelectedAnimation.Frames[MainWindow.CurrentFrameIndex].AttachmentPoints[name] = attachPos;
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
}