Shader "__Missing" {
	Properties {}

	SubShader {
		Tags {"Queue" = "Geometry" "RenderType" = "Opaque" }

		Pass {
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				struct appdata {
					float4 vertex : POSITION;
				};

				struct v2f {
					float4 pos : SV_POSITION;
				};

				v2f vert(appdata v) {
					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					return o;
				}

				fixed4 frag(v2f i) : COLOR {
					return float4(1.0, 0.0, 0.0, 1.0);
				}
			ENDCG
		}
	}
	FallBack "VertexLit"
}