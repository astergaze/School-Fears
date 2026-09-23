Shader "Custom/VolumetricFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _FogDensity ("Fog Density", Range(0.01, 1.0)) = 0.35
        _FogStart ("Fog Start Distance", Float) = 0.5
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

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.uv);
                float depth = SampleSceneDepth(input.uv);
                
                // Fondo / cielo infinito completamente negro
                #if UNITY_REVERSED_Z
                if (depth <= 0.00001) return _FogColor;
                #else
                if (depth >= 0.99999) return _FogColor;
                #endif

                float3 worldPos = ComputeWorldSpacePosition(input.uv, depth, UNITY_MATRIX_I_VP);
                float dist = length(worldPos - _WorldSpaceCameraPos);

                // Distancia efectiva a partir del umbral del jugador
                float d = max(0.0, dist - _FogStart);

                // Curva Exponential Squared nativa de Unity: e^(-(d * density)^2)
                float factor = d * _FogDensity;
                float transmittance = exp(-factor * factor);
                float fogFactor = 1.0 - saturate(transmittance);

                // Fundido hacia la oscuridad absoluta preservando emisiones HDR
                float3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogFactor);

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}