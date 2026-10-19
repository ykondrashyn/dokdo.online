Shader "Hidden/Cartoon FX/Particles"
{
    Properties
    {
        _BaseColor ("Alpha Blended Color", Color) = (0.5,0.5,0.5,0.5)
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _InvFade ("Soft Particles Factor", Range(0.01, 3)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BaseColor;
            fixed4 _TintColor;
            float _InvFade;
            sampler2D_float _CameraDepthTexture;

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                #ifdef SOFTPARTICLES_ON
                float4 projectedPosition : TEXCOORD1;
                #endif
                UNITY_FOG_COORDS(2)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _BaseColor * _TintColor;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                #ifdef SOFTPARTICLES_ON
                output.projectedPosition = ComputeScreenPos(output.vertex);
                COMPUTE_EYEDEPTH(output.projectedPosition.z);
                #endif
                UNITY_TRANSFER_FOG(output, output.vertex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                #ifdef SOFTPARTICLES_ON
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(
                    _CameraDepthTexture,
                    UNITY_PROJ_COORD(input.projectedPosition)));
                input.color.a *= saturate(_InvFade *
                    (sceneDepth - input.projectedPosition.z));
                #endif

                fixed4 color = 2.0 * input.color *
                    tex2D(_MainTex, input.texcoord);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
