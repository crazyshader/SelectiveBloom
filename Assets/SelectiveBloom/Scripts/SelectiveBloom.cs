using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BloomSystem
{
    static BloomSystem m_Instance; // singleton
    static public BloomSystem instance
    {
        get
        {
            if (m_Instance == null)
                m_Instance = new BloomSystem();
            return m_Instance;
        }
    }

    internal SortedDictionary<int, List<BloomRenderer>> m_BloomObjs = new SortedDictionary<int, List<BloomRenderer>>();

    public void Add(int renderQueue, BloomRenderer o)
    {
        Remove(renderQueue, o);
        List<BloomRenderer> bloomRendererList;
        if (!m_BloomObjs.TryGetValue(renderQueue, out bloomRendererList))
        {
            bloomRendererList = new List<BloomRenderer>();
            m_BloomObjs.Add(renderQueue, bloomRendererList);
        }
        bloomRendererList.Add(o);
        Debug.Log("added bloom effect " + o.gameObject.name);
    }

    public void Remove(int renderQueue, BloomRenderer o)
    {
        List<BloomRenderer> bloomRendererList;
        if (m_BloomObjs.TryGetValue(renderQueue, out bloomRendererList))
        {
            bloomRendererList.Remove(o);
            Debug.Log("removed bloom effect " + o.gameObject.name);
        }
    }
}

//[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Custom/SelectiveBloom")]
public class SelectiveBloom : MonoBehaviour
{
    #region Public Properties

    /// Prefilter threshold (gamma-encoded)
    /// Filters out pixels under this level of brightness.
    public float thresholdGamma
    {
        get { return Mathf.Max(_threshold, 0); }
        set { _threshold = value; }
    }

    /// Prefilter threshold (linearly-encoded)
    /// Filters out pixels under this level of brightness.
    public float thresholdLinear
    {
        get { return GammaToLinear(thresholdGamma); }
        set { _threshold = LinearToGamma(value); }
    }

    [SerializeField]
    [Tooltip("Filters out pixels under this level of brightness.")]
    float _threshold = 0.8f;

    /// Soft-knee coefficient
    /// Makes transition between under/over-threshold gradual.
    public float softKnee
    {
        get { return _softKnee; }
        set { _softKnee = value; }
    }

    [SerializeField, Range(0, 1)]
    [Tooltip("Makes transition between under/over-threshold gradual.")]
    float _softKnee = 0.5f;

    /// Bloom radius
    /// Changes extent of veiling effects in a screen
    /// resolution-independent fashion.
    public float radius
    {
        get { return _radius; }
        set { _radius = value; }
    }

    [SerializeField, Range(1, 7)]
    [Tooltip("Changes extent of veiling effects\n" +
             "in a screen resolution-independent fashion.")]
    float _radius = 2.5f;

    /// Bloom intensity
    /// Blend factor of the result image.
    public float intensity
    {
        get { return Mathf.Max(_intensity, 0); }
        set { _intensity = value; }
    }

    [SerializeField]
    [Tooltip("Blend factor of the result image.")]
    float _intensity = 0.8f;

    /// High quality mode
    /// Controls filter quality and buffer resolution.
    public bool highQuality
    {
        get { return _highQuality; }
        set { _highQuality = value; }
    }

    [SerializeField]
    [Tooltip("Controls filter quality and buffer resolution.")]
    bool _highQuality = true;

    /// Anti-flicker filter
    /// Reduces flashing noise with an additional filter.
    [SerializeField]
    [Tooltip("Reduces flashing noise with an additional filter.")]
    bool _antiFlicker = true;

    public bool antiFlicker
    {
        get { return _antiFlicker; }
        set { _antiFlicker = value; }
    }

    [SerializeField, Range(0, 1)]
    [Tooltip("Base factor of the result image.")]
    float _baseIntensity = 1.0f;

    /// Base intensity
    /// Base factor of the result image.
    public float baseIntensity
    {
        get { return Mathf.Max(_baseIntensity, 0); }
        set { _baseIntensity = value; }
    }

    #endregion

    #region Private Members

    [SerializeField, HideInInspector]
    Shader _shader;

    Material _material;

    const int kMaxIterations = 16;
    RenderTexture[] _blurBuffer1 = new RenderTexture[kMaxIterations];
    RenderTexture[] _blurBuffer2 = new RenderTexture[kMaxIterations];

    private Camera m_Camera;
    private CommandBuffer m_BloomBuffer;
    private RenderTexture m_BloomRT;
    private BloomSystem m_BloomSystem;

    float LinearToGamma(float x)
    {
#if UNITY_5_3_OR_NEWER
        return Mathf.LinearToGammaSpace(x);
#else
            if (x <= 0.0031308f)
                return 12.92f * x;
            else
                return 1.055f * Mathf.Pow(x, 1 / 2.4f) - 0.055f;
#endif
    }

    float GammaToLinear(float x)
    {
#if UNITY_5_3_OR_NEWER
        return Mathf.GammaToLinearSpace(x);
#else
            if (x <= 0.04045f)
                return x / 12.92f;
            else
                return Mathf.Pow((x + 0.055f) / 1.055f, 2.4f);
#endif
    }

