// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Custom/PlaneOutline"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_ColorOverlay("ColorOverlay", Color) = (1, 1, 1, 1)
		[MaterialToggle] ToggleOverlay("ToggleOverlay", Float) = 0
		_MainTex("Albedo", 2D) = "white" {}

		_OutlineSize("Outline size", Range(0, 30.0)) = 1.0
		_OutlineColor("Outline color", Color) = (0, 0, 0, 1)
		_FadeSize("Fade size", Range(0, 1.0)) = 0
		_GlowDepth("Glow depth", Range(0, 1)) = 0
		_GlowSpeed("Glow speed", Float) = 0
		_Transparent("Transparent", Float) = 1.0

	    _Transparency("Transparency", Range(0.0,1.0)) = 0.25
		_CutoutThresh("Cutout Threshold", Range(0.0,1.0)) = 0.0
		_Distance("Distance", Float) = 1
		_Amplitude("Amplitude", Float) = 1
		_Speed("Speed", Float) = 1
		_Amount("Amount", Range(0.0,1.0)) = 1

        //_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        //_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        //_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        //[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

        //[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        //_MetallicGlossMap("Metallic", 2D) = "white" {}

        //[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        //[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        //_BumpScale("Scale", Float) = 1.0
        //_BumpMap("Normal Map", 2D) = "bump" {}

        //_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        //_ParallaxMap ("Height Map", 2D) = "black" {}

        //_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        //_OcclusionMap("Occlusion", 2D) = "white" {}

        //_EmissionColor("Color", Color) = (0,0,0)
        //_EmissionMap("Emission", 2D) = "white" {}

        //_DetailMask("Detail Mask", 2D) = "white" {}

        //_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        //_DetailNormalMapScale("Scale", Float) = 1.0
        //_DetailNormalMap("Normal Map", 2D) = "bump" {}

        //[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


        //// Blending state
        //[HideInInspector] _Mode ("__mode", Float) = 0.0
        //[HideInInspector] _SrcBlend ("__src", Float) = 1.0
        //[HideInInspector] _DstBlend ("__dst", Float) = 0.0
        //[HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

