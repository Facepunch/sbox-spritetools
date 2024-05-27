using System;
using Sandbox;

namespace Quest;

public sealed class PlayerTrigger : Component, Component.ITriggerListener
{
	[Property] Action<Collider> OnTrigger { get; set; }

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "player" ) && !other.Tags.Has( "trigger" ) )
			OnTrigger?.Invoke( other );
	}
}