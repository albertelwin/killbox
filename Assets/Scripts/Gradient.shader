
Shader "Custom/Gradient" {
	Properties {
		_Color0 ("Color", Color) = (1, 1, 1, 1)
		_Color1 ("Color", Color) = (1, 1, 1, 1)
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
				float y : TEXCOORD0;
			};

			fixed4 _Color0;
			fixed4 _Color1;

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.y = v.vertex.y;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				fixed4 color = lerp(_Color0, _Color1, clamp(i.y, 0.0, 1.0));
				return color;
			}

			ENDCG
		}
	}

}
