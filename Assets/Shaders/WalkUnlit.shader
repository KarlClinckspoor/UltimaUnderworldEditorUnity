// The walk-around view's surfaces (Extensions/View/WalkView.cs): a texture times the vertex
// colour and a tint, both sides drawn, transparent texels cut out, depth written, darkened
// with distance between the globals _WalkFadeStart and _WalkFadeEnd when _WalkDark is 1 (done
// here rather than with Unity's fog, whose variants a build may strip). Its material is
// Resources/WalkUnlit.mat, which keeps it in builds.
Shader "UltimateEditor/WalkUnlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	SubShader
	{
		Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "IgnoreProjector" = "True" }
		Cull Off
		ZWrite On
		ZTest LEqual
		Lighting Off
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
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
				float light : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			fixed _Cutoff;
			float _WalkFadeStart;
			float _WalkFadeEnd;
			float _WalkDark;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color * _Color;
				float dist = length(UnityObjectToViewPos(v.vertex));
				float fade = saturate((_WalkFadeEnd - dist) / max(0.001, _WalkFadeEnd - _WalkFadeStart));
				o.light = lerp(1.0, fade, _WalkDark);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 c = tex2D(_MainTex, i.uv) * i.color;
				clip(c.a - _Cutoff);
				c.rgb *= i.light;
				return c;
			}
			ENDCG
		}
	}
}
