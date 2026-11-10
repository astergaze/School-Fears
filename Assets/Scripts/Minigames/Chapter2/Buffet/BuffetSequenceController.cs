using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Secuencia del buffet. Va en un GameObject vacío de la escena.
///  1) Al empezar: monólogo ("debo ir en silencio") y popup de objetivo.
///  2) BeginPatrol(): lo llama la PlayerTriggerZone del escondite al SALIR de ella; después de un
///     pequeño delay el Rector empieza a patrullar.
/// </summary>
public class BuffetSequenceController : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private DialogueLine[] introLines;
    [SerializeField] private float introDelay = 1.2f;
    [Tooltip("Para que el monólogo no se repita si el jugador vuelve a entrar al buffet.")]
    [SerializeField] private string introDoneFlag = "buffet_intro_done";

    [Header("Objetivo en pantalla")]
    [Tooltip("ID del CSV. Si está vacío se usa Objective Direct Text.")]
    [SerializeField] private string objectiveTextId;
    [TextArea(1, 3)]
    [SerializeField] private string objectiveDirectText = "Llegá a las escaleras sin que te vea.";

    [Header("Rector")]
    [SerializeField] private BuffetRectorAI rector;
    [SerializeField] private float patrolStartDelay = 1.0f;
    public UnityEvent onPatrolBegin;

    private bool begun;

    private IEnumerator Start()
    {
        var gm = GameManager.Instance;
        bool alreadyDone = gm != null && gm.HasFlag(introDoneFlag);

        if (introLines != null && introLines.Length > 0 && !alreadyDone && DialogueManager.Instance != null)
        {
            yield return new WaitForSeconds(introDelay);

            bool ended = false;
            Action handler = null;
            handler = () =>
            {
                ended = true;
                if (DialogueManager.Instance != null) DialogueManager.Instance.OnDialogueEnded -= handler;
            };
            DialogueManager.Instance.OnDialogueEnded += handler;
            DialogueManager.Instance.StartDialogue(introLines);
            gm?.SetFlag(introDoneFlag);

            while (!ended) yield return null;
        }

        ShowObjective();
    }

    private void ShowObjective()
    {
        if (ObjectivePopupUI.Instance == null) return;
        if (!string.IsNullOrEmpty(objectiveTextId)) ObjectivePopupUI.Instance.ShowMessageById(objectiveTextId);
        else if (!string.IsNullOrEmpty(objectiveDirectText)) ObjectivePopupUI.Instance.ShowMessage(objectiveDirectText);
    }

    /// <summary>Engancharlo al evento "On Player Exit" de la zona del escondite inicial.</summary>
    public void BeginPatrol()
    {
        if (begun) return;
        begun = true;
        StartCoroutine(BeginRoutine());
    }

    private IEnumerator BeginRoutine()
    {
        yield return new WaitForSeconds(patrolStartDelay);
        if (rector != null) rector.StartPatrol();
        onPatrolBegin?.Invoke();
    }
}
