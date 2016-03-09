Shader "Custom/FadeImageEffect" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader {
		Pass {
		ZTest Always Cull Off ZWrite Off
				
		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#include "UnityCG.cginc"

		uniform sampler2D _MainTex;
		uniform float _Alpha;

		fixed4 frag(v2f_img i) : SV_Target {
			fixed4 color = tex2D(_MainTex, i.uv);
			return lerp(color, float4(0.0, 0.0, 0.0, 0.0), _Alpha);
		}
		ENDCG
		}
	}

	Fallback off
}
