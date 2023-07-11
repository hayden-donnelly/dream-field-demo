using System;
using UnityEngine;
namespace UnityStandardAssets.ImageEffects
{
    public enum SSRDebugMode
    {
        None = 0,
        IncomingRadiance = 1,
        SSRResult = 2,
        FinalGlossyTerm = 3,
        SSRMask = 4,
        Roughness = 5,
        BaseColor = 6,
        SpecColor = 7,
        Reflectivity = 8,
        ReflectionProbeOnly = 9,
        ReflectionProbeMinusSSR = 10,
        SSRMinusReflectionProbe = 11,
        NoGlossy = 12,
        NegativeNoGlossy = 13,
        MipLevel = 14,
    }

    public enum PassIndex
    {
        RayTraceStep1 = 0,
        RayTraceStep2 = 1,
        RayTraceStep4 = 2,
        RayTraceStep8 = 3,
        RayTraceStep16 = 4,
        CompositeFinal = 5,
        Blur = 6,
        CompositeSSR = 7,
        Blit = 8,
        EdgeGeneration = 9,
	    MinMipGeneration = 10,
	    HitPointToReflections = 11,
        BilateralKeyPack = 12,
    }

    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Image Effects/Rendering/Screen Space Reflection")]
    public class ScreenSpaceReflection : PostEffectsBase
    {
        [Tooltip("Max raytracing length.")]
        [Range(16, 2048)]
        public int maxSteps = 128;

        [Tooltip("Log base 2 of ray tracing coarse step size. Higher traces farther, lower gives better quality silhouettes.")]
        [Range(0, 4)]
        public int rayPixelsStep = 3;

        [Tooltip("Half resolution SSRR is much faster, but less accurate. Much of the quality loss can be recovered when used in conjunction with 'Bilateral Upsample'")]
        public bool halfResolution = true;

        [Tooltip("Maximum reflection distance in world units.")]
        [Range(0.5f, 1000.0f)]
        public float maxDistance = 100.0f;

        [Tooltip("How far away from the maxDistance to begin fading SSR.")]
        [Range(0.0f, 1000.0f)]
        public float fadeDistance = 100.0f;

        [Tooltip("Typical thickness of columns, walls, furniture, and other objects that reflection rays might pass behind.")]
        [Range(0.01f, 10.0f)]
        public float expectedWallThicknessMeters = 0.5f;

        [Tooltip("Higher = fade out SSRR near the edge of the screen so that reflections don't pop under camera motion.")]
        [Range(0.0f, 1.0f)]
        public float screenEdgeFading = 0.03f;

        // When enabled, we just add our reflections on top of the existing ones. This is physically incorrect, but several
        // popular demos and games have taken this approach, and it does hide some artifacts.
        [Tooltip("Add reflections on top of existing ones. Not physically correct.")]
        public bool additiveReflection = false;

        [Tooltip("Improve visual fidelity of reflections on rough surfaces near corners in the scene, at the cost of a small amount of performance.")]
        public bool improveCorners = true;

        [Tooltip("Improve visual fidelity of mirror reflections at the cost of a small amount of performance.")]
        public bool reduceBanding = true;

        [Tooltip("Nonphysical multiplier for the SSR reflections. 1.0 is physically based.")]
        [Range(0.0f, 2.0f)]
        public float reflectionMultiplier = 1.0f;

        // Debug variable, useful for forcing all surfaces in a scene to reflection with arbitrary sharpness/roughness
        [Tooltip("Nonphysical multiplier for the SSR reflections. 1.0 is physically based.")]
        [Range(-4.0f, 4.0f)]
        private float mipBias = 0.0f;

        // Flag for whether to knock down the reflection term by occlusion stored in the gbuffer. Currently consistently gives
        // better results when true, so this flag is private for now.
        private bool useOcclusion = true;

        // When enabled, all filtering is performed at the highest resolution. This is extraordinarily slow, and should only be used during development.
        private bool fullResolutionFiltering = false;

        // Crude sky fallback, feature-gated until next revision
        private bool fallbackToSky = false;

        [Tooltip("Enable to force more surfaces to use reflection probes if you see streaks on the sides of objects or bad reflections of their backs.")] 
        public bool treatBackfaceHitAsMiss = false;

        [Tooltip("Enable for a performance gain in scenes where most glossy objects are horizontal, like floors, water, and tables. Leave off for scenes with glossy vertical objects.")]
        public bool suppressBackwardsRays = false;

        // Debug visualizations
        [Tooltip("Various Debug Visualizations")]
        public SSRDebugMode m_DebugMode = SSRDebugMode.None;

        // If false, just uses the glossy GI buffer results
        [Tooltip("Uncheck to disable SSR without disabling the entire component")]
        public bool enableSSR = true;

