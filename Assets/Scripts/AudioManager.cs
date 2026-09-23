using UnityEngine;

/// <summary>
/// Singleton persistente para reproducir efectos de sonido desde cualquier script,
/// sin que cada uno necesite armar su propio AudioSource. Pensado para SFX cortos
/// y reutilizables: pasos, pickups, puertas, etc.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Tooltip("Cantidad de AudioSource internos para poder solapar sonidos con distinto pitch al mismo tiempo")]
    [SerializeField] private int poolSize = 8;

    private AudioSource[] pool;
    private int nextSource = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            pool[i] = src;
        }
    }

    /// <summary>
    /// Reproduce un clip una sola vez, con pitch aleatorio entre pitchMin y pitchMax.
    /// Usa un pool de AudioSources en vez de uno solo para que dos sonidos con pitch
    /// distinto puedan sonar solapados sin pisarse el pitch entre sí.
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null) return;

        AudioSource src = pool[nextSource];
        nextSource = (nextSource + 1) % pool.Length;

        src.pitch = Random.Range(pitchMin, pitchMax);
        src.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Elige un clip al azar de un array, evitando repetir el mismo índice dos veces seguidas.
    /// Pasar por ref una variable propia de quien llama, para recordar el último elegido.
    /// </summary>
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