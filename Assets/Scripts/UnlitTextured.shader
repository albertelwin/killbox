﻿Shader "Custom/UnlitTextured" {
Properties {
	_Color ("Color", Color) = (1, 1, 1, 1)
	_Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
	_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
}

SubShader {
	Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
	LOD 200

	ZWrite Off
	Blend SrcAlpha OneMinusSrcAlpha

	Pass {
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				half2 texcoord : TEXCOORD0;
			};

			fixed4 _Color;
			fixed4 _Temperature;

			uniform float _InfraredAmount;

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 color = tex2D(_MainTex, i.texcoord);
				if(_InfraredAmount > 0.0) {
					color.rgb *= _Temperature.rgb;
				}
				else {
					color.rgb *= _Color.rgb;
				}
				color.a *= _Color.a;
				return color;
			}
		ENDCG
	}
}

}
