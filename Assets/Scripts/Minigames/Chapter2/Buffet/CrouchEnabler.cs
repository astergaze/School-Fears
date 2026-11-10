using System.Collections;
using UnityEngine;

/// <summary>
/// Poné uno en un GameObject vacío de la escena donde SÍ se puede agachar (el buffet).
/// Al destruirse la escena, el CrouchController vuelve a quedar deshabilitado solo.
/// </summary>
public class CrouchEnabler : MonoBehaviour
{
    [Tooltip("Alex empieza agachado detrás del mostrador hasta que el jugador toca la tecla de agacharse.")]
    [SerializeField] private bool startCrouched = true;

    [Header("Aviso de control (opcional)")]
    [SerializeField] private bool showHint = true;
    [SerializeField] private float hintDelay = 2.5f;
    [Tooltip("ID del CSV. Si está vacío se usa Hint Direct Text.")]
    [SerializeField] private string hintTextId;
    [TextArea(1, 3)]
    [SerializeField] private string hintDirectText = "Mantené CTRL para agacharte.";
    [SerializeField] private string hintShownFlag = "crouch_hint_shown";

    private IEnumerator Start()
    {
        float t = 0f;
        while (CrouchController.Instance == null && t < 3f)
        {
            t += Time.deltaTime;
            yield return null;
        }

        var cc = CrouchController.Instance;
        if (cc == null)
        {
            Debug.LogWarning("CrouchEnabler: el Player no tiene CrouchController.");
            yield break;
        }

        cc.SetAllowed(true);
        if (startCrouched) cc.StartCrouched();

        if (!showHint) yield break;
        var gm = GameManager.Instance;
        if (gm != null && gm.HasFlag(hintShownFlag)) yield break;

        yield return new WaitForSeconds(hintDelay);

        if (ObjectivePopupUI.Instance == null) yield break;
        if (!string.IsNullOrEmpty(hintTextId)) ObjectivePopupUI.Instance.ShowMessageById(hintTextId);
        else if (!string.IsNullOrEmpty(hintDirectText)) ObjectivePopupUI.Instance.ShowMessage(hintDirectText);
        gm?.SetFlag(hintShownFlag);
    }

    private void OnDestroy()
    {
        if (CrouchController.Instance != null) CrouchController.Instance.SetAllowed(false);
    }
}
