using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Zona genérica (BoxCollider): dispara eventos cuando el Player entra / sale.
/// Se fuerza a Trigger. Ejemplos en el buffet: la zona del escondite inicial (al SALIR llama a
/// BuffetSequenceController.BeginPatrol).
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerTriggerZone : MonoBehaviour
{
    [Tooltip("Cada evento se dispara una sola vez.")]
    [SerializeField] private bool oneShot = true;
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    private bool enterFired;
    private bool exitFired;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneShot && enterFired) return;
        enterFired = true;
        onPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (oneShot && exitFired) return;
        exitFired = true;
        onPlayerExit?.Invoke();
    }

    public void ResetZone() { enterFired = false; exitFired = false; }
}
