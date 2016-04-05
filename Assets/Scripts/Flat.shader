Shader "Custom/Flat" {
    Properties {
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Emission ("Emission", Float) = 0.0
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

                uniform float _Brightness = 1.0;

                uniform float4 _LightColor0;

                struct appdata {
                    float4 vertex : POSITION;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    LIGHTING_COORDS(2, 3)
                    UNITY_FOG_COORDS(4)
                };

                v2f vert(appdata v) {
                    v2f o;
                    o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
                    TRANSFER_VERTEX_TO_FRAGMENT(o)
                    UNITY_TRANSFER_FOG(o, o.pos);
                    return o;
                }

                fixed4 frag(v2f i) : COLOR {
                    fixed atten = LIGHT_ATTENUATION(i);
                    fixed4 color = (_Color * _LightColor0 * lerp(0.75, 1.0, atten) + _Color * _Emission + UNITY_LIGHTMODEL_AMBIENT) * _Brightness;

                    UNITY_APPLY_FOG(i.fogCoord, color);
                    return color;
                }
            ENDCG
        }
    }
    FallBack "VertexLit"
}