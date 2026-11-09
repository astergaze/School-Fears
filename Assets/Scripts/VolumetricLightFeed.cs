using UnityEngine;

/// <summary>
/// Empuja los datos de esta Spot Light como propiedades GLOBALES de shader.
/// Limpia automáticamente la intensidad al desactivarse o cambiar de escena para evitar luces fantasma.
/// </summary>
[RequireComponent(typeof(Light))]
public class VolumetricLightFeed : MonoBehaviour
{
    private static readonly int PosID = Shader.PropertyToID("_VLightPositionWS");
    private static readonly int DirID = Shader.PropertyToID("_VLightDirectionWS");
    private static readonly int RangeID = Shader.PropertyToID("_VLightRange");
    private static readonly int CosHalfAngleID = Shader.PropertyToID("_VLightCosHalfAngle");
    private static readonly int ColorID = Shader.PropertyToID("_VLightColor");
    private static readonly int IntensityID = Shader.PropertyToID("_VLightIntensity");

    private Light lightComponent;

    private void Awake()
    {
        lightComponent = GetComponent<Light>();
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
    }

    private void DisableGlobalLight()
    {
        Shader.SetGlobalFloat(IntensityID, 0f);
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