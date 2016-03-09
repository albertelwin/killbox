Shader "Custom/Invert" {
	Properties {}

	SubShader {
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 200
		
		ZWrite Off
		//Blend OneMinusDstColor OneMinusSrcAlpha
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
				
				v2f vert(appdata_t v)
				{
					v2f o;
					o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
					return o;
				}
				
				fixed4 frag(v2f i) : SV_Target
				{
					return float4(1.0, 1.0, 1.0, 1.0);
				}
			ENDCG
		}
	}
}
