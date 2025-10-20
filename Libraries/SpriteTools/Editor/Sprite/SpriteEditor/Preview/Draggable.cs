using Sandbox;
using System;

namespace SpriteTools.SpriteEditor.Preview;

public class Draggable : SceneObject
{
	public Action<Vector2> OnPositionChanged;

	public Draggable ( SceneWorld world, string model, Transform transform ) : base( world, model, transform )
	{
		Tags.Add( "draggable" );
	}
}