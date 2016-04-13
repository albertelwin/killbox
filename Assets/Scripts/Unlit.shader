Shader "Custom/Unlit" {
	Properties {
		_Color ("Color", Color) = (1, 1, 1, 1)
		_Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
	}

	SubShader {
		Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
			};

			struct v2f {
				float4 vertex : SV_POSITION;
			};

			fixed4 _Color;
			fixed4 _Temperature;

			uniform float _InfraredAmount;

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 col = float4(lerp(_Color.xyz, _Temperature.xyz, _InfraredAmount), _Color.a);
				return col;
			}

			ENDCG
		}
	}

}
