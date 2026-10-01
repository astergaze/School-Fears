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

            // Ray march simple cámara -> worldPos, acumulando scattering de la spot light.
            // No consulta shadow maps (no hay oclusión real todavía): si el haz pasa "a través"
            // de una pared visualmente notorio, se puede agregar sampleo de sombra más adelante.
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

                    accum += rangeAtten * spotAtten * phase * stepLen;
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

                // Distancia efectiva a partir del umbral del jugador
                float d = max(0.0, dist - _FogStart);

                // Curva Exponential Squared nativa de Unity: e^(-(d * density)^2)
                float factor = d * _FogDensity;
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
