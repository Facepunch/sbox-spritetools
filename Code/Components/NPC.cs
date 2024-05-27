using System;
using Sandbox;

namespace Quest;

public class NPC : Interactable
{

    public override void Interact()
    {
        base.Interact();

        Log.Info( "Hello, I am an NPC!" );
    }
}