
Shader "Custom/PixelTexture" {
Properties {
	_Color ("Color", Color) = (1, 1, 1, 1)
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
				float2 screen_pos : TEXCOORD1;
			};

			fixed4 _Color;

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _MainTex_TexelSize;

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				float4 pos = mul(UNITY_MATRIX_MVP, float4(0.0, 0.0, 0.0, 1.0));
				float2 offset = ComputeScreenPos(pos).xy - 0.5;
				o.screen_pos = ComputeScreenPos(o.vertex).xy - offset;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				float2 pixel_pos = floor(i.screen_pos.xy * _ScreenParams.xy) + floor((_MainTex_TexelSize.zw - _ScreenParams.xy) * 0.5) + 0.5;
				float2 uv = pixel_pos * _MainTex_TexelSize.xy;
				return tex2D(_MainTex, uv) * _Color;
			}
		ENDCG
	}
}

}
