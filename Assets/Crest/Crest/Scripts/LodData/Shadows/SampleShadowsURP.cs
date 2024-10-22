﻿// Crest Ocean System

// Copyright 2021 Wave Harmonic Ltd

#if CREST_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Crest
{
    public class SampleShadowsURP : ScriptableRenderPass
    {
        static SampleShadowsURP _instance;
        public static bool Created => _instance != null;

        public SampleShadowsURP(RenderPassEvent renderPassEvent)
        {
            this.renderPassEvent = renderPassEvent;
        }

        public static void Enable()
        {
            if (_instance == null)
            {
                _instance = new SampleShadowsURP(RenderPassEvent.AfterRenderingSkybox);
            }

            RenderPipelineManager.beginCameraRendering -= EnqueueSampleShadowPass;
            RenderPipelineManager.beginCameraRendering += EnqueueSampleShadowPass;
        }

        public static void Disable()
        {
            RenderPipelineManager.beginCameraRendering -= EnqueueSampleShadowPass;
        }

        public static void EnqueueSampleShadowPass(ScriptableRenderContext context, Camera camera)
        {
            if (OceanRenderer.Instance == null || OceanRenderer.Instance._lodDataShadow == null)
            {
                return;
            }

            // Only sample shadows for the main camera.
            if (!ReferenceEquals(OceanRenderer.Instance.Viewpoint, camera.transform))
            {
                return;
            }

            if (camera.TryGetComponent<UniversalAdditionalCameraData>(out var cameraData))
            {
                cameraData.scriptableRenderer.EnqueuePass(_instance);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (context == null)
                throw new System.ArgumentNullException("context");

            if (renderingData.lightData.mainLightIndex == -1)
                return;

            var cmd = OceanRenderer.Instance._lodDataShadow.BufCopyShadowMap;
            if (cmd == null) return;

            var camera = renderingData.cameraData.camera;

            // Target is not multi-eye so stop mult-eye rendering for this command buffer. Breaks registered shadow
            // inputs without this.
            if (camera.stereoEnabled)
            {
                context.StopMultiEye(camera);
            }

            context.ExecuteCommandBuffer(cmd);

            if (camera.stereoEnabled)
            {
                context.StartMultiEye(camera);
            }
            else
            {
                // Restore matrices otherwise remaining render will have incorrect matrices. Each pass is responsible
                // for restoring matrices if required.
                cmd.Clear();
                cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
            }
        }
    }
}

#endif // CREST_URP
