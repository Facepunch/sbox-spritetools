using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Linq;

namespace SpriteTools.SpriteEditor.Inspector;

[CustomEditor(typeof(List<SpriteAttachment>))]
public class AttachmentListControlWidget : ControlWidget
{
    MainWindow MainWindow;
    public override bool SupportsMultiEdit => false;

    SerializedCollection Collection;

    Layout Content;

    IconButton addButton;

    public AttachmentListControlWidget(SerializedProperty property, MainWindow window) : base(property)
    {
        MainWindow = window;
        Layout = Layout.Column();
        Layout.Spacing = 2;

        if (!property.TryGetAsObject(out var so) || so is not SerializedCollection sc)
            return;

        Collection = sc;
        Collection.OnEntryAdded = Rebuild;
        Collection.OnEntryRemoved = Rebuild;

        Content = Layout.Column();

        Layout.Add(Content);

        Rebuild();
    }

    public void Rebuild()
    {
        using var _ = SuspendUpdates.For(this);

        Content.Clear(true);
        Content.Margin = 0;

        var grid = Layout.Grid();
        grid.VerticalSpacing = 2;
        grid.HorizontalSpacing = 2;

        int y = 0;
        foreach (var entry in Collection)
        {
            var attachment = entry.GetValue<SpriteAttachment>();

            var control = Create(entry);
            var index = y;
            //grid.AddCell( 0, y, new IconButton( "drag_handle" ) { IconSize = 13, Foreground = Theme.ControlBackground, Background = Color.Transparent, FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight } );
            grid.AddCell(1, y, control, 1, 1, control.CellAlignment);
            var visibilityButton = grid.AddCell(2, y, new IconButton("visibility", () => RemoveEntry(index)) { Background = Theme.ControlBackground, FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
            visibilityButton.ToolTip = "Toggle attachment visibility";
            var clearButton = grid.AddCell(3, y, new IconButton("clear", () => RemoveEntry(index)) { Background = Theme.ControlBackground, FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
            clearButton.ToolTip = "Remove attachment";

            visibilityButton.Icon = (attachment?.Visible ?? true) ? "visibility" : "visibility_off";
            visibilityButton.OnClick = () =>
            {
                MainWindow.PushUndo("Toggle {attachment.Name} visibility");
                attachment.Visible = !attachment.Visible;
                visibilityButton.Icon = attachment.Visible ? "visibility" : "visibility_off";
                MainWindow.PushRedo();
            };

            y++;
        }

        // bottom row
        {
            addButton = grid.AddCell(1, y, new IconButton("add") { Background = Theme.ControlBackground, ToolTip = "Add attachment", FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
            addButton.MouseClick = AddEntry;
        }

        Content.Add(grid);
    }

    void AddEntry()
    {
        Collection.Add(null);
    }

    void RemoveEntry(int index)
    {
        // var prop = Collection.ElementAt(index);
        // var attachment = prop.GetValue<SpriteAttachment>();
        // if (attachment is not null)
        // {
        //     foreach (var frame in MainWindow.SelectedAnimation.Frames)
        //     {
        //         frame.AttachmentPoints.Remove(attachment.Name);
        //     }
        // }
        Collection.RemoveAt(index);
    }

    protected override void OnPaint()
    {
        Paint.Antialiasing = true;

        Paint.ClearPen();
        Paint.SetBrush(Theme.ControlText.Darken(0.6f));

        if (Collection is not null && Collection.Count() > 0)
        {
            //	Paint.DrawRect( Content.OuterRect, 2.0f );
            //	Paint.DrawRect( new Rect( addButton.Position, addButton.Size ).Grow( 0, 8, 0, 0 ), 2.0f );
        }
        else
        {
            //	Paint.DrawRect( new Rect( addButton.Position, addButton.Size ).Grow( 0, 0, 0, 0 ), 2.0f );
        }


    }

}
