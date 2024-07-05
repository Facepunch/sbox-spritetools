using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.SpriteEditor.AnimationList;

public class AnimationList : Widget
{
    public SpriteResource Sprite { get; set; }
    public MainWindow MainWindow { get; }
    List<AnimationButton> Animations = new();

    ScrollArea scrollArea;
    Layout content;

    public AnimationList(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Animations";
        WindowTitle = "Animations";
        SetWindowIcon("directions_walk");


        Layout = Layout.Column();
        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.CanGrow;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.Flexible;

        // Add content list
        content = Layout.Column();
        content.Margin = 4;
        content.AddStretchCell();
        scrollArea.Canvas.Layout.Add(content);

        // Add component button
        var row = scrollArea.Canvas.Layout.AddRow();
        row.AddStretchCell();
        row.Margin = 16;
        var button = row.Add(new Button.Primary("Create New Animation", "add"));
        button.MinimumWidth = 300;
        button.Clicked = CreateAnimationPopup;
        row.AddStretchCell();

        scrollArea.Canvas.Layout.AddStretchCell();

        Layout.Add(scrollArea);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateAnimationList();
        MainWindow.OnAssetLoaded += UpdateAnimationList;
        MainWindow.OnAnimationChanges += UpdateAnimationList;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnAssetLoaded -= UpdateAnimationList;
        MainWindow.OnAnimationChanges -= UpdateAnimationList;
    }

    void CreateAnimationPopup()
    {
        var popup = new PopupWidget(MainWindow);
        popup.Layout = Layout.Column();
        popup.Layout.Margin = 16;
        popup.Layout.Spacing = 8;

        popup.Layout.Add(new Label($"What would you like to name the animation?"));

        var entry = new LineEdit(popup);
        var button = new Button.Primary("Create");

        button.MouseClick = () =>
        {
            if (!string.IsNullOrEmpty(entry.Text) && !MainWindow.Sprite.Animations.Any(a => a.Name.ToLowerInvariant() == entry.Text.ToLowerInvariant()))
            {
                CreateAnimation(entry.Text);
                UpdateAnimationList();
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

    [EditorEvent.Hotload]
    public void UpdateAnimationList()
    {
        content.Clear(true);
        Animations.Clear();

        var animations = MainWindow.Sprite.Animations;

        foreach (var animation in animations)
        {
            var button = content.Add(new AnimationButton(this, MainWindow, animation));
            button.MouseClick = () => SelectAnimation(button);
            Animations.Add(button);
        }
    }

    void CreateAnimation(string name)
    {
        var anim = new SpriteAnimation(name);
        anim.Looping = true;

        MainWindow.PushUndo($"Create Animation {name}");
        MainWindow.Sprite.Animations.Add(anim);
        MainWindow.PushRedo();
    }

    void SelectAnimation(AnimationButton button)
    {
        MainWindow.SelectedAnimation = button.Animation;
        MainWindow.OnAnimationSelected?.Invoke();
    }

    public static void ShowNamingError(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            var confirm = new PopupWindow("Invalid name ''", "You cannot give an animation an empty name", "OK");
            confirm.Show();
        }
        else
        {
            var confirm = new PopupWindow($"Invalid name '{name}'", "You cannot give two animations the same name", "OK");
            confirm.Show();
        }
    }

}