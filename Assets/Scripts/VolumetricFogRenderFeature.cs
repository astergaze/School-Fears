using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class VolumetricFogRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material fogMaterial = null;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    private class FogPass : ScriptableRenderPass
    {
        private readonly Material material;

        public FogPass(Material mat)
        {
            this.material = mat;
        }

        private class PassData
        {
            public Material fogMat;
            public TextureHandle srcTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle activeColor = resourceData.activeColorTexture;

            // Creamos un target temporal dentro de Render Graph
            var desc = renderGraph.GetTextureDesc(activeColor);
            desc.name = "_VolumetricFogTemp";
            desc.clearBuffer = false;
            TextureHandle tempTexture = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Volumetric Fog Pass", out var passData))
            {
                passData.fogMat = material;
                passData.srcTexture = activeColor;

                builder.UseTexture(activeColor, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0), data.fogMat, 0);
                });
            }

            // Copiamos de vuelta al color principal
            using (var copyBuilder = renderGraph.AddRasterRenderPass<PassData>("Volumetric Fog Copy Back", out var passData))
            {
                passData.srcTexture = tempTexture;

                copyBuilder.UseTexture(tempTexture, AccessFlags.Read);
                copyBuilder.SetRenderAttachment(activeColor, 0, AccessFlags.Write);

                copyBuilder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.srcTexture, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }

    private FogPass fogPass;

    public override void Create()
    {
        if (settings.fogMaterial != null)
        {
            fogPass = new FogPass(settings.fogMaterial);
            fogPass.renderPassEvent = settings.renderPassEvent;
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.fogMaterial != null && fogPass != null)
        {
            renderer.EnqueuePass(fogPass);
        }
    }
}