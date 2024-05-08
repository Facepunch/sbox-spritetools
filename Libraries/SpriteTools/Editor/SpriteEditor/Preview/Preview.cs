using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class Preview : Widget
{
    public MainWindow MainWindow { get; }
    private readonly RenderingWidget Rendering;

    public Preview(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Preview";
        WindowTitle = "Preview";
        SetWindowIcon("emoji_emotions");

        Layout = Layout.Column();

        Rendering = new RenderingWidget(MainWindow, this);
        Layout.Add(Rendering);

        UpdateTexture();
        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.OnTextureUpdate += UpdateTexture;
        MainWindow.OnAnimationSelected += UpdateWindowTitle;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnTextureUpdate -= UpdateTexture;
        MainWindow.OnAnimationSelected -= UpdateWindowTitle;
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
        Rendering.PreviewMaterial.Set("Color", texture);
        Rendering.TextureRect.SetMaterialOverride(Rendering.PreviewMaterial);
        // Rendering.TextureRect.Attributes.Set( "Color", texture );
    }
}