        // Perf optimization we still need to test across platforms
        [Tooltip("Enable to try and bypass expensive bilateral upsampling away from edges. There is a slight performance hit for generating the edge buffers, but a potentially high performance savings from bypassing bilateral upsampling where it is unneeded. Test on your target platforms to see if performance improves.")]
        private bool useEdgeDetector = false;

        [Tooltip("Enable for better reflections of very bright objects at a performance cost")]
        public bool useHDRIntermediates = true;

        [Tooltip("Increase if reflections flicker on very rough surfaces.")]
        [Range(0.0f, 1.0f)]
        public float minSmoothness = 0.2f;

        [Tooltip("Start falling back to non-SSR value solution at minSmoothness - smoothnessFalloffRange, with full fallback occuring at minSmoothness.")]
        [Range(0.0f, 0.2f)]
        public float smoothnessFalloffRange = 0.05f;

        [Tooltip("Controls how blurry reflections get as objects are further from the camera. 0 is constant blur no matter trace distance or distance from camera. 1 fully takes into account both factors.")]
        [Range(0.0f, 1.0f)]
        public float distanceBlur = 1.0f;

        [Tooltip("Amplify Fresnel fade out. Increase if floor reflections look good close to the surface and bad farther 'under' the floor.")]
        [Range(0.0f, 1.0f)]
        public float fresnelFade = 0.2f;

        [Tooltip("Higher values correspond to a faster Fresnel fade as the reflection changes from the grazing angle.")]
        [Range(0.1f, 10.0f)]
        public float fresnelFadePower = 2.0f;

        [Tooltip("Drastically improves reflection reconstruction quality at the expense of some performance.")]
        public bool bilateralUpsample = true;

        [Tooltip("Run the final resolve pass at full resolution, even if the trace was done at low resolution. Trades performance for higher quality.")]
        public bool fullResResolve = true;

        [Tooltip("Improves quality in scenes with varying smoothness, at a potential performance cost.")]
        public bool traceEverywhere = false;

        public Shader ssrShader;
        private Material ssrMaterial;


        public override bool CheckResources()
        {
            CheckSupport(true);
            ssrMaterial = CheckShaderAndCreateMaterial(ssrShader, ssrMaterial);

            if (!isSupported)
                ReportAutoDisable();
            return isSupported;
        }

        void OnDisable()
        {
            if (ssrMaterial)
                DestroyImmediate(ssrMaterial);
            ssrMaterial = null;
        }


        [ImageEffectOpaque]
        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (CheckResources() == false)
            {
                Graphics.Blit(source, destination);
                return;
            }

            int rtW, rtH;
            rtW = source.width;
            rtH = source.height;
            
            // RGB: Normals, A: Roughness.
            // Has the nice benefit of allowing us to control the filtering mode as well.
            RenderTexture bilateralKeyTexture = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGB32);
            bilateralKeyTexture.filterMode = FilterMode.Point;
            Graphics.Blit(source, bilateralKeyTexture, ssrMaterial, (int)PassIndex.BilateralKeyPack);
            ssrMaterial.SetTexture("_NormalAndRoughnessTexture", bilateralKeyTexture);

            float sWidth = source.width;
            float sHeight = source.height;

            Vector2 sourceToTempUV = new Vector2(sWidth / rtW,
                                                 sHeight / rtH);

            int downsampleAmount = halfResolution ? 2 : 1;

            rtW = rtW / downsampleAmount;
            rtH = rtH / downsampleAmount;

            ssrMaterial.SetVector("_SourceToTempUV", new Vector4(sourceToTempUV.x, sourceToTempUV.y,
                                                                 1.0f / sourceToTempUV.x, 1.0f / sourceToTempUV.y));


            Matrix4x4 P = GetComponent<Camera>().projectionMatrix;
            Vector4 projInfo = new Vector4
                ((-2.0f / (Screen.width * P[0])),
                 (-2.0f / (Screen.height * P[5])),
                 ((1.0f - P[2]) / P[0]),
                 ((1.0f + P[6]) / P[5]));

            /** The height in pixels of a 1m object if viewed from 1m away. */
            float pixelsPerMeterAtOneMeter = sWidth / (-2.0f * (float)(Math.Tan(GetComponent<Camera>().fieldOfView/180.0 * Math.PI * 0.5)));
            ssrMaterial.SetFloat("_PixelsPerMeterAtOneMeter", pixelsPerMeterAtOneMeter);


            float sx = Screen.width / 2.0f;
            float sy = Screen.height / 2.0f;

