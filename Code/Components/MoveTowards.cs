using System;
using Sandbox;

public sealed class MoveTowards : Component
{
	[Property] GameObject Target { get; set; }
	[Property] float Speed { get; set; } = 10f;

	protected override void OnFixedUpdate()
	{
		float speed = 1f - MathF.Pow(0.5f, Speed * Time.Delta);
		Transform.Position = Vector3.Lerp(Transform.Position, Target.Transform.Position, speed);
	}
}
