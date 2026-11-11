using UnityEngine;

public class MenuMusic : MonoBehaviour
{
    [SerializeField] private AudioClip menuMusicClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private void Start()
    {
        if (AudioManager.Instance != null && menuMusicClip != null)
        {
            AudioManager.Instance.PlayMusic(menuMusicClip, volume);
        }
    }
}