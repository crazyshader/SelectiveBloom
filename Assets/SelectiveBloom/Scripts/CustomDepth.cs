using UnityEngine;
using UnityEngine.Rendering;


public class CustomDepth : MonoBehaviour
{
    private Camera m_Camera;
    private RenderTexture m_ColorBuffer;
    private RenderTexture m_DepthBuffer;

    private CommandBuffer m_DepthCommand;
    private CommandBuffer m_ColorCommand;

    private CameraEvent colorCameraEvent = CameraEvent.AfterForwardAlpha;
    private CameraEvent depthCameraEvent = CameraEvent.AfterSkybox;

    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        //m_Camera.depthTextureMode = DepthTextureMode.Depth;
    }

    void OnEnable()
    {
        m_ColorBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        m_ColorBuffer.name = "Color RenderTexture";
        m_DepthBuffer = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
        m_DepthBuffer.name = "Depth RenderTexture";
        m_Camera.SetTargetBuffers(m_ColorBuffer.colorBuffer, m_DepthBuffer.depthBuffer);

        m_DepthCommand = new CommandBuffer();
        m_DepthCommand.name = "Set depth texture";
        m_DepthCommand.SetGlobalTexture("_DepthTexture", m_DepthBuffer);
        m_Camera.AddCommandBuffer(depthCameraEvent, m_DepthCommand);

        m_ColorCommand = new CommandBuffer();
        m_ColorCommand.name = "Blit to Back buffer";
        m_ColorCommand.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        m_ColorCommand.Blit(m_ColorBuffer, BuiltinRenderTextureType.CurrentActive);
        m_ColorCommand.SetGlobalTexture("_ColorTexture", m_ColorBuffer);
        m_Camera.AddCommandBuffer(colorCameraEvent, m_ColorCommand);
    }

    void OnDisable()
    {
        if (m_DepthCommand != null)
        {
            m_Camera.RemoveCommandBuffer(depthCameraEvent, m_DepthCommand);
            m_DepthCommand.Dispose();
            m_DepthCommand = null;
        }
        if (m_ColorCommand != null)
        {
            m_Camera.RemoveCommandBuffer(colorCameraEvent, m_ColorCommand);
            m_ColorCommand.Dispose();
            m_ColorCommand = null;
        }

        if (m_DepthBuffer != null)
        {
            RenderTexture.ReleaseTemporary(m_DepthBuffer);
            m_DepthBuffer = null;
        }
        if (m_ColorBuffer != null)
        {
            RenderTexture.ReleaseTemporary(m_ColorBuffer);
            m_ColorBuffer = null;
        }
    }
}
