// Objeto movible (puzzle)
using UnityEngine;

public class MoveInteractable : Interactable
{
    private bool isBeingMoved = false;

    public override void Interact(Player player)
    {
        isBeingMoved = !isBeingMoved;
        Debug.Log(isBeingMoved ? "Grabbing object" : "Released object");
        // Acá va la lógica de drag/push
    }
}