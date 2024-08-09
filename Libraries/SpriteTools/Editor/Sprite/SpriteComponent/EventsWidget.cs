using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using Sandbox;

namespace SpriteTools;

[CustomEditor(typeof(SpriteComponent.BroadcastControls))]
public class SpriteComponentControlWidget : ControlWidget
{
    public override bool SupportsMultiEdit => false;
    SerializedObject serializedObject;
    SpriteComponent spriteComponent;

    public SpriteComponentControlWidget(SerializedProperty property) : base(property)
    {
        spriteComponent = property.Parent.Targets.First() as SpriteComponent;
        serializedObject = spriteComponent?.GetSerialized();
        if (serializedObject is null)
        {
            return;
        }

        Layout = Layout.Column();
        Layout.Spacing = 2;

        Rebuild();
    }

    protected override void OnPaint()
    {

    }

    void Rebuild()
    {
        Layout.Clear(true);
        serializedObject.TryGetProperty(nameof(SpriteComponent.BroadcastEvents), out var events);
        Layout.Add(new DictionaryActionControlWidget(events, spriteComponent));
    }

    private class DictionaryActionControlWidget : ControlWidget
    {
        SpriteComponent Component;
        SerializedCollection Collection;
        Layout Content;

        public DictionaryActionControlWidget(SerializedProperty property, SpriteComponent component) : base(property)
        {
            Component = component;

            Layout = Layout.Column();
            Layout.Spacing = 2;

            if (!property.TryGetAsObject(out var so) || so is not SerializedCollection sc)
                return;

            Collection = sc;
            Collection.OnEntryAdded = Rebuild;
            Collection.OnEntryRemoved = Rebuild;
            Collection.OnPropertyChanged = (prop) => Rebuild();

            Content = Layout.Column();
            Layout.Add(Content);

            Rebuild();
        }

        public override void OnDestroyed()
        {
            base.OnDestroyed();

            Collection.OnEntryAdded = null;
            Collection.OnEntryRemoved = null;
            Collection.OnPropertyChanged = null;
        }

        [EditorEvent.Hotload]
        public void Rebuild()
        {
            if ((Component?.BroadcastEvents?.Count ?? 0) == 0)
            {
                return;
            }

            Content?.Clear(true);
            Content.Margin = 0;

            var grid = Layout.Grid();
            grid.VerticalSpacing = 2;
            grid.HorizontalSpacing = 2;
            grid.SetMinimumColumnWidth(1, 10);
            grid.SetMinimumColumnWidth(3, 150);
            grid.SetColumnStretch(0, 1, 0, 100, 0);

            int y = 0;
            foreach (var entry in Collection)
            {
                var key = entry.GetKey();

                var keyControl = Create(key);
                var valControl = Create(entry);

                var index = y;
                var kc = grid.AddCell(1, y, keyControl, 1, 1, keyControl.CellAlignment);
                kc.MaximumWidth = 64;
                if (kc is StringControlWidget scw)
                {
                    scw.Enabled = false;
                }
                grid.AddCell(2, y, new IconButton(":") { IconSize = 13, Foreground = Theme.ControlText, Background = Color.Transparent, FixedWidth = ControlRowHeight, FixedHeight = ControlRowHeight });
                grid.AddCell(3, y, valControl, 1, 1, valControl.CellAlignment);

                y++;
            }

            Content.Add(grid);
        }

        protected override void OnPaint()
        {
            Paint.Antialiasing = true;
        }

        protected override void OnValueChanged()
        {
            Rebuild();
        }

        protected override int ValueHash
        {
            get
            {
                var hc = new HashCode();
                hc.Add(base.ValueHash);
                hc.Add(Collection);

                if (Component is not null)
                {
                    foreach (var key in Component.BroadcastEvents.Keys)
                    {
                        hc.Add(key);
                    }
                }

                return hc.ToHashCode();
            }
        }
    }

}