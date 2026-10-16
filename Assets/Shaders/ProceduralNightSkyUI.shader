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
 float3 sky=lerp(float3(0.003,0.006,0.018),float3(0.012,0.025,0.060),saturate(uv.y*0.9));

 // A gently arcing, diagonal Milky Way with broad clouds and fine mottling.
 float bandCenter=0.69-0.18*uv.x+0.035*sin(uv.x*6.0+seed*0.0017);
 float bandDistance=uv.y-bandCenter;
 float band=exp(-bandDistance*bandDistance*82.0);
 float cloudLarge=valueNoise(uv*float2(5.5,8.0)+seed*0.003);
 float cloudMedium=valueNoise(uv*float2(15.0,23.0)-seed*0.005);
 float cloudFine=valueNoise(uv*float2(39.0,54.0)+seed*0.009);
 float clouds=saturate(cloudLarge*0.55+cloudMedium*0.34+cloudFine*0.20-0.16);
 float core=exp(-bandDistance*bandDistance*245.0)*smoothstep(0.22,0.78,cloudMedium);
 sky+=band*clouds*float3(0.105,0.125,0.235);
 sky+=core*float3(0.055,0.072,0.145);
 // Break the band into visible wisps instead of a smooth purple stripe.
 sky*=1.0-band*0.10;
 sky+=band*cloudFine*float3(0.028,0.032,0.070);

 // Fine, pin-point stars. A small subset are brighter, with a restrained halo.
 float2 grid=uv*float2(132.0,96.0);
 float2 cell=floor(grid);
 float2 local=frac(grid)-0.5;
 float rnd=hash21(cell+seed*13.17);
 float2 jitter=float2(hash21(cell+seed*7.31),hash21(cell+seed*3.7))-0.5;
 float2 delta=local-jitter*0.34;
 float radius=lerp(0.045,0.135,hash21(cell+seed*5.1));
 float present=step(0.958,rnd);
 float star=(1.0-smoothstep(radius,radius+0.035,length(delta)))*present;
 float bright=step(0.994,rnd);
 float halo=exp(-dot(delta,delta)*210.0)*bright*0.13;
 float3 starColor=lerp(float3(0.53,0.68,1.0),float3(1.0,0.91,0.78),hash21(cell+seed*9.2));
 sky+=star*(0.42+bright*1.5)*starColor+halo*starColor;

 // Keep the moon at the foreground artwork's reflection point.
 float aspect=_MainTex_TexelSize.z/_MainTex_TexelSize.w;
 float2 md=(uv-_MoonCenter.xy)*float2(aspect,1.0);
 float moon=1.0-smoothstep(0.037,0.039,length(md));
 float glow=exp(-dot(md,md)*850.0);
 float craters=valueNoise(md*190.0+13.0);
 sky+=glow*float3(0.075,0.12,0.22);
 sky=lerp(sky,float3(0.86,0.90,1.0)*(0.86+0.14*craters),moon);
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