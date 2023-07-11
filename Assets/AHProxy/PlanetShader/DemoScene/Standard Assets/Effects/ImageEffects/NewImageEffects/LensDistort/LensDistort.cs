using UnityEngine;
using System;

namespace UnityStandardAssets.ImageEffects {
	[ExecuteInEditMode]
	[AddComponentMenu("Image Effects/Other/LensDistort")]
	[RequireComponent(typeof (Camera))]
	public class LensDistort : PostEffectsBase {
		public enum Mode {
			Distort = 0,
			Undistort = 1
		}


		public Mode tweakMode = Mode.Distort;
		[Range(0.0f, 100.0f)] public float Amount = 0.0f;
		[Range(0.0f, 1.0f)] public float CenterX = 0.5f;
		[Range(0.0f, 1.0f)] public float CenterY = 0.5f;
		[Range(0.0f, 2.0f)] public float AmountX = 1.0f;
		[Range(0.0f, 2.0f)] public float AmountY = 1.0f;

		[Range(0.0f, 1.0f)] public float ChromaticAberration = 0.0f;
		[Range(0.5f, 2.0f)] public float Scale = 1.0f;
		public bool OverSampling = false;

		public Shader LensDistortShader;
		private Material LensDistortMaterial;

		public override bool CheckResources() {
			CheckSupport(false, true);

			LensDistortMaterial = CheckShaderAndCreateMaterial(LensDistortShader, LensDistortMaterial);

			if (!isSupported)
				ReportAutoDisable();

			return isSupported;
		}


		private void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if ((Amount == 0.0f && Scale == 1.0f && ChromaticAberration == 0.0f) || !CheckResources()) {
				Graphics.Blit(source, destination);
				return;
			}
			float amount = 1.6f*Math.Max(Amount, 1.0f);
			float theta = 0.01745329251994f*Math.Min(160.0f, amount);
			float sigma = 2.0f*Mathf.Tan(theta*0.5f);
			int passNum = ChromaticAberration == 0.0f ? OverSampling ? 2 : 0 : OverSampling ? 6 : 4;

			Vector4 p0 = new Vector4(2.0f*CenterX - 1.0f, 2.0f*CenterY - 1.0f, AmountX, AmountY);
			Vector4 p1 = new Vector4(tweakMode == Mode.Distort ? theta : 1.0f/theta, sigma, 1.0f/Scale, 0.0f);
			LensDistortMaterial.SetTexture("_MainTex", source);
			LensDistortMaterial.SetVector("_CenterScale", p0);
			LensDistortMaterial.SetVector("_Amount", p1);
			LensDistortMaterial.SetFloat("_ChromaticAberration", 2.0f*ChromaticAberration);
			Graphics.Blit(source, destination, LensDistortMaterial, passNum + (tweakMode == Mode.Distort ? 0 : 1));
		}
	}
}