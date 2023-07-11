using UnityEngine;
using System;

namespace UnityStandardAssets.ImageEffects
{
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Other/FilmicVignette")]
	[RequireComponent(typeof(Camera))]

	public class FilmicVignette : PostEffectsBase
	{

		[Range(0.0f, 1.0f)]
		public float Radius  = 0.5f;
		[Range(0.0f, 1.0f)]
		public float Spread  = 0.5f;
		[Range(0.0f, 1.0f)]
		public float Darken = 0.0f;
		[Range(0.0f, 1.0f)]
		public float Desaturate = 0.0f;
		[Range(0.0f, 1.0f)]
		public float Blur  = 0.0f;

		public Shader FilmicVignetteShader;
		private Material FilmicVignetteMaterial;

		public override bool CheckResources ()
		{
			CheckSupport (false, true);

			FilmicVignetteMaterial = CheckShaderAndCreateMaterial (FilmicVignetteShader, FilmicVignetteMaterial);

			if (!isSupported)
				ReportAutoDisable ();

			return isSupported;
		}


		void OnRenderImage (RenderTexture source, RenderTexture destination)
		{

			if ((Darken == 0.0f && Blur == 0.0f && Desaturate == 0.0f) || !CheckResources( ))
			{
				Graphics.Blit(source, destination);
				return;
			}

			float r1 = 0.5f * Radius * Radius;
			float r2 = Radius + Spread;
			Vector4 p0 = new Vector4(r1, 1.0f / (r2 * r2 - r1), Darken, Desaturate);
			if (Blur == 0.0f)
			{
				FilmicVignetteMaterial.SetVector("_Param0", p0);
				Graphics.Blit(source, destination, FilmicVignetteMaterial, 0);
			}
			else
			{
				float widthOverHeight = (1.0f * source.width) / (1.0f * source.height);
				float oneOverBaseSize = 1.0f / source.width;
				float blurSpread = 2.0f;
				RenderTexture tmp = null;
				RenderTexture blur1 = null;
				RenderTexture blur2 = null;
				blur1 = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0, source.format);
				FilmicVignetteMaterial.SetVector ("_Param0",new Vector4 (0.0f, blurSpread * oneOverBaseSize * widthOverHeight, 0.0f, 0.0f));
				tmp = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0, source.format);
				FilmicVignetteMaterial.SetTexture("_MainTex", source);
				Graphics.Blit (source, tmp, FilmicVignetteMaterial, 2);
				RenderTexture.ReleaseTemporary (blur1);

				FilmicVignetteMaterial.SetVector ("_Param0",new Vector4 (blurSpread * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
				blur1 = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0, source.format);
				FilmicVignetteMaterial.SetTexture("_MainTex", tmp);
				Graphics.Blit (tmp, blur1, FilmicVignetteMaterial, 2);
				RenderTexture.ReleaseTemporary (tmp);

				FilmicVignetteMaterial.SetVector ("_Param0",new Vector4 (0.0f, blurSpread * oneOverBaseSize * widthOverHeight, 0.0f, 0.0f));
				tmp = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0, source.format);
				FilmicVignetteMaterial.SetTexture("_MainTex", blur1);
				Graphics.Blit (blur1, tmp, FilmicVignetteMaterial, 2);

				FilmicVignetteMaterial.SetVector ("_Param0",new Vector4 (blurSpread * oneOverBaseSize, 0.0f, 0.0f, 0.0f));
				blur2 = RenderTexture.GetTemporary (source.width / 2, source.height / 2, 0, source.format);
				FilmicVignetteMaterial.SetTexture("_MainTex", tmp);
				Graphics.Blit (tmp, blur2, FilmicVignetteMaterial, 2);
				RenderTexture.ReleaseTemporary (tmp);

				FilmicVignetteMaterial.SetVector("_Param0", p0);
				FilmicVignetteMaterial.SetFloat("_Coe", Blur);
				FilmicVignetteMaterial.SetTexture("_MainTex", source);
				FilmicVignetteMaterial.SetTexture("_BlurTex1", blur1);
				FilmicVignetteMaterial.SetTexture("_BlurTex2", blur2);
				Graphics.Blit(source, destination, FilmicVignetteMaterial, 1);
				RenderTexture.ReleaseTemporary(blur1);
				RenderTexture.ReleaseTemporary(blur2);

			}

		}

	}
}