            Matrix4x4 warpToScreenSpaceMatrix = new Matrix4x4();
            warpToScreenSpaceMatrix.SetRow(0, new Vector4(sx, 0.0f, 0.0f, sx));
            warpToScreenSpaceMatrix.SetRow(1, new Vector4(0.0f, sy, 0.0f, sy));
            warpToScreenSpaceMatrix.SetRow(2, new Vector4(0.0f, 0.0f, 1.0f, 0.0f));
            warpToScreenSpaceMatrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            Matrix4x4 projectToPixelMatrix = warpToScreenSpaceMatrix * P;

            ssrMaterial.SetVector("_ScreenSize", new Vector2(Screen.width, Screen.height));
            ssrMaterial.SetVector("_ReflectionBufferSize", new Vector2(rtW, rtH));
            Vector2 invScreenSize = new Vector2((float)(1.0 / (double)Screen.width), (float)(1.0 / (double)Screen.height));

            ssrMaterial.SetVector("_InvScreenSize", invScreenSize);
            ssrMaterial.SetVector("_ProjInfo", projInfo); // used for unprojection
            ssrMaterial.SetMatrix("_ProjectToPixelMatrix", projectToPixelMatrix);
            ssrMaterial.SetMatrix("_WorldToCameraMatrix", GetComponent<Camera>().worldToCameraMatrix);
            ssrMaterial.SetMatrix("_CameraToWorldMatrix", GetComponent<Camera>().worldToCameraMatrix.inverse);
            ssrMaterial.SetInt("_EnableRefine", reduceBanding ? 1 : 0);
            ssrMaterial.SetInt("_AdditiveReflection", additiveReflection ? 1 : 0);
            ssrMaterial.SetInt("_ImproveCorners", improveCorners ? 1 : 0);
            ssrMaterial.SetFloat("_ScreenEdgeFading", screenEdgeFading);
            ssrMaterial.SetFloat("_MipBias", mipBias);
            ssrMaterial.SetInt("_UseOcclusion", useOcclusion ? 1 : 0);
            ssrMaterial.SetInt("_BilateralUpsampling", bilateralUpsample ? 1 : 0);
            ssrMaterial.SetInt("_FallbackToSky", fallbackToSky ? 1 : 0);
            ssrMaterial.SetInt("_TreatBackfaceHitAsMiss", treatBackfaceHitAsMiss ? 1 : 0);
            ssrMaterial.SetInt("_SuppressBackwardsRays", suppressBackwardsRays ? 1 : 0);
            ssrMaterial.SetInt("_TraceEverywhere", traceEverywhere ? 1 : 0);

            float z_f = GetComponent<Camera>().farClipPlane;
            float z_n = GetComponent<Camera>().nearClipPlane;

            Vector3 cameraClipInfo = (z_f == float.PositiveInfinity) ?
                new Vector3(z_n, -1.0f, 1.0f) :
                    new Vector3(z_n * z_f, z_n - z_f, z_f);

            ssrMaterial.SetVector("_CameraClipInfo", cameraClipInfo);
            ssrMaterial.SetFloat("_MaxRayTraceDistance", maxDistance);
            ssrMaterial.SetFloat("_FadeDistance", fadeDistance);
            ssrMaterial.SetFloat("_LayerThickness", expectedWallThicknessMeters);

            int maxMip = 5;
            RenderTexture[] reflectionBuffers;
            RenderTextureFormat intermediateFormat = useHDRIntermediates ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            
            reflectionBuffers = new RenderTexture[maxMip];
            for (int i = 0; i < maxMip; ++i)
            {
                if (fullResolutionFiltering)
                {
                    reflectionBuffers[i] = RenderTexture.GetTemporary(rtW, rtH, 0, intermediateFormat);
                }
                else
                {
                    reflectionBuffers[i] = RenderTexture.GetTemporary(rtW >> i, rtH >> i, 0, intermediateFormat);
                }
                // We explicitly interpolate during bilateral upsampling.
                reflectionBuffers[i].filterMode = bilateralUpsample ? FilterMode.Point : FilterMode.Bilinear;
            }

            ssrMaterial.SetInt("_EnableSSR", enableSSR ? 1 : 0);
            ssrMaterial.SetInt("_DebugMode", (int)m_DebugMode);

            ssrMaterial.SetInt("_MaxSteps", maxSteps);

            RenderTexture rayHitTexture = RenderTexture.GetTemporary(rtW, rtH, 0, RenderTextureFormat.ARGBHalf);

            // We have 5 passes for different step sizes
            int tracePass = Mathf.Clamp(rayPixelsStep, 0, 4);
            Graphics.Blit(source, rayHitTexture, ssrMaterial, tracePass);

