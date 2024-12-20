using Sandbox;
using Editor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace SpriteTools.TilesetEditor;


[CustomEditor(typeof(List<AutotileBrush>))]
public class AutotileBrushListControl : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    public override bool IncludeLabel => false;

    internal MainWindow MainWindow;

    internal AutotileBrushControl SelectedBrush;
    internal AutotileBrush.Tile SelectedTile;
    internal List<AutotileBrushControl> Buttons = new();

    internal Layout content;
    ScrollArea scrollArea;
    Button btnNewBrush;
    TilesetResource lastInheritedFrom;

    KeyboardModifiers modifiers;

    public AutotileBrushListControl(SerializedProperty property) : base(property)
    {
        Layout = Layout.Column();
        Layout.Spacing = 8;

        if (property.IsNull)
        {
            property.SetValue(new List<AutotileBrush>());
        }

        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.CanShrink;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.CanGrow;
        scrollArea.MinimumHeight = 300;
        scrollArea.MaximumHeight = 300;
        scrollArea.FixedHeight = 300;
        Layout.Add(scrollArea);

        // Content list
        content = Layout.Column();
        content.Margin = 4;
        content.AddStretchCell();
        scrollArea.Canvas.Layout.Add(content);

        // Buttons
        var row = Layout.AddRow();
        row.AddStretchCell();
        row.Spacing = 8;
        btnNewBrush = row.Add(new Button.Primary("New Autotile Brush", "add"));
        btnNewBrush.ToolTip = "Create a new autotile brush";
        btnNewBrush.Clicked += NewBrushPopup;

        scrollArea.Canvas.Layout.AddStretchCell();

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateList();
    }

    protected override void OnPaint()
    {
        if (MainWindow is null)
        {
            foreach (var window in MainWindow.OpenWindows)
            {
                if (IsDescendantOf(window.inspector))
                {
                    MainWindow = window;
                    window.inspector.autotileBrushList = this;
                    break;
                }
            }
            if (MainWindow is null)
            {
                btnNewBrush.Visible = false;
            }
        }
        else
        {
            if (MainWindow.Tileset.InheritAutotileFrom != lastInheritedFrom)
            {
                lastInheritedFrom = MainWindow.Tileset.InheritAutotileFrom;
                UpdateList();
            }
        }

        Paint.SetBrush(Theme.ControlBackground);
        Paint.SetPen(Theme.ControlBackground);
        Paint.DrawRect(scrollArea.LocalRect, 4);
    }

    public void UpdateList()
    {
        content.Clear(true);
        Buttons.Clear();

        var brushes = SerializedProperty.GetValue<List<AutotileBrush>>();
        var allBrushes = new List<AutotileBrush>();
        var firstBrush = brushes.FirstOrDefault();

        foreach (var brush in brushes)
        {
            allBrushes.Add(brush);
        }

        if (firstBrush is not null)
        {
            var allTilesets = ResourceLibrary.GetAll<TilesetResource>();
            foreach (var tileset in allTilesets)
            {
                if (tileset.AutotileBrushes.Contains(firstBrush))
                {
                    if (tileset.InheritAutotileFrom is not null)
                    {
                        foreach (var inheritedBrush in tileset.InheritAutotileFrom.GetAllAutotileBrushes())
                        {
                            allBrushes.Add(inheritedBrush);
                        }
                    }
                    break;
                }
            }
        }

        foreach (var brush in allBrushes)
        {
            var button = content.Add(new AutotileBrushControl(this, brush));
            if (!brushes.Contains(brush))
            {
                button.Enabled = false;
            }
            Buttons.Add(button);
        }
    }

    internal void SelectBrush(AutotileBrushControl button)
    {
        if (MainWindow is null) return;
        if (SelectedBrush == button && SelectedTile is null) return;

        SelectedBrush = button;
        SelectedTile = null;
        MainWindow.inspector.UpdateSelectedAutotileSheet();
    }

    internal void SelectTile(AutotileTileControl button)
    {
        if (MainWindow is null) return;
        if (SelectedBrush != button.ParentBrush)
        {
            SelectedBrush = button.ParentBrush;
        }
        if (SelectedTile == button.Tile) return;

        SelectedTile = button.Tile;
        MainWindow.inspector.UpdateSelectedAutotileSheet();
    }

    public void DeleteAll()
    {
        var brushes = new List<AutotileBrush>();
        SerializedProperty.SetValue(brushes);
        UpdateList();
    }

    public void DeleteBrush(AutotileBrush brush)
    {
        if (SelectedBrush?.Brush == brush)
        {
            SelectedBrush = null;
            SelectedTile = null;
        }
        var brushes = SerializedProperty.GetValue<List<AutotileBrush>>();
        brushes.Remove(brush);
        SerializedProperty.SetValue(brushes);
        UpdateList();
        MainWindow?.inspector?.UpdateSelectedAutotileSheet();
    }

    void NewBrushPopup()
    {
        var menu = new PopupWidget(null);
        menu.Layout = Layout.Column();
        menu.MinimumWidth = ScreenRect.Width;
        menu.MaximumWidth = ScreenRect.Width;

        ScrollArea scrollArea = menu.Layout.Add(new ScrollArea(this), 1);
        scrollArea.Canvas = new Widget(scrollArea)
        {
            Layout = Layout.Column(),
            VerticalSizeMode = (SizeMode)3,
            HorizontalSizeMode = (SizeMode)3
        };

        IEnumerable<EnumDescription.Entry> enumerableList = EditorTypeLibrary.GetEnumDescription(typeof(AutotileType));
        foreach (var entry in enumerableList)
        {
            var button = scrollArea.Canvas.Layout.Add(new MenuOption(entry));
            button.MouseLeftPress += () =>
            {
                NewBrush((AutotileType)entry.IntegerValue);
                menu?.Close();
            };
        }

        menu.Position = ScreenRect.BottomLeft;
        menu.Visible = true;
        menu.AdjustSize();
        menu.ConstrainToScreen();
        menu.OnPaintOverride = () =>
        {
            Paint.SetBrushAndPen(Theme.ControlBackground);
            Rect rect = Paint.LocalRect;
            Paint.DrawRect(in rect, 0f);
            return true;
        };
    }

    void NewBrush(AutotileType autotileType)
    {
        var layers = SerializedProperty.GetValue<List<AutotileBrush>>();
        layers.Add(new AutotileBrush(autotileType){Tileset = MainWindow.Tileset});
        SerializedProperty.SetValue(layers);
        UpdateList();
        MainWindow?.inspector?.UpdateSelectedAutotileSheet();
    }

    protected override void OnKeyPress(KeyEvent e)
    {
        base.OnKeyPress(e);
        modifiers = e.KeyboardModifiers;
    }

    protected override void OnKeyRelease(KeyEvent e)
    {
        base.OnKeyRelease(e);
        modifiers = e.KeyboardModifiers;
    }

}

file class MenuOption : Widget
{
    EnumDescription.Entry info;

    public MenuOption(EnumDescription.Entry e) : base(null)
    {
        info = e;

        Layout = Layout.Row();
        Layout.Margin = 8;

        if (!string.IsNullOrWhiteSpace(e.Icon))
        {
            Layout.Add(new IconButton(e.Icon) { Background = Color.Transparent, TransparentForMouseEvents = true, IconSize = 18 });
        }

        Layout.AddSpacingCell(8);
        var c = Layout.AddColumn();
        var title = c.Add(new Label(e.Title));
        title.SetStyles("font-size: 12px; font-weight: bold; font-family: Poppins; color: white;");

        if (!string.IsNullOrWhiteSpace(e.Description))
        {
            var desc = c.Add(new Label(e.Description.Trim('\n', '\r', '\t', ' ')));
            desc.WordWrap = true;
            desc.MinimumHeight = 1;
            desc.MinimumWidth = 200;
        }
    }

    protected override void OnPaint()
    {
        if (Paint.HasMouseOver)
        {
            Paint.SetBrushAndPen(Theme.Blue.WithAlpha(0.1f));
            Paint.DrawRect(LocalRect.Shrink(2), 2);
        }
    }
}