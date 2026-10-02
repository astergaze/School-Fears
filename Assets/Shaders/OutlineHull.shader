Shader "Custom/OutlineHull"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width (offset a lo largo de la normal, unidades locales)", Float) = 0.02
        _EmissionIntensity ("Emission Intensity", Float) = 2.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+1" }

        Pass
        {
            Name "OutlineHull"
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _OutlineWidth;
                float _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Offset por vértice a lo largo de su normal, en espacio objeto.
                // No depende del pivote del GameObject ni de su jerarquía - funciona
                // igual de bien para una nota plana que para un cilindro asimétrico
                // como la linterna.
                float3 offsetPositionOS = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(offsetPositionOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_BaseColor.rgb * _EmissionIntensity, 1.0);
            }
            ENDHLSL
        }
    }
}
