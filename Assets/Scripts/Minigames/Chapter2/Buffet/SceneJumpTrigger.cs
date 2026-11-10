using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Trigger que, al entrar el Player, hace fade a negro y carga otra escena (usa SceneTransitioner).
/// En el buffet: el "empty" al pie de la escalera, con Target Scene = detencionloop1.
/// La escena destino tiene que estar en Build Settings.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SceneJumpTrigger : MonoBehaviour
{
    [SerializeField] private string targetScene = "detencionloop1";
    [Tooltip("Pausa antes de empezar el fade (para dejar que la niebla termine de cerrarse).")]
    [SerializeField] private float delay = 0f;
    [SerializeField] private AudioClip sound;
    [Tooltip("Flag opcional del GameManager que se marca al disparar (ej: fell_down_stairs).")]
    [SerializeField] private string flagToSet = "";
    public UnityEvent onTriggered;

    private bool fired;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired || !other.CompareTag("Player")) return;
        fired = true;
        StartCoroutine(Go());
    }

    private IEnumerator Go()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (!string.IsNullOrEmpty(flagToSet)) GameManager.Instance?.SetFlag(flagToSet);
        if (sound != null && AudioManager.Instance != null) AudioManager.Instance.PlaySFX(sound);
        onTriggered?.Invoke();

        if (SceneTransitioner.Instance != null) SceneTransitioner.Instance.GoToScene(targetScene, null, null);
        else SceneManager.LoadScene(targetScene);
    }
}
