using UnityEngine;

public class PauseTabs : MonoBehaviour
{
    [Header("Paneles (arrastra en el mismo orden que los botones)")]
    public GameObject panelControles;
    public GameObject panelAudio;
    public GameObject panelVideo;

    void OnEnable()
    {
        // Al abrir la pausa, mostrar Controles por defecto (opcional)
        MostrarControles();
    }

    public void MostrarControles()
    {
        panelControles.SetActive(true);
        panelAudio.SetActive(false);
        panelVideo.SetActive(false);
    }

    public void MostrarAudio()
    {
        panelControles.SetActive(false);
        panelAudio.SetActive(true);
        panelVideo.SetActive(false);
    }

    public void MostrarVideo()
    {
        panelControles.SetActive(false);
        panelAudio.SetActive(false);
        panelVideo.SetActive(true);
    }
}