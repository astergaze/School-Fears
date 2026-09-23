using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 0.02f;

    private GameObject outlineObject;
    private Renderer originalRenderer;

    private void Awake()
    {
        originalRenderer = GetComponent<Renderer>();
        CreateOutlineObject();
        SetOutlineVisible(false);
    }

    private void CreateOutlineObject()
    {
        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one * (1f + outlineWidth);

        MeshFilter originalMesh = GetComponent<MeshFilter>();
        if (originalMesh != null)
        {
            outlineObject.AddComponent<MeshFilter>().mesh = originalMesh.mesh;
        }

        Renderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
        outlineRenderer.material = CreateOutlineMaterial();
        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
    }

    private Material CreateOutlineMaterial()
    {
        // Usa el shader standard pero con Cull Front para que solo se vea el borde
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Front);
        mat.SetColor("_BaseColor", outlineColor);
        mat.SetFloat("_Surface", 0); // Opaque
        mat.enableInstancing = true;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", outlineColor * 2.5f);
        return mat;
    }

    public void SetOutlineVisible(bool visible)
    {
        if (outlineObject != null)
            outlineObject.SetActive(visible);
    }
}