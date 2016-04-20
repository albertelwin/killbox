Shader "Custom/Water" {
    Properties {
        _Color ("Main Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Temperature ("Temperature", Color) = (0.0, 0.0, 0.0, 1.0)
        _Wave ("Wave", Vector) = (1.0, 0.2, 1.0, 0.0)
        _Spec ("Spec", Vector) = (1.0, 32.0, 0.0, 0.0)
    }

    SubShader {
        Tags {"Queue" = "Transparent+1" "IgnoreProjector"="True" "RenderType" = "Transparent" "LightMode" = "Always" "LightMode" = "ForwardBase"}

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                fixed4 _Temperature;
                float4 _Wave;
                float4 _Direction;
                float4 _Spec;

                uniform float _Brightness;
                uniform float _InfraredAmount;

                uniform float4 _LightColor0;

                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                };

                struct v2f {
                    float4 pos : SV_POSITION;
                    nointerpolation float3 normal : TEXCOORD0;
                    float3 light_dir : TEXCOORD1;
                    float3 view_dir : TEXCOORD2;
                    LIGHTING_COORDS(3, 4)
                    UNITY_FOG_COORDS(5)
                };

                v2f vert(appdata v) {
                    v2f o;

                    float3 v0 = mul(_Object2World, v.vertex).xyz;
                    float3 v1 = v0 + float3(0.05, 0.0, 0.0);
                    float3 v2 = v0 + float3(0.0, 0.0, 0.05);

                    float time_x = _Time.y * _Wave.z;
                    float time_y = _Time.y * _Wave.z * 0.333;

                    v0.y += sin(v0.x + time_x) * _Wave.x;
                    v0.y += sin((v0.x * 0.2 + v0.z * 0.4) + time_y) * _Wave.y;

                    v1.y += sin(v1.x + time_x) * _Wave.x;
                    v1.y += sin((v1.x * 0.2 + v1.z * 0.4) + time_y) * _Wave.y;

                    v2.y += sin(v2.x + time_x) * _Wave.x;
                    v2.y += sin((v2.x * 0.2 + v2.z * 0.4) + time_y) * _Wave.y;

                    v1.y -= (v1.y - v0.y) * _Wave.w;
                    v2.y -= (v2.y - v0.y) * _Wave.w;

                    float4 vertex = mul(_World2Object, float4(v0.x, v0.y, v0.z, 1.0));

                    float3 normal = cross(v2 - v0, v1 - v0);
                    normal = mul(_World2Object, normal);

                    o.pos = mul(UNITY_MATRIX_MVP, vertex);
                    o.normal = normalize(normal);
                    o.light_dir = ObjSpaceLightDir(vertex);
                    o.view_dir = ObjSpaceViewDir(vertex);
                    TRANSFER_VERTEX_TO_FRAGMENT(o)
                    UNITY_TRANSFER_FOG(o, o.pos);
                    return o;
                }

                fixed4 frag(v2f i) : COLOR {
                    i.normal = normalize(i.normal);
                    i.light_dir = normalize(i.light_dir);
                    i.view_dir = normalize(i.view_dir);

                    float nl = saturate(dot(i.normal, i.light_dir));
                    fixed atten = saturate(LIGHT_ATTENUATION(i) * nl);

                    float3 h = normalize(i.light_dir + i.view_dir);
                    float nh = saturate(dot(i.normal, h));
                    float spec = saturate(pow(nh, _Spec.y)) * _Spec.x;

                    fixed4 color = fixed4(0.0, 0.0, 0.0, 0.0);
                    color += _Color * _LightColor0 * atten;
                    color += UNITY_LIGHTMODEL_AMBIENT;
                    color.a = _Color.a;

                    color += spec;

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