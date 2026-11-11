using UnityEngine;

/// <summary>
/// Empuja los datos de esta Spot Light como propiedades GLOBALES de shader.
/// Limpia automáticamente la intensidad al desactivarse o cambiar de escena para evitar luces fantasma.
///
/// Además arma un "mapa de oclusión" con raycasts: para una grilla de direcciones dentro del cono
/// guarda la distancia hasta el primer obstáculo (pared, mostrador, etc.). El shader lo usa para
/// cortar el haz volumétrico donde hay geometría. Los obstáculos necesitan Collider (no triggers).
/// </summary>
[RequireComponent(typeof(Light))]
public class VolumetricLightFeed : MonoBehaviour
{
    // Deben coincidir con VOCC_AZ / VOCC_POL en VolumetricFog.shader
    public const int OccAz = 24;
    public const int OccPol = 6;

    private static readonly int PosID = Shader.PropertyToID("_VLightPositionWS");
    private static readonly int DirID = Shader.PropertyToID("_VLightDirectionWS");
    private static readonly int RangeID = Shader.PropertyToID("_VLightRange");
    private static readonly int CosHalfAngleID = Shader.PropertyToID("_VLightCosHalfAngle");
    private static readonly int ColorID = Shader.PropertyToID("_VLightColor");
    private static readonly int IntensityID = Shader.PropertyToID("_VLightIntensity");

    private static readonly int RightID = Shader.PropertyToID("_VLightRightWS");
    private static readonly int UpID = Shader.PropertyToID("_VLightUpWS");
    private static readonly int OccID = Shader.PropertyToID("_VLightOcc");
    private static readonly int OccMaxAngleID = Shader.PropertyToID("_VLightOccMaxAngle");
    private static readonly int OccEnabledID = Shader.PropertyToID("_VLightOccEnabled");

    [Header("Oclusión del haz")]
    [Tooltip("Si está apagado, el haz atraviesa todo (comportamiento anterior)")]
    [SerializeField] private bool occlusion = true;
    [Tooltip("Capas que bloquean el haz. Por defecto todas (los triggers se ignoran y los colliders propios del Rector también)")]
    [SerializeField] private LayerMask occluders = ~0;
    [Tooltip("Dibuja en la Scene view los rayos que usa la oclusión (solo para depurar)")]
    [SerializeField] private bool debugRays = false;

    private Light lightComponent;
    private Transform ownRoot;
    private readonly Vector4[] occData = new Vector4[OccAz * OccPol / 4];
    private readonly RaycastHit[] hitBuf = new RaycastHit[16];

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
        ownRoot = transform.root;
    }

    private void LateUpdate()
    {
        if (lightComponent == null || !lightComponent.enabled)
        {
            DisableGlobalLight();
            return;
        }

        Shader.SetGlobalVector(PosID, transform.position);
        Shader.SetGlobalVector(DirID, transform.forward);
        Shader.SetGlobalFloat(RangeID, lightComponent.range);
        Shader.SetGlobalFloat(CosHalfAngleID, Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad));
        Shader.SetGlobalColor(ColorID, lightComponent.color);
        Shader.SetGlobalFloat(IntensityID, lightComponent.intensity);

        if (occlusion)
        {
            UpdateOcclusion();
        }
        else
        {
            Shader.SetGlobalFloat(OccEnabledID, 0f);
        }
    }

    private void UpdateOcclusion()
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        float range = lightComponent.range;
        float maxAngle = lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad;

        for (int i = 0; i < OccPol; i++)
        {
            float theta = (OccPol > 1 ? i / (float)(OccPol - 1) : 0f) * maxAngle;
            float sinT = Mathf.Sin(theta);
            float cosT = Mathf.Cos(theta);

            for (int j = 0; j < OccAz; j++)
            {
                // Mismo mapeo que el shader: u = phi / (2π) + 0.5  ->  phi = (j / AZ - 0.5) * 2π
                float phi = ((j / (float)OccAz) - 0.5f) * 2f * Mathf.PI;
                Vector3 dir = fwd * cosT + (right * Mathf.Cos(phi) + up * Mathf.Sin(phi)) * sinT;

                float d = range + 1f; // sin obstáculo: más lejos que el alcance de la luz
                int n = Physics.RaycastNonAlloc(pos, dir, hitBuf, range, occluders, QueryTriggerInteraction.Ignore);
                for (int k = 0; k < n; k++)
                {
                    if (hitBuf[k].collider.transform.root == ownRoot) continue; // ignorar al propio Rector
                    if (hitBuf[k].distance < d) d = hitBuf[k].distance;
                }

                int idx = i * OccAz + j;
                occData[idx >> 2][idx & 3] = d;

                if (debugRays)
                    Debug.DrawRay(pos, dir * Mathf.Min(d, range), Color.yellow);
            }
        }

        Shader.SetGlobalVector(RightID, right);
        Shader.SetGlobalVector(UpID, up);
        Shader.SetGlobalVectorArray(OccID, occData);
        Shader.SetGlobalFloat(OccMaxAngleID, maxAngle);
        Shader.SetGlobalFloat(OccEnabledID, 1f);
    }

    private void DisableGlobalLight()
    {
        Shader.SetGlobalFloat(IntensityID, 0f);
        Shader.SetGlobalFloat(OccEnabledID, 0f);
    }

    private void OnDisable()
    {
        DisableGlobalLight();
    }

    private void OnDestroy()
    {
        DisableGlobalLight();
    }
}