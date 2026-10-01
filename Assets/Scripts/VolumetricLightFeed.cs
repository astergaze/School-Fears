using UnityEngine;

/// <summary>
/// Empuja los datos de esta Spot Light como propiedades GLOBALES de shader,
/// para que el pase fullscreen VolumetricFog pueda calcular el haz volumétrico
/// sin depender de índices de luces de URP (funciona igual con Forward o Forward+).
/// Colocar este componente en el mismo GameObject que la Spot Light (spotLightTransform).
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
        if (lightComponent == null) return;

        Shader.SetGlobalVector(PosID, transform.position);
        Shader.SetGlobalVector(DirID, transform.forward);
        Shader.SetGlobalFloat(RangeID, lightComponent.range);
        Shader.SetGlobalFloat(CosHalfAngleID, Mathf.Cos(lightComponent.spotAngle * 0.5f * Mathf.Deg2Rad));
        Shader.SetGlobalColor(ColorID, lightComponent.color);
        // OJO: si el proyecto usa "Use Physical Light Units", este valor puede ser grande
        // (lúmenes) y hay que compensar bajando _LightScatterIntensity en el material.
        Shader.SetGlobalFloat(IntensityID, lightComponent.intensity);
    }
}