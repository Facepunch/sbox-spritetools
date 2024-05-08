using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Timeline;

public class Timeline : Widget
{
    public SpriteResource Sprite { get; set; }
    public MainWindow MainWindow { get; }

    ScrollArea scrollArea;

    public Timeline(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Frames";
        WindowTitle = "Frames";
        SetWindowIcon("view_column");

        Layout = Layout.Column();
        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Row();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.Flexible;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.CanGrow;

        Layout.Add(scrollArea);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateFrameList();

        MainWindow.OnAnimationSelected += UpdateFrameList;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnAnimationSelected -= UpdateFrameList;
    }

    [EditorEvent.Hotload]
    public void UpdateFrameList()
    {
        if (MainWindow?.SelectedAnimation is null) return;

        scrollArea.Canvas.Layout.Clear(true);

        if (MainWindow.SelectedAnimation.Frames is not null)
        {
            int index = 0;
            foreach (var frame in MainWindow.SelectedAnimation.Frames)
            {
                var frameButton = new FrameButton(this, MainWindow, index);
                scrollArea.Canvas.Layout.Add(frameButton);
                index++;
            }
        }

        var addButton = new IconButton("add");
        addButton.Width = 128;
        addButton.Height = 128;
        addButton.Size = new Vector2(128, 128);
        addButton.OnClick = () => LoadImage();

        scrollArea.Canvas.Layout.Add(addButton);
    }

    void LoadImage()
    {
        if (MainWindow.SelectedAnimation is null) return;

        var picker = new AssetPicker(this, AssetType.ImageFile);
        picker.Window.StateCookie = "SpriteEditor.Import";
        picker.Window.RestoreFromStateCookie();
        picker.Window.Title = $"Import Frame - {MainWindow.Sprite.ResourceName} - {MainWindow.SelectedAnimation.Name}";
        picker.MultiPick = true;
        // picker.Assets = new List<Asset>() { Asset };
        // picker.OnAssetHighlighted = x => Asset = x.First();
        picker.OnAssetPicked = x =>
        {
            List<string> paths = new List<string>();
            foreach (var asset in x)
            {
                paths.Add(asset.GetSourceFile());
            }
            MainWindow.SelectedAnimation.Frames.AddRange(paths);
            UpdateFrameList();
        };
        picker.Window.Show();
    }

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);

        if (e.Key == KeyCode.Delete || e.Key == KeyCode.Backspace)
        {
            if (MainWindow.SelectedAnimation is null) return;

            MainWindow.SelectedAnimation.Frames.RemoveAt(MainWindow.CurrentFrameIndex);
            UpdateFrameList();
        }
    }

}