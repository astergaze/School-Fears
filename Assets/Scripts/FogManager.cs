using UnityEngine;
using System.Collections; // Necesario para las corrutinas

public class FogManager : MonoBehaviour
{
    [Header("Configuración Inicial de la Niebla")]
    [SerializeField] private bool enableFogOnStart = true;
    [SerializeField] private Color fogColor = new Color(0f, 0f, 0f); // Lo dejé en negro para el terror
    [Tooltip("Linear es ideal para juegos de terror realistas")]
    [SerializeField] private FogMode fogMode = FogMode.Linear;
    [SerializeField] private float fogStartDistance = 0f;
    [SerializeField] private float fogEndDistance = 3f; // Un buen valor de inicio

    private void Start()
    {
        // Aplicamos la configuración inicial a los ajustes de renderizado de Unity al arrancar
        RenderSettings.fog = enableFogOnStart;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogStartDistance = fogStartDistance;
        RenderSettings.fogEndDistance = fogEndDistance;
    }

    // Método público por si algún evento necesita cambiar la niebla de golpe
    public void SetFogDistance(float newDistance)
    {
        RenderSettings.fogEndDistance = newDistance;
    }

    // Método público para que otros scripts activen la transición suave
    public void TransitionFogDistance(float targetDistance, float duration)
    {
        StartCoroutine(SmoothFogTransition(targetDistance, duration));
    }

    // Corrutina que anima la niebla acercándose o alejándose
    private IEnumerator SmoothFogTransition(float targetDistance, float duration)
    {
        float startDistance = RenderSettings.fogEndDistance;
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            RenderSettings.fogEndDistance = Mathf.Lerp(startDistance, targetDistance, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null; // Espera al siguiente frame
        }
        
        // Asegura que el valor final sea el exacto
        RenderSettings.fogEndDistance = targetDistance;
    }
}