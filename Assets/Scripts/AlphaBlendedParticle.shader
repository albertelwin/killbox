Shader "Custom/AlphaBlendedParticle" {
Properties {
	_TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
	_MainTex ("Particle Texture", 2D) = "white" {}
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off

	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_particles

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;

			fixed4 _TintColor;
			fixed4 _Temperature;

			uniform float _Brightness;
			uniform float _InfraredAmount;

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			v2f vert(appdata_t v) {
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color;
				if(_InfraredAmount > 0.0) {
					o.color *= _Temperature;
				}
				else {
					o.color *= _TintColor;
				}
				o.color *= 2.0;
				o.color.rgb *= _Brightness;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target {
				return i.color * tex2D(_MainTex, i.texcoord);
			}
			ENDCG
		}
	}
}
}
