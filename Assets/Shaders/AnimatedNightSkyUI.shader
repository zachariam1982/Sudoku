Shader "Sudoku/Animated Night Sky UI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SkyScrollSpeed ("Sky Scroll Speed", Range(0, 0.001)) = 0.00018
        _SkyArcHeight ("Sky Arc Height", Range(0, 0.2)) = 0.08
        _SkyGridTop ("Grid Top In Background UV", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _SkyScrollSpeed;
            float _SkyArcHeight;
            float _SkyGridTop;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float aspect = _MainTex_TexelSize.z / max(_MainTex_TexelSize.w, 1.0);
                float isLandscape = step(1.0, aspect);
                float skyBottom = lerp(0.27, 0.52, isLandscape);
                float skyMask = smoothstep(skyBottom, skyBottom + 0.08, input.texcoord.y);

                float2 movingUV = input.texcoord;
                float scroll = frac(_Time.y * _SkyScrollSpeed);
                movingUV.x = frac(movingUV.x + scroll);

                // The moon is baked into each background. Translate the whole
                // upper sky rigidly so its disc stays round along the parabola.
                float moonSourceX = lerp(0.412, 0.861, isLandscape);
                float moonX = frac(moonSourceX - scroll);
                float arc = 4.0 * moonX * (1.0 - moonX);
                // Conservative lower edge of the moon/glow in the source art.
                float moonFloor = lerp(0.81, 0.68, isLandscape);
                float clearanceLift = max(0.0, _SkyGridTop + 0.025 - moonFloor);
                float rise = clearanceLift + _SkyArcHeight * arc;
                // Fade the displacement in above the horizon. Never wrap Y:
                // the moon must not reappear at the bottom of the texture.
                movingUV.y = max(skyBottom + 0.08,
                    input.texcoord.y - rise * skyMask);

                fixed4 stillColor = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;
                fixed4 movingColor = tex2D(_MainTex, movingUV) + _TextureSampleAdd;
                fixed4 color = lerp(stillColor, movingColor, skyMask) * input.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
