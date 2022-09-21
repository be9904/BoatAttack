using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//[DisallowMultipleRendererFeature] // once not internal, this needs to be here
public class GlobalVolumeFeature : ScriptableRendererFeature
{
    class GlobalVolumePass : ScriptableRenderPass
    {
        private class PassData { }
        
        public VolumeProfile _baseProfile;
        public List<VolumeProfile> _qualityProfiles;

        private Volume vol;
        private Volume qualityVol;
        public static GameObject volumeHolder;

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            DoVolumes();
        }

        private void DoVolumes()
        {
            if(volumeHolder == null)
            {
                volumeHolder = new GameObject("[DefaultVolume]");
                vol = volumeHolder.AddComponent<Volume>();
                vol.isGlobal = true;
                qualityVol = volumeHolder.AddComponent<Volume>();
                qualityVol.isGlobal = true;
                volumeHolder.hideFlags = HideFlags.HideAndDontSave;
            }

            if (vol && _baseProfile)
            {
                vol.sharedProfile = _baseProfile;
            }

            if (!qualityVol || _qualityProfiles == null) return;
            
            var index = QualitySettings.GetQualityLevel();

            if(_qualityProfiles.Count >= index && _qualityProfiles[index] != null)
                qualityVol.sharedProfile = _qualityProfiles?[index];
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Volume Feature", out _))
            {
                builder.AllowPassCulling(false);
                DoVolumes();
                builder.SetRenderFunc((PassData _, RenderGraphContext _) => { });
            }
        }
    }

    GlobalVolumePass m_ScriptablePass;

    public VolumeProfile _baseProfile;
    public List<VolumeProfile> _qualityProfiles = new List<VolumeProfile>();

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass = new GlobalVolumePass
        {
            // Configures where the render pass should be injected.
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents,
            _baseProfile = this._baseProfile,
            _qualityProfiles = this._qualityProfiles,
        };
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(GlobalVolumePass.volumeHolder == null)
        {
            var old = GameObject.Find("[DefaultVolume]");
            if (Application.isPlaying)
            {
                Destroy(old);
            }
            else
            {
                DestroyImmediate(old);
            }
        }
        renderer.EnqueuePass(m_ScriptablePass);
    }
}


