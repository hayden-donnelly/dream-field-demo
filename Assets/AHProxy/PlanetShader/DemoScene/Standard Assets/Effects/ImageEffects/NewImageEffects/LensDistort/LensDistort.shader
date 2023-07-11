Shader "Hidden/LensDistort"
{
	Properties 
	{
        _MainTex ("Base (RGB)", 2D) = "black"
	}

	CGINCLUDE

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		uniform half4 _MainTex_TexelSize;
		uniform float4 _MainTex_ST;
		uniform half4 _CenterScale;
		uniform half4 _Amount;
		half _ChromaticAberration;

		struct v_distCoords
		{
			float4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;
		};

		v_distCoords vertDist(appdata_img v)
		{
			v_distCoords o;
			o.pos = UnityObjectToClipPos (v.vertex);
			o.uv = v.texcoord.xy;
			return o;
		}

		#define SAMPLE_NUM	5
		#define SAMPLE_W	0.2h
		static const half2 SampleOffset[SAMPLE_NUM] =
		{
			half2(0.0h, 0.0h),
			half2(0.5h, 0.5h),
			half2(-0.5h, 0.5h),
			half2(0.5h, -0.5h),
			half2(-0.5h, -0.5h)
		};

		half4 fragDistL(v_distCoords i) : COLOR
		{
			half2 uv = i.uv;
			uv = (uv - 0.5h) * _Amount.z + 0.5h;
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif
			half2 ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
			half ru = length(ruv);
			half wu = ru * _Amount.x;
			ru = tan(wu) * (1.0h/(ru * _Amount.y));
			uv = uv + ruv * (ru - 1.0h);
			half4 color = tex2D (_MainTex, uv);
			return (wu < 1.5h && uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
		}

		half4 fragUnDistL(v_distCoords i) : COLOR
		{
			half2 uv = (i.uv - 0.5h) * _Amount.z + 0.5h;
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif
			half2 ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
			half ru = length(ruv);
			ru = (1.0h / ru) * _Amount.x * atan(ru * _Amount.y);
			uv = uv + ruv * (ru - 1.0h);
			half4 color = tex2D (_MainTex, uv);
			return (uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
		}

		half4 fragDistH(v_distCoords i) : COLOR
		{
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif

			half2 uv, ruv;
			half ru, wu;
			half4 color = half4(0.0h, 0.0h, 0.0h, 0.0h);

			for (int j = 0; j < SAMPLE_NUM; j++)
			{
				uv = i.uv + SampleOffset[j] * _MainTex_TexelSize.xy;
				uv = (uv - 0.5h) * _Amount.z + 0.5h;
				ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
				ru = length(ruv);
				wu = ru * _Amount.x;
				ru = tan(wu) * (1.0h/(ru * _Amount.y));
				uv = uv + ruv * (ru - 1.0h);
				color += (wu < 1.5h && uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? tex2D (_MainTex, uv):half4(0.0h, 0.0h, 0.0h, 0.0h);
			}

			return SAMPLE_W * color;
		}

		half4 fragUnDistH(v_distCoords i) : COLOR
		{
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif

			half2 uv, ruv;
			half ru;
			half4 color = half4(0.0h, 0.0h, 0.0h, 0.0h);

			for (int j = 0; j < SAMPLE_NUM; j++)
			{
				uv = i.uv + SampleOffset[j] * _MainTex_TexelSize.xy;
				uv = (uv - 0.5f) * _Amount.z + 0.5f;
				ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
				ru = length(ruv);
				ru = (1.0h / ru) * _Amount.x * atan(ru * _Amount.y);
				uv = uv + ruv * (ru - 1.0);
				color += (uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? tex2D (_MainTex, uv):half4(0.0h, 0.0h, 0.0h, 0.0h);
			}

			return SAMPLE_W * color;
		}

		half4 fragDistCAL(v_distCoords i) : COLOR
		{
			half2 uv = i.uv;
			uv = (uv - 0.5h) * _Amount.z + 0.5h;
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif
			half2 ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
			half ru = length(ruv);
			half wu = ru * _Amount.x;
			ru = tan(wu) * (1.0h/(ru * _Amount.y));
			uv = uv + ruv * (ru - 1.0h);
			half4 color = tex2D (_MainTex, uv);
			ruv = 2.0h * uv - 1.0h;
			half2 uvg = _MainTex_TexelSize.xy * _ChromaticAberration * ruv * dot(ruv, ruv);
			uvg = uv - uvg;
			color.g = tex2D (_MainTex, uvg).g;
			return (wu < 1.5h && uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
		}

		half4 fragUnDistCAL(v_distCoords i) : COLOR
		{
			half2 uv = (i.uv - 0.5h) * _Amount.z + 0.5h;
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif
			half2 ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
			half ru = length(ruv);
			ru = (1.0h / ru) * _Amount.x * atan(ru * _Amount.y);
			uv = uv + ruv * (ru - 1.0h);
			half4 color = tex2D (_MainTex, uv);
			ruv = 2.0h * uv - 1.0h;
			half2 uvg = _MainTex_TexelSize.xy * _ChromaticAberration * ruv * dot(ruv, ruv);
			uvg = uv + uvg;
			color.g = tex2D (_MainTex, uvg).g;
			return (uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
		}

		half4 fragDistCAH(v_distCoords i) : COLOR
		{
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif

			half2 uv, ruv, uvg;
			half ru, wu;
			half4 color;
			half4 acc = half4(0.0h, 0.0h, 0.0h, 0.0h);

			for (int j = 0; j < SAMPLE_NUM; j++)
			{
				uv = i.uv + SampleOffset[j] * _MainTex_TexelSize.xy;
				uv = (uv - 0.5h) * _Amount.z + 0.5h;
				ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
				ru = length(ruv);
				wu = ru * _Amount.x;
				ru = tan(wu) * (1.0h/(ru * _Amount.y));
				uv = uv + ruv * (ru - 1.0h);
				color = tex2D (_MainTex, uv);
				ruv = 2.0h * uv - 1.0h;
				uvg = _MainTex_TexelSize.xy * _ChromaticAberration * ruv * dot(ruv, ruv);
				uvg = uv - uvg;
				color.g = tex2D (_MainTex, uvg).g;
				acc += (wu < 1.5h && uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
			}

			return SAMPLE_W * acc;
		}

		half4 fragUnDistCAH(v_distCoords i) : COLOR
		{
			half4 centerScale = _CenterScale;
			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0.0)
				centerScale.y = -centerScale.y;
			#endif

			half2 uv, ruv, uvg;
			half ru;
			half4 color, acc = half4(0.0h, 0.0h, 0.0h, 0.0h);

			for (int j = 0; j < SAMPLE_NUM; j++)
			{
				uv = i.uv + SampleOffset[j] * _MainTex_TexelSize.xy;
				uv = (uv - 0.5f) * _Amount.z + 0.5f;
				ruv = centerScale.zw * (uv - 0.5h - centerScale.xy);
				ru = length(ruv);
				ru = (1.0h / ru) * _Amount.x * atan(ru * _Amount.y);
				uv = uv + ruv * (ru - 1.0);
				color = tex2D (_MainTex, uv);
				ruv = 2.0h * uv - 1.0h;
				uvg = _MainTex_TexelSize.xy * _ChromaticAberration * ruv * dot(ruv, ruv);
				uvg = uv + uvg;
				color.g = tex2D (_MainTex, uvg).g;
				acc += (uv.x >= 0.0h && uv.y >= 0.0h && uv.x < 1.0h && uv.y < 1.0h) ? color:half4(0.0h, 0.0h, 0.0h, 0.0h);
			}

			return SAMPLE_W * acc;
		}

	ENDCG

	SubShader
	{

		ZTest Always Cull Off ZWrite Off Fog { Mode off }

		// 0
		Pass
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragDistL
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
			 
		}

		// 1
		Pass 
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragUnDistL
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG 

		}
		// 2
		Pass
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragDistH
			#pragma fragmentoption ARB_precision_hint_fastest
			ENDCG

		}

		// 3
		Pass
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragUnDistH
			#pragma fragmentoption ARB_precision_hint_fastest
			ENDCG

		}

		// 4
		Pass
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragDistCAL
			#pragma fragmentoption ARB_precision_hint_fastest
			ENDCG

		}

		// 5
		Pass
		{

			CGPROGRAM
			#pragma vertex vertDist
			#pragma fragment fragUnDistCAL
			#pragma fragmentoption ARB_precision_hint_fastest
			ENDCG

		}

		// 6
		Pass
		{

		CGPROGRAM
		#pragma vertex vertDist
		#pragma fragment fragDistCAH
		#pragma fragmentoption ARB_precision_hint_fastest
		ENDCG

		}

		// 7
		Pass
		{

		CGPROGRAM
		#pragma vertex vertDist
		#pragma fragment fragUnDistCAH
		#pragma fragmentoption ARB_precision_hint_fastest
		ENDCG

		}
	}

	FallBack Off
}

