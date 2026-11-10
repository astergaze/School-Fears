using UnityEngine;

/// <summary>Globales de niebla extra. El shader VolumetricFog tiene que leer _FogExtraDensity (ver guía).</summary>
public static class FogGlobals
{
    private static readonly int ExtraDensityId = Shader.PropertyToID("_FogExtraDensity");
    public static void SetExtraDensity(float value) => Shader.SetGlobalFloat(ExtraDensityId, value);
}

/// <summary>
/// Zona de la escalera: a medida que el jugador baja, la niebla se va haciendo más densa y la linterna
/// va perdiendo su efecto de limpiar la niebla. Al llegar abajo (SceneJumpTrigger) se hace fade y se
/// va a la escena de detención.
///
/// Va en un objeto con Collider (se fuerza a Trigger) que cubra toda la escalera.
/// Top Point = arriba de los escalones, Bottom Point = abajo (el progreso se mide proyectando
/// la posición del jugador sobre ese segmento).
/// </summary>
[RequireComponent(typeof(Collider))]
public class StairsFogZone : MonoBehaviour
{
    [SerializeField] private Transform topPoint;
    [SerializeField] private Transform bottomPoint;

    [Tooltip("Niebla extra que se SUMA a la normal cuando el jugador llega abajo (global _FogExtraDensity).")]
    [SerializeField] private float maxExtraDensity = 1.5f;
    [Tooltip("Progreso (0 arriba, 1 abajo) a partir del cual empieza a notarse la niebla.")]
    [SerializeField, Range(0f, 0.9f)] private float startsAtProgress = 0.15f;
    [Tooltip("La linterna deja de limpiar la niebla a medida que se baja.")]
    [SerializeField] private bool blockFlashlight = true;
    [SerializeField] private float smoothing = 5f;

    private float target;
    private float current;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player") || topPoint == null || bottomPoint == null) return;

        Vector3 pos = Player.Instance != null ? Player.Instance.transform.position : other.bounds.center;
        Vector3 axis = bottomPoint.position - topPoint.position;
        float len2 = axis.sqrMagnitude;
        if (len2 < 0.0001f) return;

        float t = Mathf.Clamp01(Vector3.Dot(pos - topPoint.position, axis) / len2);
        target = Mathf.InverseLerp(startsAtProgress, 1f, t);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) target = 0f;
    }

    private void Update()
    {
        current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        if (Mathf.Abs(current - target) < 0.001f) current = target;
        Apply(current);
    }

    private void Apply(float amount)
    {
        float s = Mathf.SmoothStep(0f, 1f, amount);
        FogGlobals.SetExtraDensity(s * maxExtraDensity);
        if (blockFlashlight && FlashlightController.Instance != null)
            FlashlightController.Instance.SetFogBlock(s);
    }

    // Los globales de shader son estáticos y sobreviven al cambio de escena: siempre limpiarlos
    private void OnDisable() { ClearGlobals(); }
    private void OnDestroy() { ClearGlobals(); }

    private void ClearGlobals()
    {
        FogGlobals.SetExtraDensity(0f);
        if (FlashlightController.Instance != null) FlashlightController.Instance.SetFogBlock(0f);
    }

    private void OnDrawGizmosSelected()
    {
        if (topPoint == null || bottomPoint == null) return;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(topPoint.position, bottomPoint.position);
        Gizmos.DrawWireSphere(topPoint.position, 0.2f);
        Gizmos.DrawWireSphere(bottomPoint.position, 0.35f);
    }
}