    #endregion

    #region MonoBehaviour Functions
    void Awake()
    {
        m_Camera = GetComponent<Camera>();
        m_BloomSystem = BloomSystem.instance;
    }

    void OnEnable()
    {
        var shader = _shader ? _shader : Shader.Find("Hidden/PostProcessing/SelectiveBloom");
        _material = new Material(shader);
        _material.hideFlags = HideFlags.DontSave;

        m_BloomBuffer = new CommandBuffer();
        m_BloomBuffer.name = "Bloom Buffer";
        m_Camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, m_BloomBuffer);
    }

    void OnDisable()
    {
        DestroyImmediate(_material);

        if (m_BloomBuffer != null)
        {
            m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, m_BloomBuffer);
            m_BloomBuffer.Dispose();
            m_BloomBuffer = null;
        }

        if (m_BloomRT != null)
        {
            RenderTexture.ReleaseTemporary(m_BloomRT);
            m_BloomRT = null;
        }
    }

    private void OnPreCull()
    {
        if (m_BloomRT != null)
            RenderTexture.ReleaseTemporary(m_BloomRT);
        m_BloomRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
        m_BloomRT.name = "Bloom RenderTexture";
    }

    private void OnPreRender()
    {
        m_BloomBuffer.Clear();
        m_BloomBuffer.SetRenderTarget(m_BloomRT);
        m_BloomBuffer.ClearRenderTarget(true, true, Color.clear);

        foreach (var bloomList in m_BloomSystem.m_BloomObjs)
        {
            List<BloomRenderer> bloomRendererList = bloomList.Value;
            foreach (var bloomObj in bloomRendererList)
            {
                Material glowMat = bloomObj.bloomMaterial;
                if (glowMat != null)
                {
                    m_BloomBuffer.DrawRenderer(bloomObj.bloomRenderer, glowMat, 0, 0);
                }
            }
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (m_BloomRT == null) return;

        var useRGBM = Application.isMobilePlatform;

        // source texture size
        var tw = m_BloomRT.width;
        var th = m_BloomRT.height;

        // halve the texture size for the low quality mode
        if (!_highQuality)
        {
            tw /= 2;
            th /= 2;
        }

        // blur buffer format
        var rtFormat = useRGBM ?
            RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;

        // determine the iteration count
        var logh = Mathf.Log(th, 2) + _radius - 8;
        var logh_i = (int)logh;
        var iterations = Mathf.Clamp(logh_i, 1, kMaxIterations);

        // update the shader properties
        var lthresh = thresholdLinear;
        _material.SetFloat("_Threshold", lthresh);

        var knee = lthresh * _softKnee + 1e-5f;
        var curve = new Vector3(lthresh - knee, knee * 2, 0.25f / knee);
        _material.SetVector("_Curve", curve);

        var pfo = !_highQuality && _antiFlicker;
        _material.SetFloat("_PrefilterOffs", pfo ? -0.5f : 0.0f);

        _material.SetFloat("_SampleScale", 0.5f + logh - logh_i);
        _material.SetFloat("_Intensity", intensity);
        _material.SetFloat("_baseIntensity", baseIntensity);

        // prefilter pass
        var prefiltered = RenderTexture.GetTemporary(tw, th, 0, rtFormat);
        var pass = _antiFlicker ? 1 : 0;
        Graphics.Blit(m_BloomRT, prefiltered, _material, pass);

        // construct a mip pyramid
        var last = prefiltered;
        for (var level = 0; level < iterations; level++)
        {
            _blurBuffer1[level] = RenderTexture.GetTemporary(
                last.width / 2, last.height / 2, 0, rtFormat
            );

            pass = (level == 0) ? (_antiFlicker ? 3 : 2) : 4;
            Graphics.Blit(last, _blurBuffer1[level], _material, pass);

            last = _blurBuffer1[level];
        }

        // upsample and combine loop
        for (var level = iterations - 2; level >= 0; level--)
        {
            var basetex = _blurBuffer1[level];
            _material.SetTexture("_BaseTex", basetex);

            _blurBuffer2[level] = RenderTexture.GetTemporary(
                basetex.width, basetex.height, 0, rtFormat
            );

            pass = _highQuality ? 6 : 5;
            Graphics.Blit(last, _blurBuffer2[level], _material, pass);
            last = _blurBuffer2[level];
        }

        // finish process
        _material.SetTexture("_BaseTex", m_BloomRT);
        pass = _highQuality ? 8 : 7;
        // 需要注意的地方
        //Graphics.Blit(last, prefiltered, _material, pass);
        //Graphics.Blit(prefiltered, destination, _material, 9);
        Graphics.Blit(last, destination, _material, pass);

        // release the temporary buffers
        for (var i = 0; i < kMaxIterations; i++)
        {
            if (_blurBuffer1[i] != null)
                RenderTexture.ReleaseTemporary(_blurBuffer1[i]);

            if (_blurBuffer2[i] != null)
                RenderTexture.ReleaseTemporary(_blurBuffer2[i]);

            _blurBuffer1[i] = null;
            _blurBuffer2[i] = null;
        }

        RenderTexture.ReleaseTemporary(prefiltered);
    }

    #endregion
}
