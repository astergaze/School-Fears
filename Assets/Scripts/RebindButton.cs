using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RebindButton : MonoBehaviour
{
    [Header("Acción y binding específico")]
    public InputActionReference actionReference; // arrastrá "Move" acá
    public int bindingIndex; // usá la tabla: 2=Adelante, 4=Atrás, 6=Izquierda, 8=Derecha

    [Header("UI")]
    public TMP_Text teclaActualText;

    InputActionRebindingExtensions.RebindingOperation operacionActual;

    void Start()
    {
        ActualizarTexto();
    }

    public void IniciarRebind()
    {
        teclaActualText.text = "Presiona una tecla...";

        actionReference.action.Disable();

        operacionActual = actionReference.action.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation => FinalizarRebind())
            .OnCancel(operation => FinalizarRebind())
            .Start();
    }

    void FinalizarRebind()
    {
        operacionActual.Dispose();
        actionReference.action.Enable();
        ActualizarTexto();

        string rebinds = actionReference.action.actionMap.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("rebinds", rebinds);
    }

    void ActualizarTexto()
    {
        teclaActualText.text = InputControlPath.ToHumanReadableString(
            actionReference.action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice);
    }
}