//	CGINCLUDE
//#define UNITY_SETUP_BRDF_INPUT MetallicSetup
//		ENDCG

			SubShader
		{
			Tags {"Queue" = "Transparent" "RenderType" = "Transparent" }
			LOD 100

			//ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D _MainTex;
				float4 _Color;
				float4 _ColorOverlay;
				float ToggleOverlay;
				float4 _MainTex_ST;
				//float4 _TintColor;
				float _Transparency;
				float _CutoutThresh;
				float _Distance;
				float _Amplitude;
				float _Speed;
				float _Amount;

				v2f vert(appdata v)
				{
					v2f o;
					//v.vertex.x += sin(_Time.y * _Speed + v.vertex.y * _Amplitude) * _Distance * _Amount;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					// sample the texture
					fixed4 col = tex2D(_MainTex, i.uv);
					col.a = _Transparency;
					col *= _Color;
					if (ToggleOverlay > 0)
						col *= _ColorOverlay;
					//clip(col.r - _CutoutThresh);
					return col;
				}
				ENDCG
			}
        

			Pass
			{
				ZWrite Off
				ZTest Always
				Blend SrcAlpha OneMinusSrcAlpha

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					float3 normal : NORMAL;
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					UNITY_VERTEX_OUTPUT_STEREO
				};

				//sampler2D _MainTex;
				float _OutlineSize;
				float4 _OutlineColor;
				float _FadeSize;
				float _GlowDepth;
				float _GlowSpeed;

				v2f vert(appdata v)
				{
					v2f o;
					if (_OutlineSize == 0)
					{
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.uv = v.uv;
						
						return o;
					}
					float scale = 0.02f;
					float expand = 1.0f + scale;
					//THIS IS MEH
					//Needs corners as well to be ideal
					if (v.normal.x > 0)
					{
						//v.vertex.yz *= expand;	//Opcja pierwotna
						//v.vertex.y += (v.uv.x - 0.5f) * scale;
						//v.vertex.z += (v.uv.y - 0.5f) * scale;
						if (v.uv.x == 0)
							v.vertex.y -= scale;
						else if (v.uv.x == 1)
							v.vertex.y += scale;
						if (v.uv.y == 0)
							v.vertex.z += scale;
						else if (v.uv.y == 1)
							v.vertex.z -= scale;
					}
					else if (v.normal.x < 0)
					{
						//v.vertex.yz *= expand;
						if (v.uv.x == 0)
							v.vertex.y += scale;
						else if (v.uv.x == 1)
							v.vertex.y -= scale;
						if (v.uv.y == 0)
							v.vertex.z += scale;
						else if (v.uv.y == 1)
							v.vertex.z -= scale;
						//v.vertex.y -= (v.uv.x - 0.5f) * scale;
						//v.vertex.z -= (v.uv.y - 0.5f) * scale;
					}
					else if (v.normal.y < 0)
					{
						//v.vertex.xz *= expand;
						if (v.uv.x == 0)
							v.vertex.x -= scale;
						else if (v.uv.x == 1)
							v.vertex.x += scale;
						if (v.uv.y == 0)
							v.vertex.z += scale;
						else if (v.uv.y == 1)
							v.vertex.z -= scale;
						//v.vertex.x += (v.uv.x - 0.5f) * scale;
						//v.vertex.z += (v.uv.y - 0.5f) * scale;
					}
					else if (v.normal.y > 0)
					{
						//v.vertex.xz *= expand;
						if (v.uv.x == 0)
							v.vertex.x += scale;
						else if (v.uv.x == 1)
							v.vertex.x -= scale;
						if (v.uv.y == 0)
							v.vertex.z += scale;
						else if (v.uv.y == 1)
							v.vertex.z -= scale;
						//v.vertex.x -= (v.uv.x - 0.5f) * scale;
						//v.vertex.z -= (v.uv.y - 0.5f) * scale;
					}
					else if (v.normal.z != 0)
					{
						//v.vertex.xy *= expand;
						if (v.uv.x == 0)
							v.vertex.x -= scale;
						else if (v.uv.x == 1)
							v.vertex.x += scale;
						if (v.uv.y == 0)
							v.vertex.y += scale;
						else if (v.uv.y == 1)
							v.vertex.y -= scale;
						//v.vertex.x += (v.uv.x - 0.5f) * scale;
						//v.vertex.y += (v.uv.y - 0.5f) * scale;
					}
					/*v.vertex.xy *= expand;*/
					o.vertex = UnityObjectToClipPos(v.vertex);
					//o.uv = (v.uv - 0.5f) * expand + 0.5f;
					//o.uv = v.uv;
					o.uv = v.uv;
					if (v.uv.x == 0)
						o.uv.x -= scale;
					else if (v.uv.x == 1)
						o.uv.x += scale;
					if (v.uv.y == 0)
						o.uv.y -= scale;
					else if (v.uv.y == 1)
						o.uv.y += scale;
					return o;
				}

				fixed4 frag(v2f i) : SV_Target
				{
					if (_OutlineSize == 0)
						return fixed4(0, 0, 0, 0);

					float2 fromCenter = abs(i.uv - 0.5f);
					float2 fromEdge = fromCenter - 0.5f;

					fromEdge.x /= length(float2(ddx(i.uv.x), ddy(i.uv.x)));
					fromEdge.y /= length(float2(ddx(i.uv.y), ddy(i.uv.y)));

					float distance = abs(min(max(fromEdge.x,fromEdge.y), 0.0f) + length(max(fromEdge, 0.0f)));

					fixed4 col = fixed4(0, 0, 0, 0);//tex2D(_MainTex, i.uv);
					float overflow = max(fromCenter.x, fromCenter.y);
					col.a *= step(overflow, 0.5f);
					if (_FadeSize == 0)
					{
						if (distance < _OutlineSize)
							col = _OutlineColor;
					}
					else
					{
						float fade = (-distance / (2 * _FadeSize * _OutlineSize)) + (1 + 1 / _FadeSize) / 2;
						col = lerp(col, _OutlineColor, saturate(fade));
					}
					if (col.a > 0)
						col.a += sin(_Time[1] * _GlowSpeed + 1) * _GlowDepth * 0.5f;

					return col;
				}
				ENDCG
			}

		}
		FallBack "VertexLit"
}
