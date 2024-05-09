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
    IconButton buttonPlay;
    IconButton buttonFramePrevious;
    IconButton buttonFrameNext;

    IntegerControlWidget widgetCurrentFrame;
    Label labelFrameCount;

    public Timeline(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Timeline";
        WindowTitle = "Timeline";
        SetWindowIcon("view_column");

        Layout = Layout.Column();

        MinimumWidth = 512f;
        MinimumHeight = 128f;

        var bannerLayout = Layout.Row();
        bannerLayout.Margin = 4;

        var label1 = new Label(this);
        label1.Text = "Frame:";
        bannerLayout.Add(label1);
        bannerLayout.AddSpacingCell(4);

        MainWindow.GetSerialized().TryGetProperty(nameof(MainWindow.CurrentFrame), out var currentFrameIndex);
        widgetCurrentFrame = new IntegerControlWidget(currentFrameIndex);
        widgetCurrentFrame.MaximumWidth = 64f;
        bannerLayout.Add(widgetCurrentFrame);
        bannerLayout.AddSpacingCell(4);

        labelFrameCount = new Label(this);
        labelFrameCount.Text = "/ 0";
        bannerLayout.Add(labelFrameCount);

        bannerLayout.AddStretchCell();

        buttonFramePrevious = new IconButton("navigate_before");
        buttonFramePrevious.OnClick = () =>
        {
            if (MainWindow.SelectedAnimation is null) return;
            var frame = MainWindow.CurrentFrameIndex;
            frame--;
            if (frame < 0) frame = MainWindow.SelectedAnimation.Frames.Count - 1;
            MainWindow.CurrentFrameIndex = frame;
        };
        bannerLayout.Add(buttonFramePrevious);
        bannerLayout.AddSpacingCell(4);

        buttonPlay = new IconButton("play_arrow");
        buttonPlay.OnClick = () =>
        {
            MainWindow.PlayPause();
        };
        bannerLayout.Add(buttonPlay);
        bannerLayout.AddSpacingCell(4);
        UpdatePlayButton();

        buttonFrameNext = new IconButton("navigate_next");
        buttonFrameNext.OnClick = () =>
        {
            if (MainWindow.SelectedAnimation is null) return;
            var frame = MainWindow.CurrentFrameIndex;
            frame++;
            if (frame >= MainWindow.SelectedAnimation.Frames.Count) frame = 0;
            MainWindow.CurrentFrameIndex = frame;
        };
        bannerLayout.Add(buttonFrameNext);
        bannerLayout.AddSpacingCell(4);

        bannerLayout.AddStretchCell();

        var text = bannerLayout.Add(new Label("Frame Size:"));
        text.HorizontalSizeMode = SizeMode.CanShrink;
        bannerLayout.AddSpacingCell(4);
        var slider = new FloatSlider(this);
        slider.HorizontalSizeMode = SizeMode.CanGrow;
        slider.Minimum = 16f;
        slider.Maximum = 128f;
        slider.Step = 1f;
        slider.Value = FrameButton.FrameSize;
        slider.MinimumWidth = 128f;
        slider.OnValueEdited = () =>
        {
            FrameButton.FrameSize = slider.Value;
            Update();
        };
        bannerLayout.Add(slider);

        Layout.Add(bannerLayout);

        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Row();
        scrollArea.Canvas.Layout.Spacing = 4;

        Layout.Add(scrollArea);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateFrameList();

        MainWindow.OnAnimationSelected += UpdateFrameList;
        MainWindow.OnPlayPause += UpdatePlayButton;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnAnimationSelected -= UpdateFrameList;
        MainWindow.OnPlayPause -= UpdatePlayButton;
    }

    [EditorEvent.Hotload]
    void Hotload()
    {
        UpdateFrameList();
        UpdatePlayButton();
    }

    void UpdatePlayButton()
    {
        buttonPlay.Icon = MainWindow.Playing ? "pause" : "play_arrow";
        buttonPlay.Update();
    }

    public void UpdateFrameList()
    {
        if (MainWindow?.SelectedAnimation is null) return;

        scrollArea.Canvas.Layout.Clear(true);
        scrollArea.Canvas.VerticalSizeMode = SizeMode.Flexible;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.Flexible;

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
        widgetCurrentFrame.Range = new Vector2(1, MainWindow.SelectedAnimation.Frames.Count);
        widgetCurrentFrame.RangeClamped = true;
        widgetCurrentFrame.HasRange = true;
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
            List<SpriteAnimationFrame> frames = new List<SpriteAnimationFrame>();
            foreach (var asset in x)
            {
                frames.Add(new SpriteAnimationFrame(asset.GetSourceFile()));
            }
            MainWindow.SelectedAnimation.Frames.AddRange(frames);
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

    [EditorEvent.Frame]
    void Frame()
    {
        if (MainWindow.SelectedAnimation is null)
        {
            labelFrameCount.Text = "/ 0";
            return;
        }

        labelFrameCount.Text = $"/ {MainWindow.SelectedAnimation.Frames.Count}";
    }

}