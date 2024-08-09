using System;
using System.Linq;
using System.Reflection;
using Editor;
using Sandbox;

namespace SpriteTools.TilesetTool;

[CanEdit(typeof(TilesetTool))]
public class TilesetToolInspector : InspectorWidget
{
    public static TilesetToolInspector Active { get; private set; }
    TilesetTool Tool;
    StatusWidget Header;

    ScrollArea scrollArea;
    ControlSheet mainSheet;
    ControlSheet selectedSheet;

    internal Preview.Preview Preview;

    public TilesetToolInspector(SerializedObject so) : base(so)
    {
        if (so.Targets.FirstOrDefault() is not TilesetTool tool) return;

        Tool = tool;
        Tool.UpdateInspector += UpdateHeader;
        Tool.UpdateInspector += UpdateSelectedSheet;

        Layout = Layout.Column();
        Layout.Margin = 4;
        Layout.Spacing = 8;

        Active = this;
        Rebuild();
    }

    [EditorEvent.Hotload]
    void Rebuild()
    {
        if (Layout is null) return;
        Layout.Clear(true);

        scrollArea = new ScrollArea(this);
        scrollArea.Canvas = new Widget();
        scrollArea.Canvas.Layout = Layout.Column();
        scrollArea.Canvas.VerticalSizeMode = SizeMode.CanGrow;
        scrollArea.Canvas.HorizontalSizeMode = SizeMode.Flexible;
        scrollArea.Canvas.Layout.Spacing = 8;
        Layout.Add(scrollArea);

        Header = new StatusWidget(this);
        scrollArea.Canvas.Layout.Add(Header);
        UpdateHeader();

        mainSheet = new ControlSheet();
        scrollArea.Canvas.Layout.Add(mainSheet);
        UpdateMainSheet();

        UpdateSelectedSheet();

        Preview = new Preview.Preview(this);
        scrollArea.Canvas.Layout.Add(Preview);

        scrollArea.Canvas.Layout.AddStretchCell();

    }

    internal void UpdateHeader()
    {
        Header.Text = "Paint Tiles";
        Header.LeadText = Tool.SelectedLayer == null ? "No Layer Selected" : $"Selected Layer: {Tool.SelectedLayer.Name}";
        Header.Color = (false) ? Theme.Red : Theme.Blue;
        Header.Icon = (false) ? "warning" : "dashboard";
        Header.Update();
    }

    internal void UpdateMainSheet()
    {
        if (!(Layout?.IsValid ?? false)) return;
        if (mainSheet is null) return;

        mainSheet?.Clear(true);

        if (Tool?.CurrentTool is not null)
        {
            var toolName = (Tool.CurrentTool.GetType()?.GetCustomAttribute<TitleAttribute>()?.Value ?? "Unknown") + " Tool";
            mainSheet.AddObject(Tool.CurrentTool.GetSerialized(), toolName, x => x.HasAttribute<PropertyAttribute>() && x.PropertyType != typeof(Action));
        }
        if (Tool.SelectedComponent.IsValid())
        {
            mainSheet.AddObject(Tool.SelectedComponent.GetSerialized(), null, x =>
            {
                if (!x.HasAttribute<PropertyAttribute>()) return false;
                if (x.PropertyType == typeof(Action)) return false;
                switch (x.Name)
                {
                    case nameof(Collider.Static):
                    case nameof(Collider.Surface):
                    case nameof(Collider.IsTrigger):
                    case nameof(Collider.OnTriggerEnter):
                    case nameof(Collider.OnTriggerExit):
                        return false;
                }
                return true;
            });
        }
    }

    internal void UpdateSelectedSheet()
    {
        if (!(Layout?.IsValid ?? false)) return;

        if (!(selectedSheet?.IsValid ?? false))
        {
            selectedSheet = new ControlSheet();
            scrollArea.Canvas.Layout.Add(selectedSheet);
        }

        selectedSheet?.Clear(true);
        if (Tool.SelectedLayer is not null)
        {
            selectedSheet.AddObject(Tool.SelectedLayer.GetSerialized(), null, x => x.HasAttribute<PropertyAttribute>() && x.PropertyType != typeof(Action));
        }
    }

    private class StatusWidget : Widget
    {
        public string Icon { get; set; }
        public string Text { get; set; }
        public string LeadText { get; set; }
        public Color Color { get; set; }

        public StatusWidget(Widget parent) : base(parent)
        {
            MinimumSize = 48;
            SetSizeMode(SizeMode.Default, SizeMode.CanShrink);
        }

        protected override void OnPaint()
        {
            var rect = new Rect(0, Size);

            Paint.ClearPen();
            Paint.SetBrush(Theme.Black.Lighten(0.9f));
            Paint.DrawRect(rect);

            rect.Left += 8;

            Paint.SetPen(Color);
            var iconRect = Paint.DrawIcon(rect, Icon, 24, TextFlag.LeftCenter);

            rect.Top += 8;
            rect.Left = iconRect.Right + 8;

            Paint.SetPen(Color);
            Paint.SetDefaultFont(10, 500);
            var titleRect = Paint.DrawText(rect, Text, TextFlag.LeftTop);

            rect.Top = titleRect.Bottom + 2;

            Paint.SetPen(Color.WithAlpha(0.6f));
            Paint.SetDefaultFont(8, 400);
            Paint.DrawText(rect, LeadText, TextFlag.LeftTop);
        }
    }
}