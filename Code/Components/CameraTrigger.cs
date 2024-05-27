using Sandbox;

namespace Quest;

public sealed class CameraTrigger : Component, Component.ITriggerListener
{
	[Property] CameraController.CameraMode Mode { get; set; }
	[Property] PhysicsLock CameraLock { get; set; }
	[Property] BBox Range { get; set; }
	[Property] bool RelativeLock { get; set; } = false;
	[Property] float LerpSpeed { get; set; } = 10.0f;

	[Property] GameObject TransformA { get; set; }
	[Property] GameObject TransformB { get; set; }

	protected override void OnStart()
	{
		TransformB ??= Player.Local.PointAt;
		BBox box = new BBox( Transform.Position, Transform.Scale );
		var meshComponent = Components.Get<MeshComponent>();
		if ( meshComponent is not null )
		{
			box = meshComponent.Mesh.CalculateBounds();
			box = box.Translate( Transform.Position );
		}
		var playerPos = Player.Local.Transform.Position;
		if ( box.Contains( playerPos ) )
		{
			var camera = Scene.Camera.Components.Get<CameraController>();
			if ( camera is null ) return;
			camera.Set( Mode, TransformA, TransformB, LerpSpeed );
			camera.SetLock( CameraLock, RelativeLock, Range );

			Log.Info( GameObject.Name );
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "player" ) && !other.Tags.Has( "trigger" ) )
		{
			var camera = Scene.Camera.Components.Get<CameraController>();
			if ( camera is null ) return;
			camera.Set( Mode, TransformA, TransformB, LerpSpeed );
			camera.SetLock( CameraLock, RelativeLock, Range );
		}
	}

	public void OnTriggerExit( Collider other )
	{

	}
}