using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Linterna en primera persona para School Fears.
/// Va en el Player (persistente). Al "usar" el ItemData de la linterna desde el inventario
/// se prende/apaga una SpotLight que sigue la cámara con un pequeño retraso, y a la vez
/// se le pasa al shader VolumetricFog (como globales) la posición, dirección y ángulo del cono,
/// y dentro de ese cono la niebla es menos densa y empieza más lejos.
///
/// Ya NO toca el material M_VolumetricFog: todo viaja por Shader.SetGlobal*, así que no hay
/// nada que restaurar en el asset y no puede quedar "pegado" si el editor se cuelga.
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

    [Header("Niebla (solo dentro del cono)")]
    [Tooltip("Densidad de la niebla dentro del cono. Menor = se ve más lejos. (El material usa 0.4)")]
    [SerializeField] private float flashlightDensity = 0.12f;
    [Tooltip("Distancia desde la cámara donde empieza la niebla dentro del cono. (El material usa 1)")]
    [SerializeField] private float flashlightStart = 4f;
    [Tooltip("Multiplica el ángulo del cono de la luz para el efecto de niebla (1 = igual que la luz).")]
    [SerializeField, Range(0.5f, 1.5f)] private float fogAngleScale = 1f;
    [SerializeField] private float fadeTime = 0.35f;

    [Header("Feedback")]
    [SerializeField] private AudioClip toggleClip;
    [SerializeField, Range(0f, 1f)] private float toggleVolume = 0.8f;
    public UnityEvent<bool> onToggled;

    public bool IsOn { get; private set; }
    public bool Unlocked { get; private set; }

    private static readonly int FlashPosId = Shader.PropertyToID("_FlashPositionWS");
    private static readonly int FlashDirId = Shader.PropertyToID("_FlashDirectionWS");
    private static readonly int FlashRangeId = Shader.PropertyToID("_FlashRange");
    private static readonly int FlashCosHalfId = Shader.PropertyToID("_FlashCosHalfAngle");
    private static readonly int FlashCosInnerId = Shader.PropertyToID("_FlashCosInnerAngle");
    private static readonly int FlashStrengthId = Shader.PropertyToID("_FlashStrength");
    private static readonly int FlashFogDensityId = Shader.PropertyToID("_FlashFogDensity");
    private static readonly int FlashFogStartId = Shader.PropertyToID("_FlashFogStart");

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
    public void RestoreState(bool unlocked, bool on)
    {
        Unlocked = unlocked;
        SetOn(on);
    }

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

            if (blend <= 0f)
            {
                Shader.SetGlobalFloat(FlashStrengthId, 0f);
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

        PushFogGlobals();
    }

    private void PushFogGlobals()
    {
        float s = Mathf.SmoothStep(0f, 1f, blend);

        // Medio ángulo del cono para la niebla; el interior (borde duro) siempre menor que el exterior
        float half = Mathf.Clamp(spotAngle * 0.5f * fogAngleScale, 1f, 89f);
        float inner = Mathf.Min(innerSpotAngle * 0.5f * fogAngleScale, half * 0.95f);

        Shader.SetGlobalVector(FlashPosId, rig.position);
        Shader.SetGlobalVector(FlashDirId, rig.forward);
        Shader.SetGlobalFloat(FlashRangeId, range);
        Shader.SetGlobalFloat(FlashCosHalfId, Mathf.Cos(half * Mathf.Deg2Rad));
        Shader.SetGlobalFloat(FlashCosInnerId, Mathf.Cos(inner * Mathf.Deg2Rad));
        Shader.SetGlobalFloat(FlashFogDensityId, flashlightDensity);
        Shader.SetGlobalFloat(FlashFogStartId, flashlightStart);
        Shader.SetGlobalFloat(FlashStrengthId, s);
    }

    private void DisableFogEffect() => Shader.SetGlobalFloat(FlashStrengthId, 0f);

    // Apagar siempre el efecto al desactivar/destruir (el global es estático y sobreviviría al Play Mode)
    private void OnDisable() => DisableFogEffect();

    private void OnDestroy()
    {
        if (Instance == this)
        {
            DisableFogEffect();
            Instance = null;
        }
    }
}