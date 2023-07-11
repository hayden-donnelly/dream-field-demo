Shader "Hidden/FilmicVignette"
{
	Properties 
	{
        _MainTex ("Base (RGB)", 2D) = "black"
		_BlurTex1 ("Base (RGB)", 2D) = "black"
		_BlurTex2 ("Base (RGB)", 2D) = "black"
	}

	CGINCLUDE

		#include "UnityCG.cginc"

		sampler2D _MainTex;
		sampler2D _BlurTex1;
		sampler2D _BlurTex2;
		uniform half4 _MainTex_TexelSize;
		uniform float4 _MainTex_ST;
		uniform half4 _Param0;
		uniform half _Coe;

		struct v_distCoords
		{
			half4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;
		};

		struct v2f
		{
			half4 pos : SV_POSITION;
			half2 uv : TEXCOORD0;

			half4 uv01 : TEXCOORD1;
			half4 uv23 : TEXCOORD2;
			half4 uv45 : TEXCOORD3;
		};

		v_distCoords vertDist(appdata_img v)
		{
			v_distCoords o;
			o.pos = UnityObjectToClipPos (v.vertex);
			o.uv = v.texcoord.xy;
			return o;
		}

		v2f vertBlur(appdata_img v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv.xy = v.texcoord.xy;
			o.uv01 =  v.texcoord.xyxy + _Param0.xyxy * float4(1,1, -1,-1);
			o.uv23 =  v.texcoord.xyxy + _Param0.xyxy * float4(2,2, -2,-2);
			o.uv45 =  v.texcoord.xyxy + _Param0.xyxy * float4(3,3, -3,-3);
			return o;
		}

		half4 fragBlur(v2f i) : SV_Target
		{
			half4 color = tex2D (_MainTex, i.uv);
			color += tex2D (_MainTex, i.uv01.xy);
			color += tex2D (_MainTex, i.uv01.zw);
			color += tex2D (_MainTex, i.uv23.xy);
			color += tex2D (_MainTex, i.uv23.zw);
			color += tex2D (_MainTex, i.uv45.xy);
			color += tex2D (_MainTex, i.uv45.zw);
			return 0.14285714285714h * color;
		}

		half4 fragVignette1(v_distCoords i) : COLOR
		{
			half3 color = tex2D (_MainTex, i.uv).xyz;
			half3 blur1 = tex2D (_BlurTex1, i.uv).xyz;
			half3 blur2 = tex2D (_BlurTex2, i.uv).xyz;
			half2 uv = (i.uv - 0.5h);
			half ru = dot(uv, uv);
			half mask = saturate((ru - _Param0.x) * _Param0.y);

			half coe = mask * _Coe;
			color = coe <= 0.5h ? lerp(color, blur1, 2.0h * coe):lerp(blur1, blur2, 2.0h * coe - 1.0h);
			half luma = dot(color, half3(0.3h, 0.59h, 0.11h));
			color = lerp(color, luma.xxx, mask * _Param0.w);
			color = color - color * mask * _Param0.z;
			return half4(color, 1.0h);
		}

		half4 fragVignette0(v_distCoords i) : COLOR
		{
			half2 uv = (i.uv - 0.5h);
			half3 color = tex2D (_MainTex, i.uv).xyz;
			half luma = dot(color, half3(0.3h, 0.59h, 0.11h));
			half ru = dot(uv, uv);
			half mask = saturate((ru - _Param0.x) * _Param0.y);
			color = lerp(color, luma.xxx, mask * _Param0.w);
			color = color - color * mask * _Param0.z;
			return half4(color, 1.0h);
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
			#pragma fragment fragVignette0
			#pragma fragmentoption ARB_precision_hint_fastest 
			ENDCG
			 
		}
		// 1
		Pass
		{

		CGPROGRAM
		#pragma vertex vertDist
		#pragma fragment fragVignette1
		#pragma fragmentoption ARB_precision_hint_fastest
		ENDCG

		}

		// 2
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vertBlur
			#pragma fragment fragBlur
			ENDCG
		}

	}

	FallBack Off
}

