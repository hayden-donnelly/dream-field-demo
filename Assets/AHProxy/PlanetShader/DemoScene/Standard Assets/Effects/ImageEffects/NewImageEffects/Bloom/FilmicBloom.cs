using UnityEngine;
using System;

namespace UnityStandardAssets.ImageEffects
{

	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Other/FilmicBloom")]
	[RequireComponent(typeof(Camera))]

	public class FilmicBloom : PostEffectsBase
	{
		public enum TweakMode
		{
			Basic = 0,
			Advanced = 1
		}

		public enum BlendingMode
		{
			Add = 0,
			Screen = 1,
		}

		public TweakMode tweakMode = TweakMode.Basic;
		[Range(0.0f, 4.0f)]
		public float Threshold  = 0.75f;
		[Range(0.0f, 5.0f)]
		public float Intensity  = 0.5f;
		[Range(0.0f, 5.0f)]
		public float BlurAmount = 1.0f;
		[Range(0.0f, 100.0f)]
		public float SpreadAmount = 0.0f;
		public Color Tint  = new Color(1.0f, 1.0f, 1.0f, 1.0f);
		[Range(10.0f, 100.0f)]
		public float KernelSize = 50.0f;
		[Range(1.0f, 20.0f)]
		public float BlurKernelScale = 1.0f;
		[Range(3.0f, 20.0f)]
		public float SpreadKernelScale = 3.0f;
		public BlendingMode BlendMode = BlendingMode.Add;
		public bool HighQuality = false;
		public Texture2D DirtTexture = null;
		public float Dirtiness = 1.0f;

		public Shader filmicBloomShader;
		private Material filmicBloomMaterial;

		public override bool CheckResources ()
		{
			CheckSupport (false, true);

			filmicBloomMaterial = CheckShaderAndCreateMaterial (filmicBloomShader, filmicBloomMaterial);

			if (!isSupported)
				ReportAutoDisable ();

			return isSupported;
		}


		float videoBlurGetMaxScaleFor(float radius)
		{
			double x = (double)radius;
			double sc = x < 10.0 ? (0.1*x * 1.468417):(x < 36.3287 ? (0.127368 * x + 0.194737):(0.8*(float)Math.Sqrt(x)));
			return sc <= 0.0 ? 0.0f:(float)sc;
		}

		void BloomBlit(RenderTexture source, RenderTexture blur1, RenderTexture blur2, float radius1, float radius2)
		{
			const float kd0 = (4.0f/3.0f);
			const float kd1 = (1.0f/3.0f);
			float maxScale = videoBlurGetMaxScaleFor(radius1);
			int blurIteration1 = (int)maxScale;
			float lerp1 = (maxScale - (float)blurIteration1);
			maxScale = videoBlurGetMaxScaleFor(radius2);
			int blurIteration2 = (int)maxScale;
			float dUV = 1.0f;
			int rtW = source.width;
			int rtH = source.height;
			float s0 = blurIteration1 != 0 ? 1.0f:-1.0f;
			Vector4 v;
			int i;

			if (radius1 == 0.0f)
			{
				Graphics.Blit(source, blur1);
				return;
			}

			RenderTexture rt = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			rt.filterMode = FilterMode.Bilinear;
			rt.wrapMode = TextureWrapMode.Clamp;
			Graphics.Blit(source, rt);

			for (i = 0; i < blurIteration1; i++)
			{
				s0 = (i % 2 != 0 ? -1.0f:1.0f);
				v = new Vector4(s0 * dUV * kd0, dUV * kd1, s0 * dUV * kd1, -dUV * kd0);
				filmicBloomMaterial.SetVector("_Param0", v);
				RenderTexture rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
				rt2.filterMode = FilterMode.Bilinear;
				rt2.wrapMode = TextureWrapMode.Clamp;
				filmicBloomMaterial.SetTexture("_MainTex", rt);
				Graphics.Blit (rt, rt2, filmicBloomMaterial, 0);
				RenderTexture.ReleaseTemporary(rt);
				rt = rt2;
				dUV = dUV * 1.414213562373095f;
			}

			v = new Vector4(-s0 * dUV * kd0, dUV * kd1, -s0 * dUV * kd1, -dUV * kd0);
			filmicBloomMaterial.SetVector("_Param0", v);
			filmicBloomMaterial.SetFloat("_Param2", lerp1);
			filmicBloomMaterial.SetTexture("_MainTex", rt);
			Graphics.Blit (rt, blur1, filmicBloomMaterial, 1);

			if (blur2 != null)
			{
				for ( ; i < blurIteration2; i++)
				{
					s0 = (i % 2 != 0 ? -1.0f:1.0f);
					v = new Vector4(s0 * dUV * kd0, dUV * kd1, s0 * dUV * kd1, -dUV * kd0);
					filmicBloomMaterial.SetVector("_Param0", v);
					RenderTexture rt2 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
					rt2.filterMode = FilterMode.Bilinear;
					rt2.wrapMode = TextureWrapMode.Clamp;
					filmicBloomMaterial.SetTexture("_MainTex", rt);
					Graphics.Blit (rt, rt2, filmicBloomMaterial, 0);
					RenderTexture.ReleaseTemporary(rt);
					rt = rt2;
					dUV = dUV * 1.414213562373095f;
				}

				v = new Vector4(-s0 * dUV * kd0, dUV * kd1, -s0 * dUV * kd1, -dUV * kd0);
				filmicBloomMaterial.SetVector("_Param0", v);
				filmicBloomMaterial.SetFloat("_Param2", (maxScale - (float)blurIteration2));
				filmicBloomMaterial.SetTexture("_MainTex", rt);
				Graphics.Blit (rt, blur2, filmicBloomMaterial, 1);
			}

			RenderTexture.ReleaseTemporary(rt);

		}

