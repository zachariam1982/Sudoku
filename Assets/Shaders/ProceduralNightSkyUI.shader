Shader "UI/ProceduralNightSky" {
Properties {
[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
_Color ("Tint", Color) = (1,1,1,1)
_SkySeed ("Sky Seed", Float) = 1
_MoonCenter ("Moon Center", Vector) = (0.86,0.78,0,0)
_StencilComp ("Stencil Comparison", Float) = 8
_Stencil ("Stencil ID", Float) = 0
_StencilOp ("Stencil Operation", Float) = 0
_StencilWriteMask ("Stencil Write Mask", Float) = 255
_StencilReadMask ("Stencil Read Mask", Float) = 255
_ColorMask ("Color Mask", Float) = 15
[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
}
SubShader {
Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 2.0
#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
#pragma multi_compile_local _ UNITY_UI_ALPHACLIP
#include "UnityCG.cginc"
#include "UnityUI.cginc"
sampler2D _MainTex;
float4 _MainTex_ST, _MainTex_TexelSize, _ClipRect, _MoonCenter;
fixed4 _Color;
float _SkySeed;
struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 worldPosition:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
v2f vert(appdata v) {
 v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
 o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.texcoord,_MainTex); o.color=v.color*_Color; return o;
}
float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float valueNoise(float2 p) {
 float2 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
 return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+float2(1,1)),f.x),f.y);
}
float3 nightSky(float2 uv) {
 float seed=_SkySeed;
 float3 sky=lerp(float3(0.006,0.012,0.035),float3(0.025,0.045,0.105),saturate(uv.y*0.85));
 float bandCenter=0.66+0.11*sin(uv.x*4.2+seed*0.017);
 float band=exp(-pow((uv.y-bandCenter)*5.5,2.0));
 float nebula=valueNoise(uv*float2(9,7)+seed*0.03)*0.55+valueNoise(uv*float2(23,18)-seed*0.02)*0.3;
 sky+=band*nebula*float3(0.28,0.20,0.48)+band*band*float3(0.055,0.065,0.18);
 float2 cell=floor(uv*float2(145,105));
 float2 local=frac(uv*float2(145,105))-0.5;
 float rnd=hash21(cell+seed*13.17);
 float radius=lerp(0.08,0.24,hash21(cell+seed*7.31));
 float star=(1.0-smoothstep(radius,radius+0.06,length(local)))*step(0.93,rnd);
 sky+=star*(0.35+step(0.985,rnd)*3.2)*lerp(float3(0.45,0.62,1),float3(1,0.87,0.68),hash21(cell+seed*3.7));
 float aspect=_MainTex_TexelSize.z/_MainTex_TexelSize.w;
 float2 md=(uv-_MoonCenter.xy)*float2(aspect,1);
 float moon=1.0-smoothstep(0.037,0.039,length(md));
 float glow=exp(-dot(md,md)*850.0);
 float craters=valueNoise(md*190.0+13.0);
 sky+=glow*float3(0.11,0.16,0.28);
 sky=lerp(sky,float3(0.84,0.88,0.98)*(0.84+0.16*craters),moon);
 return sky;
}
fixed4 frag(v2f i):SV_Target {
 fixed4 fg=tex2D(_MainTex,i.uv)*i.color;
 float3 rgb=lerp(nightSky(i.uv),fg.rgb,fg.a);
 #ifdef UNITY_UI_CLIP_RECT
 clip(UnityGet2DClipping(i.worldPosition.xy,_ClipRect)-0.001);
 #endif
 fixed4 result=fixed4(rgb,1);
 #ifdef UNITY_UI_ALPHACLIP
 clip(result.a-0.001);
 #endif
 return result;
}
ENDCG
}
}
}