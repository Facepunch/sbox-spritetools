using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools.SpriteEditor.Animator;

public class Animator : Widget
{
    public MainWindow MainWindow { get; }

    public Animator(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Animator";
        WindowTitle = "Animator";
        SetWindowIcon("animation");

        MinimumSize = new Vector2(256, 256);

        Layout = Layout.Column();

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();
    }
}