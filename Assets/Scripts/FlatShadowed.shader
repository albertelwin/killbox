Shader "Custom/FlatShadowed" {
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
                #pragma multi_compile_fog
                #pragma multi_compile_fwdbase
                #pragma fragmentoption ARB_precision_hint_fastest
               
                #include "UnityCG.cginc"
                #include "AutoLight.cginc"

                fixed4 _Color;
                float _Emission;
                fixed4 _Temperature;
                
                uniform float _Brightness;
                uniform float _InfraredAmount;

                uniform float4 _LightColor0;
               
                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };
 
                struct v2f {
                    float4 pos : SV_POSITION;
                    float3 normal : TEXCOORD0;
                    float3 light_dir : TEXCOORD1;
                    LIGHTING_COORDS(2, 3)
                    UNITY_FOG_COORDS(4)
                };
 
                v2f vert(appdata v) {
                    v2f o;
                    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                    o.normal = v.normal;
                    o.light_dir = ObjSpaceLightDir(v.vertex);
                    TRANSFER_VERTEX_TO_FRAGMENT(o)
                    UNITY_TRANSFER_FOG(o, o.pos);
                    return o;
                }
 
                fixed4 frag(v2f i) : COLOR {
                	i.normal = normalize(i.normal);
                	i.light_dir = normalize(i.light_dir);

                	float nl = saturate(pow(saturate(dot(i.normal, i.light_dir)) + 0.75, 16.0f));
                    fixed atten = saturate(LIGHT_ATTENUATION(i) * nl);

                    fixed4 color = ((_Color + _Emission) * _LightColor0 * lerp(0.75, 1.0, atten) + UNITY_LIGHTMODEL_AMBIENT);

                    color = lerp(color, _Temperature, _InfraredAmount);

                    color *= _Brightness;
                    UNITY_APPLY_FOG(i.fogCoord, color);
                    return color;
                }
            ENDCG
        }
    }
    FallBack "VertexLit"
}