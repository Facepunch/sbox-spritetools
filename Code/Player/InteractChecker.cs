using Sandbox;

namespace Quest;

public class InteractChecker : Component, Component.ITriggerListener
{

    public Interactable Interactable;

    protected override void OnFixedUpdate()
    {
        if ( Input.Pressed( "Use" ) )
        {
            Interactable?.Interact();
        }
    }

    public void OnTriggerEnter( Collider other )
    {
        if ( Interactable.IsValid() ) return;

        if ( other.Tags.Has( "interactable" ) )
        {
            Interactable = other.GameObject.Components.Get<Interactable>();
        }
    }

    public void OnTriggerExit( Collider other )
    {
        if ( !Interactable.IsValid() ) return;

        if ( other.Tags.Has( "interactable" ) && other.Components.Get<Interactable>() is Interactable interactable )
        {
            if ( interactable == Interactable )
            {
                Interactable = null;
            }
        }
    }
}