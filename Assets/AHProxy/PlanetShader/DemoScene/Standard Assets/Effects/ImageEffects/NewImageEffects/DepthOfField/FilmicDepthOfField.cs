using UnityEngine;
using System;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Other/FilmicDepthOfField")]
	[RequireComponent(typeof(Camera))]

	public class FilmicDepthOfField : PostEffectsBase
	{
		public enum TweakMode
		{
			Basic = 0,
			Advanced = 1,
			Explicit = 2
		}
		public enum ApertureShape
		{
			Hexagon = 0,
			Octogon = 1,
			Circle = 2,
			DX11 = 3,
		}

		public bool Visualize  = false;
		[Range(0.0f, 2.0f)]
		public float NearPlane  = 0.0f;
		[Range(0.0f, 15.0f)]
		public float NearRadius  = 5.0f;
		[Range(0.0f, 2.0f)]
		public float FocusPlane  = 0.5f;
		[Range(0.0f, 2.0f)]
		public float FocusRange = 0.0f;
		[Range(0.0f, 2.0f)]
		public float FarPlane  = 1.0f;
		[Range(0.0f, 15.0f)]
		public float FarRadius  = 5.0f;
		[Range(0.0f, 4.0f)]
		public float BoostPoint  = 0.75f;
		[Range(0.0f, 5.0f)]
		public float NearBoostAmount  = 0.0f;
		[Range(0.0f, 5.0f)]
		public float FarBoostAmount  = 0.0f;
		[Range(0.0f, 10.0f)]
		public float FStops  = 22.0f;
		[Range(0.0f, 15.0f)]
		public float Radius  = 5.0f;


		public ApertureShape Shape = ApertureShape.Hexagon;
		public TweakMode tweakMode = TweakMode.Basic;

		public Shader filmicDepthOfFieldShader;
		private Material filmicDepthOfFieldMaterial;

		public override bool CheckResources ()
		{
			CheckSupport (true, true);

			filmicDepthOfFieldMaterial = CheckShaderAndCreateMaterial (filmicDepthOfFieldShader, filmicDepthOfFieldMaterial);

			if (!isSupported)
				ReportAutoDisable ();

			return isSupported;
		}


		void WhiteBoost (RenderTexture source, RenderTexture bluriness, RenderTexture tmp, RenderTexture destination)
		{
			if ((NearBoostAmount == 0.0f && FarBoostAmount == 0.0) || (tweakMode == TweakMode.Basic))
			{
				Graphics.Blit(source, destination);
			}
			else
			{
				Vector4 blurrinessCoe = new Vector4(NearRadius, FarRadius, 0.0f, 0.0f);
				filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);
				filmicDepthOfFieldMaterial.SetFloat("_Param1", BoostPoint);
				filmicDepthOfFieldMaterial.SetTexture("_MainTex", source);
				filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
				Graphics.Blit(source, tmp, filmicDepthOfFieldMaterial, 20);
				filmicDepthOfFieldMaterial.SetTexture("_MainTex", source);
				filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
				filmicDepthOfFieldMaterial.SetTexture("_ThirdTex", tmp);
				Vector4 boostCoe = new Vector4(NearBoostAmount * 0.5f, FarBoostAmount * 0.5f, 0.0f, 0.0f);
				filmicDepthOfFieldMaterial.SetVector("_Param0", boostCoe);
				Graphics.Blit(source, destination, filmicDepthOfFieldMaterial, 21);
			}
		}


		void Bluriness (RenderTexture source, RenderTexture destination)
		{
			Vector4 blurrinessParam;
			Vector4 blurrinessCoe;
			if (tweakMode == TweakMode.Basic || tweakMode == TweakMode.Advanced)
			{
				float focusDistance01 = (FocusPlane * FocusPlane * FocusPlane * FocusPlane);
				float focusRange01 = 0.0f;
				if (tweakMode == TweakMode.Advanced)
					focusRange01 = FocusRange * FocusRange * FocusRange * FocusRange;
				float focalLength = 17.5f / Mathf.Tan(0.5f * GetComponent<Camera>().fieldOfView * Mathf.Deg2Rad);
				float aperture = focalLength / FStops;
				float c1 = 1.0f, c2 = 1.0f;
				if (Visualize)
				{
					c1 = NearRadius / 15.0f;
					c2 = FarRadius / 15.0f;
				}


				blurrinessCoe = new Vector4(0.0f, 0.0f, c1, c2);
				blurrinessParam = new Vector4(aperture, focalLength, focusDistance01, focusRange01);
				filmicDepthOfFieldMaterial.SetVector("_BlurParams", blurrinessParam);
				filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);

				Graphics.Blit(source, destination, filmicDepthOfFieldMaterial, Visualize ? 0:1);
			}
			else
			{
				float focusDistance01 = (FocusPlane * FocusPlane * FocusPlane * FocusPlane);
				float nearDistance01 = NearPlane * NearPlane * NearPlane * NearPlane;
				float farDistance01 = FarPlane * FarPlane * FarPlane * FarPlane;
				float nearFocusRange01 = FocusRange * FocusRange * FocusRange * FocusRange;
				float farFocusRange01 = nearFocusRange01;

				if (focusDistance01 <= nearDistance01)
					focusDistance01 = nearDistance01 + 0.0000001f;
				if (focusDistance01 >= farDistance01)
					focusDistance01 = farDistance01 - 0.0000001f;
				if ((focusDistance01 - nearFocusRange01) <= nearDistance01)
					nearFocusRange01 = (focusDistance01 - nearDistance01 - 0.0000001f);
				if ((focusDistance01 + farFocusRange01) >= farDistance01)
					farFocusRange01 = (farDistance01 - focusDistance01 - 0.0000001f);


				float a1 = 1.0f / (nearDistance01 - focusDistance01 + nearFocusRange01), a2 = 1.0f / (farDistance01 - focusDistance01 - farFocusRange01);
				float b1 = (1.0f - a1 * nearDistance01), b2 = (1.0f - a2 * farDistance01);
				float c1 = -1.0f, c2 = 1.0f;
				if (Visualize)
				{
					c1 = NearRadius / 15.0f;
					c2 = FarRadius / 15.0f;
				}

				blurrinessParam = new Vector4(c1 * a1, c1 * b1, c2 * a2, c2 * b2);
				blurrinessCoe = new Vector4(0.0f, 0.0f, (b2 - b1) / (a1 - a2), 0.0f);
				filmicDepthOfFieldMaterial.SetVector("_BlurParams", blurrinessParam);
				filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);

				Graphics.Blit(source, destination, filmicDepthOfFieldMaterial, Visualize ? 2:3);
			}
		}

		void DoCircle (RenderTexture source, RenderTexture destination)
		{
			float maxRadius = NearRadius > FarRadius ? NearRadius:FarRadius;
			if (maxRadius == 0.0)
			{
				Graphics.Blit(source, destination);
				return;
			}

			// Setup
			int rtW = source.width;
			int rtH = source.height;
			RenderTexture bluriness = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp1 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp2 = RenderTexture.GetTemporary(rtW / 2, rtH / 2, 0, RenderTextureFormat.ARGBHalf);
			bluriness.filterMode = FilterMode.Bilinear;
			bluriness.wrapMode = TextureWrapMode.Clamp;
			tmp1.filterMode = FilterMode.Bilinear;
			tmp1.wrapMode = TextureWrapMode.Clamp;


			// Blur Map
			Bluriness (source, bluriness);

			// Boost
			RenderTexture TmpSource = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			WhiteBoost (source, bluriness, tmp1, TmpSource);

			// Convolve
			Vector4 blurrinessCoe;
			int blurPass = maxRadius <= 7.0f ? 16:18;
			RenderTexture.ReleaseTemporary(tmp1);
			tmp1 = RenderTexture.GetTemporary(rtW / 2, rtH / 2, 0, RenderTextureFormat.ARGBHalf);
			Graphics.Blit(TmpSource, tmp1);
			blurrinessCoe = new Vector4(0.4f * NearRadius, 0.4f * FarRadius, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", tmp1);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			Graphics.Blit(tmp1, tmp2, filmicDepthOfFieldMaterial, blurPass);

			blurrinessCoe = new Vector4(0.8f * NearRadius, 0.8f * FarRadius, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", TmpSource);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetTexture("_ThirdTex", tmp2);
			Graphics.Blit(TmpSource, destination, filmicDepthOfFieldMaterial, blurPass+1);

			RenderTexture.ReleaseTemporary(TmpSource);
			RenderTexture.ReleaseTemporary(bluriness);
			RenderTexture.ReleaseTemporary(tmp1);
			RenderTexture.ReleaseTemporary(tmp2);


		}

		void DoDX11 (RenderTexture source, RenderTexture destination)
		{
			Graphics.Blit(source, destination);
		}

		void DoHexagon (RenderTexture source, RenderTexture destination)
		{
			float maxRadius = NearRadius > FarRadius ? NearRadius:FarRadius;
			if (maxRadius == 0.0)
			{
				Graphics.Blit(source, destination);
				return;
			}

			// Setup
			int rtW = source.width;
			int rtH = source.height;
			RenderTexture bluriness = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp1 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp2 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			bluriness.filterMode = FilterMode.Bilinear;
			bluriness.wrapMode = TextureWrapMode.Clamp;
			tmp1.filterMode = FilterMode.Bilinear;
			tmp1.wrapMode = TextureWrapMode.Clamp;
			tmp2.filterMode = FilterMode.Bilinear;
			tmp2.wrapMode = TextureWrapMode.Clamp;


			// Blur Map
			Bluriness (source, bluriness);

			// Boost
			RenderTexture TmpSource = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			WhiteBoost (source, bluriness, tmp1, TmpSource);

			// Convolve
			int blurPass = maxRadius <= 5.0f ? 10:(maxRadius <= 10.0f ? 12:14);
			Vector4 blurrinessCoe = new Vector4(NearRadius, FarRadius, 0.0f, 0.0f);
			Vector4 delta = new Vector4(0.5f, 0.0f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", TmpSource);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(TmpSource, tmp1, filmicDepthOfFieldMaterial, blurPass);

			delta = new Vector4(0.25f, 0.433013f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", tmp1);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(tmp1, tmp2, filmicDepthOfFieldMaterial, blurPass);

			delta = new Vector4(0.25f, -0.433013f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", tmp1);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetTexture("_ThirdTex", tmp2);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(tmp1, destination, filmicDepthOfFieldMaterial, blurPass + 1);

			RenderTexture.ReleaseTemporary(tmp1);
			RenderTexture.ReleaseTemporary(tmp2);
			RenderTexture.ReleaseTemporary(bluriness);
			RenderTexture.ReleaseTemporary(TmpSource);

		}

		void DoOctogon (RenderTexture source, RenderTexture destination)
		{
			float maxRadius = NearRadius > FarRadius ? NearRadius:FarRadius;
			if (maxRadius == 0.0)
			{
				Graphics.Blit(source, destination);
				return;
			}


			// Setup
			int rtW = source.width;
			int rtH = source.height;
			RenderTexture bluriness = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp1 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			RenderTexture tmp2 = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			bluriness.filterMode = FilterMode.Bilinear;
			bluriness.wrapMode = TextureWrapMode.Clamp;
			tmp1.filterMode = FilterMode.Bilinear;
			tmp1.wrapMode = TextureWrapMode.Clamp;
			tmp2.filterMode = FilterMode.Bilinear;
			tmp2.wrapMode = TextureWrapMode.Clamp;


			// Blur Map
			Bluriness (source, bluriness);

			// Boost
			RenderTexture TmpSource = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);
			WhiteBoost (source, bluriness, tmp1, TmpSource);

			// Convolve
			int blurPass = maxRadius <= 5.0f ? 10:(maxRadius <= 10.0f ? 12:14);
			Vector4 blurrinessCoe = new Vector4(NearRadius, FarRadius, 0.0f, 0.0f);
			Vector4 delta = new Vector4(0.5f, 0.0f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetVector("_BlurCoe", blurrinessCoe);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", TmpSource);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(TmpSource, tmp1, filmicDepthOfFieldMaterial, blurPass);

			delta = new Vector4(0.0f, 0.5f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", tmp1);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(tmp1, tmp2, filmicDepthOfFieldMaterial, blurPass);

			delta = new Vector4(-0.353553f, 0.353553f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", TmpSource);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(TmpSource, tmp1, filmicDepthOfFieldMaterial, blurPass);

			delta = new Vector4(0.353553f, 0.353553f, 0.0f, 0.0f);
			filmicDepthOfFieldMaterial.SetTexture("_MainTex", tmp1);
			filmicDepthOfFieldMaterial.SetTexture("_SecondTex", bluriness);
			filmicDepthOfFieldMaterial.SetTexture("_ThirdTex", tmp2);
			filmicDepthOfFieldMaterial.SetVector("_Delta", delta);
			Graphics.Blit(tmp1, destination, filmicDepthOfFieldMaterial, blurPass + 1);

			RenderTexture.ReleaseTemporary(tmp1);
			RenderTexture.ReleaseTemporary(tmp2);
			RenderTexture.ReleaseTemporary(bluriness);
			RenderTexture.ReleaseTemporary(TmpSource);

		}

		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{

			if (!CheckResources( ))
			{
				Graphics.Blit(source, destination);
				return;
			}

			if (Visualize)
			{
				Bluriness(source, destination);
			}
			else
			{
				if (Shape == ApertureShape.Circle)
					DoCircle(source, destination);
				else if (Shape == ApertureShape.Hexagon)
					DoHexagon(source, destination);
				else if (Shape == ApertureShape.Octogon)
					DoOctogon(source, destination);
				else if (Shape == ApertureShape.DX11)
					DoDX11(source, destination);
			}

		}

	}
}
