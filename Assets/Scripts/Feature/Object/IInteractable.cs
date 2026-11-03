using UnityEngine;

public interface IInteractable
{
    public Vector2 Position { get; }
    public void Interact(Player player);
}