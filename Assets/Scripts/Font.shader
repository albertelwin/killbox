Shader "Custom/Font" {
	Properties {
		_MainTex ("Font Texture", 2D) = "white" {}
		_Color ("Text Color", Color) = (1,1,1,1)
	}

	SubShader {

		Tags { "Queue"="Transparent+1" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Lighting Off ZTest LEqual ZWrite Off Fog { Mode Off }
		//Blend OneMinusDstColor OneMinusSrcAlpha
		Blend SrcAlpha OneMinusSrcAlpha
		Offset -1, -1

		Pass {	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform fixed4 _Color;

			v2f vert (appdata_t v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = v.color * _Color;
				o.texcoord = v.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				return o;
			}

			fixed4 frag (v2f i) : COLOR
			{
				//fixed4 col = tex2D(_MainTex, i.texcoord).a;
				fixed4 col = _Color;
				col.a *= tex2D(_MainTex, i.texcoord).a;
				return col;
			}
			ENDCG 
		}
	} 	
}
