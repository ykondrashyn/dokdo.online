Shader "FX/Water"
{
    Properties
    {
        _WaveScale ("Wave scale", Range(0.02, 0.15)) = 0.063
        _ReflDistort ("Reflection distort", Range(0, 1.5)) = 0.44
        _RefrDistort ("Refraction distort", Range(0, 1.5)) = 0.4
        _RefrColor ("Refraction color", Color) = (0.34,0.85,0.92,1)
        [NoScaleOffset] _Fresnel ("Fresnel (A)", 2D) = "gray" {}
        [NoScaleOffset] _BumpMap ("Normal map", 2D) = "bump" {}
        WaveSpeed ("Wave speed (map1 x,y; map2 x,y)", Vector) =
            (19,9,-16,-7)
        [NoScaleOffset] _ReflectiveColor (
            "Reflective color (RGB) fresnel (A)", 2D) = "white" {}
        _HorizonColor ("Simple water horizon color", Color) =
            (0.172,0.463,0.435,1)
        [NoScaleOffset] _FoamTex ("Foam", 2D) = "black" {}
        _FoamColor ("Foam color", Color) = (0.72,0.88,0.95,1)
        _FoamScale ("Foam scale", Range(0.001, 0.2)) = 0.018
        _FoamStrength ("Foam strength", Range(0, 1)) = 0.22
        _WaterAlpha ("Water transparency", Range(0.5, 1)) = 0.94
        [HideInInspector] _WaveOffset ("Wave offset", Vector) = (0,0,0,0)
        [HideInInspector] _WaveScale4 ("Wave scale 4", Vector) =
            (0.063,0.063,0.0252,0.02835)
        [HideInInspector] _ReflectionTex (
            "Internal Reflection", 2D) = "black" {}
        [HideInInspector] _RefractionTex (
            "Internal Refraction", 2D) = "black" {}
    }

    SubShader
    {
        LOD 300
        Tags
        {
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "WATERMODE" = "Reflective"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile WATER_SIMPLE WATER_REFLECTIVE
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _BumpMap;
            sampler2D _Fresnel;
            sampler2D _ReflectiveColor;
            sampler2D _FoamTex;
            sampler2D _ReflectionTex;
            fixed4 _RefrColor;
            fixed4 _HorizonColor;
            fixed4 _FoamColor;
            float4 _WaveOffset;
            float4 _WaveScale4;
            float _ReflDistort;
            float _FoamScale;
            float _FoamStrength;
            float _WaterAlpha;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPosition : TEXCOORD0;
                float4 bumpUv : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
                float3 worldNormal : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.screenPosition = ComputeScreenPos(output.vertex);
                output.bumpUv.xy = input.texcoord * _WaveScale4.xy +
                    _WaveOffset.xy;
                output.bumpUv.zw = input.texcoord * _WaveScale4.zw +
                    _WaveOffset.zw;
                output.worldPosition = mul(unity_ObjectToWorld, input.vertex).xyz;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed3 firstNormal = UnpackNormal(
                    tex2D(_BumpMap, input.bumpUv.xy));
                fixed3 secondNormal = UnpackNormal(
                    tex2D(_BumpMap, input.bumpUv.zw));
                fixed3 tangentNormal = normalize(firstNormal + secondNormal);

                fixed3 surfaceNormal = normalize(input.worldNormal +
                    fixed3(tangentNormal.x, 0, tangentNormal.y) * 0.38);
                fixed3 viewDirection = normalize(
                    _WorldSpaceCameraPos.xyz - input.worldPosition);
                fixed facing = saturate(dot(surfaceNormal, viewDirection));

                fixed4 fresnelRamp = tex2D(
                    _Fresnel, fixed2(facing, 0.5));
                fixed4 gradient = tex2D(
                    _ReflectiveColor, fixed2(facing, 0.5));
                fixed fresnel = saturate(max(
                    fresnelRamp.a, gradient.a));

                fixed3 waterColor = lerp(_RefrColor.rgb, gradient.rgb,
                    saturate(gradient.a + fresnel * 0.7));
                waterColor = lerp(waterColor, _HorizonColor.rgb,
                    (1.0 - facing) * 0.45);

                #if defined(WATER_REFLECTIVE)
                float4 reflectionUv = input.screenPosition;
                reflectionUv.xy += tangentNormal.xy * _ReflDistort *
                    0.025 * input.screenPosition.w;
                fixed3 reflection = tex2Dproj(
                    _ReflectionTex, UNITY_PROJ_COORD(reflectionUv)).rgb;
                waterColor = lerp(waterColor, reflection,
                    saturate(fresnel * 0.75));
                #endif

                float2 foamUv = input.worldPosition.xz * _FoamScale +
                    _WaveOffset.xy * 0.35;
                fixed4 foam = tex2D(_FoamTex, foamUv);
                fixed foamAmount = saturate(
                    max(foam.a, dot(foam.rgb, fixed3(0.25, 0.5, 0.25))) *
                    _FoamStrength);
                waterColor = lerp(waterColor, _FoamColor.rgb, foamAmount);

                fixed alpha = saturate(_WaterAlpha +
                    foamAmount * _FoamColor.a * 0.06);
                fixed4 color = fixed4(waterColor, alpha);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
