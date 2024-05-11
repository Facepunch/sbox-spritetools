using Editor;
using Sandbox;
using System;
using System.Linq;

namespace SpriteTools.SpriteEditor.Inspector;

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

        var importLayout = scroller.Canvas.Layout.Add(Layout.Row());
        importLayout.Margin = new Sandbox.UI.Margin(16, 8, 16, 0);
        var importButton = new Button(this);
        importButton.Text = "Import Spritesheet";
        importButton.Icon = "add_to_photos";
        importButton.ToolTip = "Import a spritesheet to replace the current animation.";
        importButton.MouseClick = MainWindow.PromptImportSpritesheet;
        importLayout.Add(importButton);

        scroller.Canvas.Layout.Add(controlSheet);
        scroller.Canvas.Layout.AddStretchCell();
        Layout.Add(scroller);

        SetSizeMode(SizeMode.Default, SizeMode.CanShrink);

        UpdateControlSheet();
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

        var attachmentProp = serializedObject.GetProperty(nameof(MainWindow.SelectedAnimation.Attachments));
        if (attachmentProp is not null)
        {
            var row = new GridLayout();
            var attachmentControl = new AttachmentListControlWidget(attachmentProp, MainWindow);

            row.SetMinimumColumnWidth(0, 154);
            row.SetColumnStretch(0, 1);

            var label = row.AddCell(0, 0, new Label("Attachments") { MinimumHeight = Theme.RowHeight, Alignment = TextFlag.Center }, 2, 1, TextFlag.LeftTop);
            label.MinimumHeight = Theme.RowHeight;
            label.Alignment = TextFlag.LeftCenter;
            label.SetStyles("color: #888;");
            label.ToolTip = attachmentProp.Description ?? attachmentProp.DisplayName;
            // label.ContentMargins = new Sandbox.UI.Margin(4, 0, 0, 0);

            var lo = row.AddCell(1, 0, Layout.Column(), 2, 1, TextFlag.LeftTop);
            lo.Margin = new Sandbox.UI.Margin(16, 0, 0, 0);
            lo.Add(attachmentControl);

            controlSheet.AddLayout(row);
        }

        serializedObject.OnPropertyChanged += (prop) =>
        {
            if (prop is null) return;
            if (!prop.HasAttribute<PropertyAttribute>()) return;

            var undoName = $"Modify {prop.Name}";

            string buffer = "";
            if (MainWindow.UndoStack.MostRecent is not null)
            {
                if (MainWindow.UndoStack.MostRecent.name == undoName)
                {
                    buffer = MainWindow.UndoStack.MostRecent.undoBuffer;
                    MainWindow.UndoStack.PopMostRecent();
                }
                else
                {
                    buffer = MainWindow.UndoStack.MostRecent.redoBuffer;
                }
            }

            MainWindow.PushUndo(undoName, buffer);
            MainWindow.PushRedo();

            MainWindow.SetDirty();
        };
    }


}