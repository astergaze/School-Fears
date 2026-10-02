using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    [Header("Sensibilidad")]
    public Slider sensibilidadSlider;
    public TMP_Text sensibilidadValueText;

    void Start()
    {
        float sensGuardada = PlayerPrefs.GetFloat("MouseSensitivity", 1f);
        sensibilidadSlider.value = sensGuardada;
        ActualizarTextoSensibilidad(sensGuardada);

        sensibilidadSlider.onValueChanged.AddListener(OnSensibilidadCambiada);
    }

    void OnSensibilidadCambiada(float valor)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", valor);
        ActualizarTextoSensibilidad(valor);

        var cameraSens = FindFirstObjectByType<CameraSensitivityApplier>();
        if (cameraSens != null)
            cameraSens.AplicarSensibilidad(valor);
    }

    void ActualizarTextoSensibilidad(float valor)
    {
        sensibilidadValueText.text = valor.ToString("0.00");
    }
}