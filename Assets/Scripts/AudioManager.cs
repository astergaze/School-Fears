using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Singleton persistente para reproducir efectos de sonido y música en loop.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("Cantidad de AudioSource internos para poder solapar sonidos de SFX")]
    [SerializeField] private int poolSize = 8;
    
    private AudioSource[] pool;
    private int nextSource = 0;

    // Canal dedicado para música de fondo
    private AudioSource musicSource;

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pool para SFX
        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            pool[i] = src;
        }

        // AudioSource independiente para música
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Se ejecuta automáticamente en cuanto la nueva escena termina de cargar
        StopMusic();
    }

    /// <summary>
    /// Reproduce una pista de música en loop. Si la misma pista ya está sonando, no la reinicia.
    /// </summary>
    public void PlayMusic(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.Play();
    }

    /// <summary>
    /// Detiene la música de fondo actual.
    /// </summary>
    public void StopMusic()
    {
        musicSource.Stop();
        musicSource.clip = null;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null) return;
        AudioSource src = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Length;
        src.pitch = Random.Range(pitchMin, pitchMax);
        src.PlayOneShot(clip, volume);
    }

    public AudioClip PickRandom(AudioClip[] clips, ref int lastIndex)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];
        int index;
        do { index = Random.Range(0, clips.Length); }
        while (index == lastIndex);
        lastIndex = index;
        return clips[index];
    }
}