using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class AlonerGrabPass_RenderTex : ScriptableRendererFeature
{

    [System.Serializable]
    public class FeatureSettings
    {
        public string profilerTag = "AlonerGrabPass_RenderTex";
        public bool IsEnabled = true;
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRenderingTransparents;
        public RenderTexture tex;
    }
    class AlonerGrabPass_RenderTex_Pass : ScriptableRenderPass
    {
        string profilerTag;
        RenderTexture renderTex;

        RenderTargetHandle tempTexture;
        RenderTargetIdentifier cameraColorTargetIdent;

        public AlonerGrabPass_RenderTex_Pass(string profilerTag,
            RenderPassEvent renderPassEvent, RenderTexture tex)
        {
            this.profilerTag = profilerTag;
            this.renderPassEvent = renderPassEvent;
            this.renderTex = tex;
        }

        // This isn't part of the ScriptableRenderPass class and is our own addition.
        // For this custom pass we need the camera's color target, so that gets passed in.
        public void Setup(RenderTargetIdentifier cameraColorTargetIdent)
        {
            this.cameraColorTargetIdent = cameraColorTargetIdent;
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // called each frame before Execute, use it to set up things the pass will need
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // create a temporary render texture that matches the camera
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            // cmd.Clear();

            foreach (var camera in UnityEditor.SceneView.GetAllSceneCameras())
            {
                if (camera == renderingData.cameraData.camera) return;
            }

            if (renderingData.cameraData.camera.name.Contains("Preview")) return;

            // the actual content of our custom render pass!
            // we apply our material while blitting to a temporary texture
            // cmd.Blit(cameraColorTargetIdent, renderTex);
            cmd.Blit(renderingData.cameraData.renderer.cameraColorTarget, renderTex);
            // cmd.SetRenderTarget(renderTex);
            // cmd.ClearRenderTarget(true, true, Color.blue);

            // ...then blit it back again
            // cmd.Blit(tempTexture.Identifier(), cameraColorTargetIdent);

            // don't forget to tell ScriptableRenderContext to actually execute the commands
            context.ExecuteCommandBuffer(cmd);

            // tidy up after ourselves
            // cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }

    // MUST be named "settings" (lowercase) to be shown in the Render Features inspector
    public FeatureSettings settings = new FeatureSettings();

    RenderTargetHandle renderTextureHandle;
    AlonerGrabPass_RenderTex_Pass grabPass;

    /// <inheritdoc/>
    public override void Create()
    {
        grabPass = new AlonerGrabPass_RenderTex_Pass(
            settings.profilerTag,
            settings.WhenToInsert,
            settings.tex);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.IsEnabled)
        {
            // we can do nothing this frame if we want
            return;
        }

        // Gather up and pass any extra information our pass will need.
        // In this case we're getting the camera's color buffer target
        var cameraColorTargetIdent = renderer.cameraColorTarget;
        grabPass.Setup(cameraColorTargetIdent);

        // Ask the renderer to add our pass.
        // Could queue up multiple passes and/or pick passes to use
        renderer.EnqueuePass(grabPass);
    }
}


