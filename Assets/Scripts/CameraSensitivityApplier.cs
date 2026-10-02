using UnityEngine;
using Unity.Cinemachine;

public class CameraSensitivityApplier : MonoBehaviour
{
    public CinemachineInputAxisController inputAxisController;

    [Header("Valores base (los que ya tenés configurados)")]
    public float gainBasePanX = 100f;
    public float gainBaseTiltY = -100f;

    void Start()
    {
        AplicarSensibilidad(PlayerPrefs.GetFloat("MouseSensitivity", 1f));
    }

    public void AplicarSensibilidad(float multiplicador)
    {
        foreach (var controller in inputAxisController.Controllers)
        {
            if (controller.Name == "Look X (Pan)")
                controller.Input.Gain = gainBasePanX * multiplicador;
            else if (controller.Name == "Look Y (Tilt)")
                controller.Input.Gain = gainBaseTiltY * multiplicador;
        }
    }
}