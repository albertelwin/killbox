
Shader "Custom/FlatShadowed" {
	Properties {
		_Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Emission ("Emission", Float) = 0.0
		_Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
	}

	SubShader {
		Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "LightMode" = "Always" "LightMode" = "ForwardBase" }

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
					float3 normal : NORMAL;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					float3 normal : TEXCOORD0;
					float3 light_dir : TEXCOORD1;
					LIGHTING_COORDS(2, 3)
				};

				v2f vert(appdata v) {
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					o.normal = v.normal;
					o.light_dir = ObjSpaceLightDir(v.vertex);
					TRANSFER_VERTEX_TO_FRAGMENT(o)
					return o;
				}

				fixed4 frag(v2f i) : COLOR {
					float3 n = normalize(i.normal);
					float3 l = normalize(i.light_dir);

					float dot_nl = saturate(dot(n, l) + 0.75);
					float light = (LIGHT_ATTENUATION(i) * pow(dot_nl, 16.0) * 0.25 + 0.75) + _Emission;

					fixed4 color;
					color.a = 1.0;
					if(_InfraredAmount > 0.0f) {
						color.rgb = _Temperature.rgb;
					}
					else {
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