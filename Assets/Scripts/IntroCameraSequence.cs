using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using System.Collections;

public class IntroCameraSequence : MonoBehaviour
{
    public CinemachineCamera camInicio;
    public CinemachineCamera camPizarra;
    public Light luzPizarra;

    [Header("Paneles (deben tener Canvas Group)")]
    public GameObject titleScreen;
    public CanvasGroup titleScreenGroup;
    public GameObject mainMenuPanel;
    public CanvasGroup mainMenuPanelGroup;
    public GameObject continuePanel;

    public float duracionLuz = 2f;
    public float duracionFade = 2f; // tiempo del fade, ajustable desde el Inspector

    void Start()
    {
        // Estado inicial
        camInicio.Priority = 20;
        camPizarra.Priority = 10;
        luzPizarra.intensity = 0f;

        mainMenuPanel.SetActive(false);
        continuePanel.SetActive(false);

        titleScreen.SetActive(true);
        titleScreenGroup.alpha = 1f;

        StartCoroutine(SecuenciaIntro());
    }

    IEnumerator SecuenciaIntro()
    {
        yield return StartCoroutine(EsperarCualquierTecla());

        camPizarra.Priority = 30;

        // Mientras la cámara viaja, el texto de intro se va desvaneciendo
        yield return StartCoroutine(FadeOut(titleScreenGroup, duracionFade));
        titleScreen.SetActive(false);

        // Esperar el resto del blend de cámara si hace falta
        // (duracionFade ya corrió en paralelo con el movimiento de cámara)
        yield return new WaitForSeconds(Mathf.Max(0f, 3f - duracionFade));

        // Prender la luz gradualmente
        float t = 0f;
        while (t < duracionLuz)
        {
            t += Time.deltaTime;
            luzPizarra.intensity = Mathf.Lerp(0f, 70f, t / duracionLuz);
            yield return null;
        }

        // Mostrar el menú con fade in
        mainMenuPanel.SetActive(true);
        mainMenuPanelGroup.alpha = 0f;
        yield return StartCoroutine(FadeIn(mainMenuPanelGroup, duracionFade));
    }

    IEnumerator FadeOut(CanvasGroup group, float duracion)
    {
        float t = 0f;
        float inicio = group.alpha;
        while (t < duracion)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(inicio, 0f, t / duracion);
            yield return null;
        }
        group.alpha = 0f;
    }

    IEnumerator FadeIn(CanvasGroup group, float duracion)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(0f, 1f, t / duracion);
            yield return null;
        }
        group.alpha = 1f;
    }

    IEnumerator EsperarCualquierTecla()
    {
        yield return null;
        while (true)
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                yield break;
            yield return null;
        }
    }
}