		[ImageEffectOpaque]
		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{

			if ((BlurAmount == 0.0f && SpreadAmount == 0.0f) || Intensity == 0.0f || !CheckResources( ))
			{
				Graphics.Blit(source, destination);
				return;
			}
			float scoe = HighQuality == true ? 1.0f:0.25f;
			float rcoe = HighQuality == true ? 1.0f:0.5f;
			int rtW = (int)(scoe * (float)source.width);
			int rtH = (int)(scoe * (float)source.height);
			float w0 = 0.5f * Intensity * BlurAmount;
			float w1 = 0.5f * Intensity * SpreadAmount;
			Vector4 p0 = new Vector4(w0 * Tint.r, w0 * Tint.g, w0 * Tint.b, 1.0f);
			Vector4 p1 = new Vector4(w1 * Tint.r, w1 * Tint.g, w1 * Tint.b, 1.0f);
			float radius = (float)Screen.width * KernelSize / 1024.0f;
			float radius1 = radius * BlurKernelScale * rcoe;
			float radius2 = radius * SpreadKernelScale * rcoe;

			RenderTexture tmp1 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp2 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp3 = null;
			tmp1.filterMode = FilterMode.Bilinear;
			tmp1.wrapMode = TextureWrapMode.Clamp;
			tmp2.filterMode = FilterMode.Bilinear;
			tmp2.wrapMode = TextureWrapMode.Clamp;

			if (HighQuality == true)
			{
				filmicBloomMaterial.SetFloat("_Param2", Threshold);
				Graphics.Blit(source, tmp1, filmicBloomMaterial, 2);
			}
			else
			{

				tmp3 = RenderTexture.GetTemporary(2 * rtW, 2 * rtH, 0, RenderTextureFormat.ARGBHalf);
				tmp3.filterMode = FilterMode.Bilinear;
				tmp3.wrapMode = TextureWrapMode.Clamp;
				Graphics.Blit(source, tmp3);
				filmicBloomMaterial.SetFloat("_Param2", Threshold);
				Graphics.Blit(tmp3, tmp1, filmicBloomMaterial, 2);
				RenderTexture.ReleaseTemporary(tmp3);
				tmp3 = null;
			}

			if (SpreadAmount != 0.0f)
			{
				tmp3 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
				tmp3.filterMode = FilterMode.Bilinear;
				tmp3.wrapMode = TextureWrapMode.Clamp;
			}

			BloomBlit(tmp1, tmp2, tmp3, radius1, radius2 >= radius1 ? radius2:radius1);

			bool isLinear = true;
			//isLinear = ...
			if (!isLinear)
				Shader.EnableKeyword("UNITY_COLORSPACE_GAMMA");

			filmicBloomMaterial.SetTexture("_MainTex", source);
			filmicBloomMaterial.SetTexture("_BlurBloom", tmp2);
			filmicBloomMaterial.SetTexture("_SpreadBloom", tmp3);
			filmicBloomMaterial.SetVector("_Param0", p0);
			filmicBloomMaterial.SetVector("_Param1", p1);

			if (DirtTexture != null && Dirtiness != 0.0f)
			{
				filmicBloomMaterial.SetFloat("_Param2", Dirtiness * 100.0f);
				filmicBloomMaterial.SetTexture("_DirtTex", DirtTexture);
				Graphics.Blit(source, destination, filmicBloomMaterial, BlendMode == BlendingMode.Screen ? 6:5);
			}
			else
				Graphics.Blit(source, destination, filmicBloomMaterial, BlendMode == BlendingMode.Screen ? 4:3);


			RenderTexture.ReleaseTemporary(tmp1);
			RenderTexture.ReleaseTemporary(tmp2);
			if (tmp3 != null)
				RenderTexture.ReleaseTemporary(tmp3);

			if (!isLinear)
				Shader.DisableKeyword("UNITY_COLORSPACE_GAMMA");

		}

	}
}
