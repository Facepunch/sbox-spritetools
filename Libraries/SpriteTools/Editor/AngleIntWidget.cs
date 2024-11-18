using Editor;
using Sandbox;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteTools;

[CustomEditor(typeof(int), NamedEditor = "angle")]
public class AngleIntWidget : ControlWidget
{
    public AngleIntWidget(SerializedProperty prop) : base(prop)
    {
        Layout = Layout.Row();
        Layout.Spacing = 4;

        var intWidget = new IntegerControlWidget(prop);
        intWidget.ReadOnly = true;
        Layout.Add(intWidget);

        var rotateLeft = new IconButton("rotate_left");
        rotateLeft.OnClick += () =>
        {
            var angle = prop.GetValue<int>(0);
            angle -= 90;
            if (angle < 0) angle = 270;
            prop.SetValue(angle);
        };
        Layout.Add(rotateLeft);

        var rotateRight = new IconButton("rotate_right");
        rotateRight.OnClick += () =>
        {
            var angle = prop.GetValue<int>(0);
            angle += 90;
            if (angle > 270) angle = 0;
            prop.SetValue(angle);
        };
        Layout.Add(rotateRight);

    }

    protected override void PaintUnder()
    {

    }
}