Shader "Custom/VolumetricFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _FogDensity ("Fog Density", Range(0.01, 1.0)) = 0.35
        _FogStart ("Fog Start Distance", Float) = 0.5

        [Header(Volumetric Light)]
        _LightScatterColor ("Light Scatter Tint", Color) = (1,1,1,1)
        _LightScatterIntensity ("Light Scatter Intensity", Range(0, 30)) = 6
        _LightSteps ("Ray March Steps", Range(4, 64)) = 24
        _LightScatterG ("Scatter Anisotropy (g)", Range(-0.95, 0.95)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "VolumetricFogPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D_X(_BlitTexture);

            float4 _FogColor;
            float _FogDensity;
            float _FogStart;

            float4 _LightScatterColor;
            float _LightScatterIntensity;
            float _LightSteps;
            float _LightScatterG;

            // Alimentados desde código (VolumetricLightFeed.cs) como propiedades GLOBALES,
            // no de material: así no dependen de a qué luz apunta este material en particular.
            float3 _VLightPositionWS;
            float3 _VLightDirectionWS;
            float _VLightRange;
            float _VLightCosHalfAngle;
            float3 _VLightColor;
            float _VLightIntensity;

            // Mapa de oclusión del haz (VolumetricLightFeed.cs): distancia hasta el primer obstáculo
            // desde la luz, muestreada en una grilla polar (azimut x ángulo respecto al eje del cono).
            // VOCC_AZ / VOCC_POL deben coincidir con OccAz / OccPol en VolumetricLightFeed.cs.
            // _VLightOccEnabled = 0 (por defecto, o luz apagada) -> sin oclusión, comportamiento anterior.
            #define VOCC_AZ 24
            #define VOCC_POL 6
            float4 _VLightOcc[(VOCC_AZ * VOCC_POL) / 4];
            float3 _VLightRightWS;
            float3 _VLightUpWS;
            float _VLightOccMaxAngle;
            float _VLightOccEnabled;

            // Linterna del jugador (FlashlightController.cs, también globales).
            // _FlashStrength = 0 -> efecto desactivado, el resto de valores se ignora.
            float3 _FlashPositionWS;
            float3 _FlashDirectionWS;
            float _FlashRange;
            float _FlashCosHalfAngle;
            float _FlashCosInnerAngle;
            float _FlashStrength;
            float _FlashFogDensity;
            float _FlashFogStart;

            // Niebla extra global (StairsFogZone.cs). Si nadie la setea vale 0 -> no cambia nada.
            float _FogExtraDensity;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float InterleavedGradientNoise(float2 pixelCoord)
            {
                float3 magic = float3(0.06711056, 0.00583715, 52.9829189);
                return frac(magic.z * frac(dot(pixelCoord, magic.xy)));
            }

            float VOccAt(int az, int pol)
            {
                int idx = pol * VOCC_AZ + az;
                return _VLightOcc[idx >> 2][idx & 3];
            }

            // 1 = el punto ve la luz, 0 = hay un obstáculo entre la luz y el punto.
            // dirFromLight: dirección normalizada luz -> punto. dist: distancia luz -> punto.
            float VLightVisibility(float3 dirFromLight, float3 lightDirN, float dist)
            {
                float x = dot(dirFromLight, _VLightRightWS);
                float y = dot(dirFromLight, _VLightUpWS);
                float z = clamp(dot(dirFromLight, lightDirN), -1.0, 1.0);

                float theta = acos(z);
                float phi = atan2(y, x);

                // Azimut con wrap (interpola entre sectores vecinos)
                float u = (phi / 6.28318530 + 0.5) * VOCC_AZ;
                float uf = frac(u);
                int a0 = ((int)floor(u)) % VOCC_AZ;
                int a1 = (a0 + 1) % VOCC_AZ;

                // Ángulo polar (clamp: más allá del cono el spot ya vale 0)
                float v = saturate(theta / max(_VLightOccMaxAngle, 1e-4)) * (VOCC_POL - 1);
                int p0 = min((int)floor(v), VOCC_POL - 1);
                int p1 = min(p0 + 1, VOCC_POL - 1);
                float vf = v - p0;

                float d0 = lerp(VOccAt(a0, p0), VOccAt(a1, p0), uf);
                float d1 = lerp(VOccAt(a0, p1), VOccAt(a1, p1), uf);
                float occDist = lerp(d0, d1, vf);

                return 1.0 - smoothstep(occDist - 0.05, occDist + 0.25, dist);
            }

            // Ray march simple cámara -> worldPos, acumulando scattering de la spot light.
            // La oclusión (paredes, mostradores) viene de un mapa de distancias armado con
            // raycasts en VolumetricLightFeed.cs; no usa shadow maps de URP.
            float3 ComputeVolumetricScatter(float3 camPos, float3 worldPos, float noise)
            {
                float3 toEnd = worldPos - camPos;
                float rayLen = length(toEnd);
                float3 rayDir = toEnd / max(rayLen, 1e-5);

                int steps = (int)_LightSteps;
                float stepLen = rayLen / steps;
                float3 lightDirN = normalize(_VLightDirectionWS);

                float accum = 0.0;

                [loop]
                for (int i = 0; i < steps; i++)
                {
                    float t = (i + noise) * stepLen;
                    float3 samplePos = camPos + rayDir * t;

                    float3 toLight = _VLightPositionWS - samplePos;
                    float distToLight = length(toLight);
                    float3 dirToLight = toLight / max(distToLight, 1e-5);

                    // Atenuación por distancia (cuadrática, recortada al rango de la luz)
                    float rangeAtten = saturate(1.0 - distToLight / max(_VLightRange, 0.001));
                    rangeAtten *= rangeAtten;

                    // Atenuación del cono (spot)
                    float spotDot = dot(-dirToLight, lightDirN);
                    float spotAtten = saturate((spotDot - _VLightCosHalfAngle) / max(1.0 - _VLightCosHalfAngle, 0.001));
                    spotAtten *= spotAtten;

                    // Phase function (Henyey-Greenstein) - da el look de "rayo" forward-scattering
                    float cosTheta = dot(rayDir, dirToLight);
                    float g = _LightScatterG;
                    float phase = (1.0 - g * g) / (4.0 * 3.14159265 * pow(max(1.0 + g * g - 2.0 * g * cosTheta, 1e-4), 1.5));

                    // Oclusión: si hay un obstáculo entre la luz y este punto, no hay haz
                    float vis = 1.0;
                    if (_VLightOccEnabled > 0.5 && rangeAtten * spotAtten > 0.0)
                        vis = VLightVisibility(-dirToLight, lightDirN, distToLight);

                    accum += rangeAtten * spotAtten * vis * phase * stepLen;
                }

                return _VLightColor * _VLightIntensity * _LightScatterColor.rgb * _LightScatterIntensity * accum;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv);
                float depth = SampleSceneDepth(input.uv);

                float3 camPos = _WorldSpaceCameraPos;
                float3 worldPos = ComputeWorldSpacePosition(input.uv, depth, UNITY_MATRIX_I_VP);

                float noise = InterleavedGradientNoise(input.positionCS.xy);
                float3 scatter = ComputeVolumetricScatter(camPos, worldPos, noise);

                // Fondo / cielo infinito completamente negro (+ el haz si pasa por ahí)
                #if UNITY_REVERSED_Z
                if (depth <= 0.00001) return float4(_FogColor.rgb + scatter, 1.0);
                #else
                if (depth >= 0.99999) return float4(_FogColor.rgb + scatter, 1.0);
                #endif

                float dist = length(worldPos - camPos);

                // Máscara de la linterna: 1 dentro del cono (con borde suave y fade por rango), 0 fuera.
                float flash = 0.0;
                if (_FlashStrength > 0.0001)
                {
                    float3 toPix = worldPos - _FlashPositionWS;
                    float flashDist = length(toPix);
                    float3 fdir = _FlashDirectionWS / max(length(_FlashDirectionWS), 1e-5);
                    float cosA = dot(toPix / max(flashDist, 1e-5), fdir);

                    float cone = smoothstep(_FlashCosHalfAngle, _FlashCosInnerAngle, cosA);
                    float rangeFade = 1.0 - smoothstep(_FlashRange * 0.6, _FlashRange, flashDist);
                    flash = cone * rangeFade * _FlashStrength;
                }

                float fogDensity = lerp(_FogDensity, _FlashFogDensity, flash);
                float fogStart = lerp(_FogStart, _FlashFogStart, flash);

                // Zona de niebla densa (escalera del buffet): suma densidad y acerca el inicio de la niebla al jugador
                fogDensity += _FogExtraDensity;
                fogStart = lerp(fogStart, 0.0, saturate(_FogExtraDensity));

                // Distancia efectiva a partir del umbral del jugador
                float d = max(0.0, dist - fogStart);

                // Curva Exponential Squared nativa de Unity: e^(-(d * density)^2)
                float factor = d * fogDensity;
                float transmittance = exp(-factor * factor);
                float fogFactor = 1.0 - saturate(transmittance);

                // Fundido hacia la oscuridad absoluta preservando emisiones HDR
                float3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogFactor);
                finalColor += scatter;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
