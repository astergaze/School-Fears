using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Linterna en primera persona para School Fears.
/// Va en el Player (persistente). Al "usar" el ItemData de la linterna desde el inventario
/// se prende/apaga una SpotLight que sigue la cámara con un pequeño retraso, y a la vez
/// se reduce la densidad y se aleja el inicio de la niebla (M_VolumetricFog) para que el
/// jugador vea más lejos. Al apagarla, la niebla vuelve a sus valores originales.
///
/// No modifica el shader ni el asset del material: solo cambia _FogDensity y _FogStart
/// en runtime y los restaura al apagar / al salir.
/// </summary>
[DefaultExecutionOrder(100)] // después de Cinemachine, para que la luz no vaya un frame atrás
public class FlashlightController : MonoBehaviour
{
    public static FlashlightController Instance { get; private set; }

    [Header("Item")]
    [Tooltip("El flashlight.asset. Solo este ítem prende/apaga la linterna al usarse.")]
    [SerializeField] private ItemData flashlightItem;
    [Tooltip("Después de usar el ítem una vez, permite prender/apagar con una tecla.")]
    [SerializeField] private bool allowHotkey = true;
    [SerializeField] private Key hotkey = Key.F;

    [Header("Luz")]
    [SerializeField] private float intensity = 30f;
    [SerializeField] private float range = 28f;
    [SerializeField, Range(10f, 120f)] private float spotAngle = 55f;
    [SerializeField, Range(0f, 100f)] private float innerSpotAngle = 25f;
    [SerializeField] private Color color = new Color(1f, 0.96f, 0.85f);
    [SerializeField] private LightShadows shadows = LightShadows.Soft;
    [Tooltip("Posición de la linterna respecto a la cámara (derecha, abajo, adelante).")]
    [SerializeField] private Vector3 localOffset = new Vector3(0.18f, -0.14f, 0.25f);
    [Tooltip("Qué tan rápido alcanza la luz a la cámara. Menor = más 'peso' al girar.")]
    [SerializeField] private float followSpeed = 14f;
    [Tooltip("Opcional: modelo 3D de la linterna que se muestra en mano mientras está prendida.")]
    [SerializeField] private GameObject viewModelPrefab;

    [Header("Niebla")]
    [Tooltip("Arrastrá acá M_VolumetricFog (el mismo asset que usa el Render Feature).")]
    [SerializeField] private Material fogMaterial;
    [Tooltip("Valores originales del material (los de M_VolumetricFog). Se restauran al apagar.")]
    [SerializeField] private float baseDensity = 0.4f;
    [SerializeField] private float baseStart = 1f;
    [Tooltip("Valores con la linterna prendida. Menos densidad y más start = se ve más lejos.")]
    [SerializeField] private float flashlightDensity = 0.16f;
    [SerializeField] private float flashlightStart = 3.5f;
    [SerializeField] private float fadeTime = 0.35f;

    [Header("Feedback")]
    [SerializeField] private AudioClip toggleClip;
    [SerializeField, Range(0f, 1f)] private float toggleVolume = 0.8f;
    public UnityEvent<bool> onToggled;

    public bool IsOn { get; private set; }
    public bool Unlocked { get; private set; }

    private static readonly int FogDensityId = Shader.PropertyToID("_FogDensity");
    private static readonly int FogStartId = Shader.PropertyToID("_FogStart");

    private Transform rig;
    private Light spot;
    private GameObject viewModel;
    private Camera cam;
    private float blend;          // 0 = apagada, 1 = prendida (para el fundido)
    private bool snapRotation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        BuildRig();
    }

    private void BuildRig()
    {
        rig = new GameObject("FlashlightRig").transform;
        rig.SetParent(transform, false);

        var lightGO = new GameObject("FlashlightSpot");
        lightGO.transform.SetParent(rig, false);
        spot = lightGO.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = color;
        spot.range = range;
        spot.spotAngle = spotAngle;
        spot.innerSpotAngle = innerSpotAngle;
        spot.intensity = 0f;
        spot.shadows = shadows;
        spot.enabled = false;

        if (viewModelPrefab != null)
        {
            viewModel = Instantiate(viewModelPrefab, rig);
            viewModel.SetActive(false);
        }
    }

    /// <summary>
    /// Llamar desde el inventario cuando el jugador "usa" un ítem.
    /// Devuelve true si el ítem era la linterna (y por lo tanto ya fue manejado).
    /// </summary>
    public bool TryUse(ItemData item)
    {
        if (item == null || item != flashlightItem) return false;
        Unlocked = true;
        Toggle();
        return true;
    }

    public void Toggle() => SetOn(!IsOn);

    public void SetOn(bool on)
    {
        if (IsOn == on) return;
        IsOn = on;

        if (on)
        {
            snapRotation = true; // que aparezca ya apuntando a donde mira el jugador
            spot.enabled = true;
            if (viewModel != null) viewModel.SetActive(true);
        }

        if (toggleClip != null && cam != null)
            AudioSource.PlayClipAtPoint(toggleClip, cam.transform.position, toggleVolume);

        onToggled?.Invoke(on);
    }

    private void Update()
    {
        if (allowHotkey && Unlocked && Keyboard.current != null
            && Keyboard.current[hotkey].wasPressedThisFrame
            && Time.timeScale > 0f) // no togglear con inventario/pausa abiertos
        {
            Toggle();
        }

        // Fundido de luz + niebla (unscaled: se ve el cambio aunque el inventario pause el juego)
        float target = IsOn ? 1f : 0f;
        if (!Mathf.Approximately(blend, target))
        {
            blend = Mathf.MoveTowards(blend, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            float s = Mathf.SmoothStep(0f, 1f, blend);

            spot.intensity = intensity * s;
            ApplyFog(s);

            if (blend <= 0f)
            {
                spot.enabled = false;
                if (viewModel != null) viewModel.SetActive(false);
            }
        }
    }

    private void LateUpdate()
    {
        if (blend <= 0f && !IsOn) return;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Transform c = cam.transform;
        rig.position = c.TransformPoint(localOffset);

        if (snapRotation)
        {
            rig.rotation = c.rotation;
            snapRotation = false;
        }
        else
        {
            float k = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);
            rig.rotation = Quaternion.Slerp(rig.rotation, c.rotation, k);
        }
    }

    private void ApplyFog(float s)
    {
        if (fogMaterial == null) return;
        fogMaterial.SetFloat(FogDensityId, Mathf.Lerp(baseDensity, flashlightDensity, s));
        fogMaterial.SetFloat(FogStartId, Mathf.Lerp(baseStart, flashlightStart, s));
    }

    private void RestoreFog() => ApplyFog(0f);

    // Importante: como el material es un asset, en el Editor los cambios quedan guardados.
    // Restauramos siempre que el script se apague/destruya (incluye salir del Play Mode).
    private void OnDisable() => RestoreFog();

    private void OnDestroy()
    {
        if (Instance == this)
        {
            RestoreFog();
            Instance = null;
        }
    }
}