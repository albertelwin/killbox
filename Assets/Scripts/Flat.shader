
Shader "Custom/Flat" {
	Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Emission ("Emission", Float) = 0.0
		_Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
	}

	SubShader {
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque" "LightMode" = "Always" "LightMode" = "ForwardBase"}

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile_fwdbase
				#pragma fragmentoption ARB_precision_hint_fastest

				#include "UnityCG.cginc"
				#include "AutoLight.cginc"

				fixed4 _Color;
				float _Emission;
				fixed4 _Temperature;

				uniform float _Brightness;
				uniform float _InfraredAmount;

				struct appdata {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					LIGHTING_COORDS(2, 3)
				};

				v2f vert(appdata v) {
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					TRANSFER_VERTEX_TO_FRAGMENT(o)
					return o;
				}

				fixed4 frag(v2f i) : COLOR {
					fixed4 color;
					color.a = 1.0;
					if(_InfraredAmount > 0.0f) {
						color.rgb = _Temperature.rgb;
					}
					else {
						float light = (LIGHT_ATTENUATION(i) * 0.25 + 0.75) + _Emission;
						color.rgb = _Color.rgb * light + UNITY_LIGHTMODEL_AMBIENT.rgb;
					}
					color.rgb *= _Brightness;

					return color;
				}
			ENDCG
		}
	}
	FallBack "VertexLit"
}