            ssrMaterial.SetTexture("_HitPointTexture", rayHitTexture);
            // Resolve the hitpoints into the mirror reflection buffer
            Graphics.Blit(source, reflectionBuffers[0], ssrMaterial, (int)PassIndex.HitPointToReflections);


            ssrMaterial.SetTexture("_ReflectionTexture0", reflectionBuffers[0]);
            ssrMaterial.SetInt("_FullResolutionFiltering", fullResolutionFiltering ? 1 : 0);

            ssrMaterial.SetFloat("_MaxRoughness", 1.0f - minSmoothness);
            ssrMaterial.SetFloat("_RoughnessFalloffRange", smoothnessFalloffRange);

            ssrMaterial.SetFloat("_SSRMultiplier", reflectionMultiplier);

            RenderTexture[] edgeTextures = new RenderTexture[maxMip];
            if (bilateralUpsample && useEdgeDetector)
            {
                edgeTextures[0] = RenderTexture.GetTemporary(rtW, rtH);
                Graphics.Blit(source, edgeTextures[0], ssrMaterial, (int)PassIndex.EdgeGeneration);
                for (int i = 1; i < maxMip; ++i)
                {
                    edgeTextures[i] = RenderTexture.GetTemporary(rtW >> i, rtH >> i);
                    ssrMaterial.SetInt("_LastMip", i - 1);
                    Graphics.Blit(edgeTextures[i - 1], edgeTextures[i], ssrMaterial, (int)PassIndex.MinMipGeneration);
                }
            }

            // Generate the blurred low-resolution buffers
            for (int i = 1; i < maxMip; ++i)
            {
                RenderTexture inputTex = reflectionBuffers[i - 1];

                RenderTexture hBlur;
                if (fullResolutionFiltering)
                {
                    hBlur = RenderTexture.GetTemporary(rtW, rtH, 0, intermediateFormat);
                }
                else
                {
                    int lowMip = i;
                    hBlur = RenderTexture.GetTemporary(rtW >> lowMip, rtH >> (i - 1), 0, intermediateFormat);
                }
                for (int j = 0; j < (fullResolutionFiltering ? (i * i) : 1); ++j)
                {
                    // Currently we blur at the resolution of the previous mip level, we could save bandwidth by blurring directly to the lower resolution.
                    ssrMaterial.SetVector("_Axis", new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
                    ssrMaterial.SetFloat("_CurrentMipLevel", i - 1.0f);

                    Graphics.Blit(inputTex, hBlur, ssrMaterial, (int)PassIndex.Blur);

                    ssrMaterial.SetVector("_Axis", new Vector4(0.0f, 1.0f, 0.0f, 0.0f));

                    inputTex = reflectionBuffers[i];
                    Graphics.Blit(hBlur, inputTex, ssrMaterial, (int)PassIndex.Blur);
                    
                }

                ssrMaterial.SetTexture("_ReflectionTexture" + i, reflectionBuffers[i]);
                

                RenderTexture.ReleaseTemporary(hBlur);
            }

            if (bilateralUpsample && useEdgeDetector)
            {
                for (int i = 0; i < maxMip; ++i)
                {
                    ssrMaterial.SetTexture("_EdgeTexture" + i, edgeTextures[i]);
                }
            }
            ssrMaterial.SetInt("_UseEdgeDetector", useEdgeDetector ? 1 : 0);

            RenderTexture finalReflectionBuffer = RenderTexture.GetTemporary(fullResResolve ? source.width : rtW, fullResResolve ? source.height : rtH, 0, intermediateFormat);

            ssrMaterial.SetFloat("_FresnelFade", fresnelFade);
            ssrMaterial.SetFloat("_FresnelFadePower", fresnelFadePower);
            ssrMaterial.SetFloat("_DistanceBlur", distanceBlur);
            ssrMaterial.SetInt("_HalfResolution", halfResolution ? 1 : 0);
            Graphics.Blit(reflectionBuffers[0], finalReflectionBuffer, ssrMaterial, (int)PassIndex.CompositeSSR);


            ssrMaterial.SetTexture("_FinalReflectionTexture", finalReflectionBuffer);


            Graphics.Blit(source, destination, ssrMaterial, (int)PassIndex.CompositeFinal);


            RenderTexture.ReleaseTemporary(bilateralKeyTexture);
            RenderTexture.ReleaseTemporary(rayHitTexture);

            if (bilateralUpsample && useEdgeDetector)
            {
                for (int i = 0; i < maxMip; ++i)
                {
                    RenderTexture.ReleaseTemporary(edgeTextures[i]);
                }
            }
            RenderTexture.ReleaseTemporary(finalReflectionBuffer);
            for (int i = 0; i < maxMip; ++i)
            {
                RenderTexture.ReleaseTemporary(reflectionBuffers[i]);
            }
        }
    }
}

