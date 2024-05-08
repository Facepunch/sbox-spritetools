using System;
using Editor;
using Sandbox;

namespace SpriteTools.SpriteEditor.Preview;

public class Draggable : SceneObject
{
    public Action<Vector2> OnPositionChanged;

    public Draggable(SceneWorld world, string model, Transform transform) : base(world, model, transform)
    {
    }
}