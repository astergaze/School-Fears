using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detecta la primera vez que el jugador tiene línea de visión directa hacia la luz
/// guía, dentro de un ángulo razonable y una distancia máxima, y dispara un evento
/// una única vez. Enganchar onLightSpotted -> TentaclePathManager.BeginChase().
///
/// Usa un chequeo por ÁNGULO (no WorldToViewportPoint) porque el punto exacto del
/// transform de la Spot Light suele estar arriba de todo (cerca del techo), mientras
/// que el cono/resplandor ya es visible con la cámara apuntando bastante más abajo.
/// Pedir el punto exacto en pantalla obligaba a levantar la mirada de más.
/// </summary>
public class PlayerLightVisibility : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara del jugador. Si se deja vacío, se busca Camera.main en runtime (lazy, por si el Player se instancia después)")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Transform de la Spot Light a detectar")]
    [SerializeField] private Transform lightTransform;

    [Header("Detección")]
    [Tooltip("Distancia máxima a la que se puede 'spottear' la luz, para que no arranque la secuencia desde cualquier punto del mapa")]
    [SerializeField] private float maxSpotDistance = 15f;
    [Tooltip("Ángulo máximo (grados) entre hacia dónde mirás y la dirección hacia la luz. Generoso a propósito: no hace falta centrarla en pantalla, alcanza con que esté más o menos adelante")]
    [SerializeField] private float viewAngleThreshold = 55f;
    [Tooltip("Capas que bloquean la línea de visión (paredes, puertas sólidas, etc.). Los triggers SIEMPRE se ignoran, sin importar la layer.")]
    [SerializeField] private LayerMask obstructionMask;

    public UnityEvent onLightSpotted;

    private bool alreadySpotted = false;

    private void Update()
    {
        if (alreadySpotted) return;
        if (playerCamera == null) playerCamera = Camera.main;
        if (lightTransform == null || playerCamera == null) return;

        Vector3 toLight = lightTransform.position - playerCamera.transform.position;
        float distance = toLight.magnitude;

        bool inRange = distance <= maxSpotDistance;
        bool inAngle = inRange && Vector3.Angle(playerCamera.transform.forward, toLight) <= viewAngleThreshold;
        bool los = inAngle && HasLineOfSight(toLight, distance);

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[PlayerLightVisibility] inRange={inRange} inAngle={inAngle} lineOfSight={los}");
        }

        if (inAngle && los)
        {
            alreadySpotted = true;
            Debug.Log("[PlayerLightVisibility] Luz detectada -> invocando onLightSpotted");
            onLightSpotted?.Invoke();
        }
    }

    private bool HasLineOfSight(Vector3 toLight, float distance)
    {
        if (distance <= 0.05f) return true;

        // QueryTriggerInteraction.Ignore: los BoxCollider trigger de los nodos del
        // camino NO cuentan como obstrucción, solo colliders sólidos reales.
        return !Physics.Raycast(
            playerCamera.transform.position,
            toLight.normalized,
            distance - 0.1f,
            obstructionMask,
            QueryTriggerInteraction.Ignore);
    }
}