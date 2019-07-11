using UnityEngine;

//[ExecuteInEditMode]
public class BloomRenderer : MonoBehaviour
{
    private bool _enable;
    private int _renderQueue;

    [HideInInspector]
    public Renderer bloomRenderer;
    public Material bloomMaterial;

    void Awake()
    {
        bloomRenderer = GetComponent<Renderer>();
        if (bloomRenderer == null)
        {
            _enable = false;
            return;
        }

        _enable = true;
        Material material = bloomRenderer.material;
        if (material != null)
        {
            _renderQueue = material.shader.renderQueue;
            if (bloomMaterial == null)
            {
                CreateMaterial(material);
            }
        }
    }

    void OnEnable()
    {
        if (_enable)
        {
            BloomSystem.instance.Add(_renderQueue, this);
        }
    }

    void Start()
    {
        if (_enable)
        {
            //BloomSystem.instance.Add(_renderQueue, this);
        }
    }

    void OnDisable()
    {
        if (_enable)
        {
            BloomSystem.instance.Remove(_renderQueue, this);
        }
    }

    void OnDestroy()
    {
        DestroyImmediate(bloomMaterial, true);
    }

    Material CreateMaterial(Material originMat)
    {
        if (originMat == null)
        {
            Debug.Log("Missing origin material in " + ToString());
            return null;
        }

        Shader originShader = originMat.shader;
        if (originShader == null)
        {
            Debug.Log("Missing origin shader in " + ToString());
            return null;
        }

        Shader bloomShader = Shader.Find(string.Format("{0} (Bloom)", originShader.name));
        if (bloomShader == null)
        {
            Debug.Log("Missing bloom shader in " + ToString());
            bloomShader = originShader;
        }

        bloomMaterial = new Material(originMat);
        bloomMaterial.shader = bloomShader;
        bloomMaterial.hideFlags = HideFlags.DontSave;

        return bloomMaterial;
    }
}
