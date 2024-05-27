using System;
using Sandbox;

namespace Quest;

public class Interactable : Component
{
    [Property] Action OnInteract { get; set; }

    public virtual void Interact()
    {
        OnInteract?.Invoke();
    }
}