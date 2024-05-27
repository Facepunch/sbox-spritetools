using System;
using Sandbox;

namespace Quest;

public sealed class CameraController : Component
{
	public enum CameraMode
	{
		Static,
		PointAt,
		Offset
	}

	public CameraMode Mode { get; private set; }
	public GameTransform TransformA { get; private set; }
	public GameTransform TransformB { get; private set; }
	public BBox Range { get; private set; }

	public PhysicsLock Lock { get; private set; }
	public bool RelativeLock { get; private set; }
	public float LerpSpeed { get; private set; }


	protected override void OnUpdate()
	{
		if ( TransformA is null || TransformB is null ) return;
		var position = TransformA.Position;
		var rotation = TransformA.Rotation;
		var targetPosition = TransformB.Position;
		var lerpSpeed = 1f - MathF.Pow( 0.5f, LerpSpeed * Time.Delta );

		switch ( Mode )
		{
			case CameraMode.PointAt:
				rotation = Rotation.LookAt( targetPosition - Transform.Position );
				break;
			case CameraMode.Offset:
				position = Player.Local.Transform.Position + TransformA.LocalPosition;
				if ( Lock.X ) position = position.WithX( TransformA.Position.x );
				if ( Lock.Y ) position = position.WithY( TransformA.Position.y );
				if ( Lock.Z ) position = position.WithZ( TransformA.Position.z );
				if ( Range.Mins.x != Range.Maxs.x ) position = position.WithX( position.x.Clamp( Range.Mins.x, Range.Maxs.x ) );
				if ( Range.Mins.y != Range.Maxs.y ) position = position.WithY( position.y.Clamp( Range.Mins.y, Range.Maxs.y ) );
				if ( Range.Mins.z != Range.Maxs.z ) position = position.WithZ( position.z.Clamp( Range.Mins.z, Range.Maxs.z ) );
				break;
		}
		Transform.Position = Transform.Position.LerpTo( position, lerpSpeed );
		Transform.Rotation = Rotation.Slerp( Transform.Rotation, rotation, lerpSpeed );
	}

	public void Set( CameraMode mode, GameObject transformA, GameObject transformB, float lerpSpeed )
	{
		if ( transformA is not null ) TransformA = transformA.Transform;
		else TransformA = Transform;
		if ( transformB is not null ) TransformB = transformB.Transform;
		else TransformB = Transform;
		Mode = mode;
		LerpSpeed = lerpSpeed;
	}

	public void SetLock( PhysicsLock lockType, bool relativeLock, BBox range )
	{
		Lock = lockType;
		RelativeLock = relativeLock;
		Range = range;
	}
}