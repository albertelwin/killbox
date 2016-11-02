
//NOTE: Timothy Lottes Public Domain CRT Shader -> https://www.shadertoy.com/view/XsjSzR#

Shader "Custom/VideoFilterImageEffect" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader {
		Pass {
		ZTest Always Cull Off ZWrite Off

		CGPROGRAM
		#pragma vertex vert_img
		#pragma fragment frag
		#pragma target 3.0
		#include "UnityCG.cginc"

		#define _SCALE 0.5

		uniform sampler2D _MainTex;
		uniform float4 _MainTex_TexelSize;

		uniform float _HardScan;
		uniform float _HardPix;
		uniform float2 _Mask;
		uniform float _Saturation;

		// Nearest emulated sample given floating point position and texel offset
		// Also zero's off screen
		float3 fetch(float2 pos, float2 off) {
			float2 res = _MainTex_TexelSize.zw * _SCALE;

			pos = floor(pos * res + off) / res;
			if(max(abs(pos.x - 0.5), abs(pos.y - 0.5)) > 0.5) {
				return float3(0.0, 0.0, 0.0);
			}

			return tex2Dbias(_MainTex, float4(pos.xy, 0.0, -16.0)).rgb;
		}

		// Distance in emulated pixels to nearest texel
		float2 dist(float2 pos) {
			float2 res = _MainTex_TexelSize.zw * _SCALE;

			pos = pos * res;
			return -((pos - floor(pos)) - float2(0.5, 0.05));
		}

		// 1D Gaussian
		float gaus(float pos, float scale) {
			return exp2(scale * pos * pos);
		}

		// 3-tap Gaussian filter along horz line
		float3 horz3(float2 pos, float off) {
			float3 b = fetch(pos, float2(-1.0, off));
			float3 c = fetch(pos, float2( 0.0, off));
			float3 d = fetch(pos, float2( 1.0, off));

			float dst = dist(pos).x;

			// Convert distance to weight
			float wb = gaus(dst - 1.0, _HardPix);
			float wc = gaus(dst + 0.0, _HardPix);
			float wd = gaus(dst + 1.0, _HardPix);

			// Return filtered sample
			return (b * wb + c * wc + d * wd) / (wb + wc + wd);
		}

		// 5-tap Gaussian filter along horz line
		float3 horz5(float2 pos, float off) {
			float3 a = fetch(pos, float2(-2.0, off));
			float3 b = fetch(pos, float2(-1.0, off));
			float3 c = fetch(pos, float2( 0.0, off));
			float3 d = fetch(pos, float2( 1.0, off));
			float3 e = fetch(pos, float2( 2.0, off));

			float dst = dist(pos).x;

			// Convert distance to weight
			float wa = gaus(dst - 2.0, _HardPix);
			float wb = gaus(dst - 1.0, _HardPix);
			float wc = gaus(dst + 0.0, _HardPix);
			float wd = gaus(dst + 1.0, _HardPix);
			float we = gaus(dst + 2.0, _HardPix);

			// Return filtered sample
			return (a * wa + b * wb + c * wc + d * wd + e * we) / (wa + wb + wc + wd + we);
		}

		// Return scanline weight
		float scan(float2 pos, float off) {
			float dst = dist(pos).y;
			return gaus(dst + off, _HardScan);
		}

		// Allow nearest three lines to effect pixel
		float3 tri(float2 pos) {
			float3 a = horz3(pos,-1.0);
			float3 b = horz5(pos, 0.0);
			float3 c = horz3(pos, 1.0);

			float wa = scan(pos,-1.0);
			float wb = scan(pos, 0.0);
			float wc = scan(pos, 1.0);

			return a * wa + b * wb + c * wc;
		}

		// Shadow mask
		float3 mask(float2 pos) {
			float3 mask = _Mask.xxx;

			pos.x += pos.y * 3.0;
			pos.x = frac(pos.x / 6.0);

			if(pos.x < 0.333) {
				mask.r = _Mask.y;
			}
			else if(pos.x < 0.666) {
				mask.g = _Mask.y;
			}
			else {
				mask.b = _Mask.y;
			}

			return mask;
		}

		fixed4 frag(v2f_img i) : SV_Target {
			float3 rgb = tri(i.uv) * mask(i.uv * _MainTex_TexelSize.zw);

			float lum = ((rgb.r * 0.2126) + rgb.g * 0.7152) + rgb.b * 0.0722;
			rgb = (rgb * _Saturation) + lum * (1.0 - _Saturation);

			return float4(rgb, 1.0);
		}
		ENDCG
		}
	}

	Fallback off
}
