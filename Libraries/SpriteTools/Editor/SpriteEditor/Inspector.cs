using Editor;
using Sandbox;
using System.Linq;

namespace SpriteTools.SpriteEditor;

public class Inspector : Widget
{
    public SpriteResource Sprite { get; set; }
    public MainWindow MainWindow { get; }

    ControlSheet controlSheet;

    public Inspector(MainWindow mainWindow) : base(null)
    {
        MainWindow = mainWindow;

        Name = "Inspector";
        WindowTitle = "Inspector";
        SetWindowIcon("manage_search");

        Layout = Layout.Column();
        controlSheet = new ControlSheet();

        MinimumWidth = 450f;

        var scroller = new ScrollArea(this);
        scroller.Canvas = new Widget();
        scroller.Canvas.Layout = Layout.Column();
        scroller.Canvas.VerticalSizeMode = SizeMode.CanGrow;
        scroller.Canvas.HorizontalSizeMode = SizeMode.Flexible;

        scroller.Canvas.Layout.Add(controlSheet);
        scroller.Canvas.Layout.AddStretchCell();
        Layout.Add(scroller);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        MainWindow.OnAssetLoaded += UpdateControlSheet;
        MainWindow.OnAnimationSelected += UpdateControlSheet;
    }

    public override void OnDestroyed()
    {
        base.OnDestroyed();

        MainWindow.OnAssetLoaded -= UpdateControlSheet;
        MainWindow.OnAnimationSelected -= UpdateControlSheet;
    }

    [EditorEvent.Hotload]
    void UpdateControlSheet()
    {
        if (MainWindow?.SelectedAnimation is null) return;

        controlSheet?.Clear(true);

        var serializedObject = MainWindow.SelectedAnimation.GetSerialized();
        var props = serializedObject.Where(x => x.HasAttribute<PropertyAttribute>())
                            .OrderBy(x => x.SourceLine)
                            .ThenBy(x => x.DisplayName)
                            .ToArray();

        // controlSheet.AddRow( serializedObject.GetProperty( nameof( SpriteResource.ResourceName ) ) );

        foreach (var prop in props)
        {
            controlSheet.AddRow(prop);
        }
    }


}