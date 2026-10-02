using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.yellow;
    [Tooltip("Offset a lo largo de la normal de cada vértice, en unidades locales del mesh. " +
             "OJO: cambió de significado respecto a la versión anterior (antes era un % de escala, " +
             "ahora es una distancia absoluta) - puede que necesites retocar este valor por objeto.")]
    [SerializeField] private float outlineWidth = 0.02f;
    [SerializeField] private float emissionIntensity = 2.5f;

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
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;

        // Ya NO escalamos el transform (eso distorsiona mallas con pivote descentrado,
        // como una linterna sostenida de un extremo). El inflado ahora lo hace el
        // shader, offseteando cada vértice a lo largo de su normal.
        outlineObject.transform.localScale = Vector3.one;

        MeshFilter originalMesh = GetComponent<MeshFilter>();
        if (originalMesh == null || originalMesh.sharedMesh == null) return;

        Mesh mesh = originalMesh.sharedMesh;
        outlineObject.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
        Material outlineMat = CreateOutlineMaterial();

        // Si la malla tiene varios submeshes (como la Linterna, con 3 materiales en
        // el MeshRenderer original), hay que llenar TODOS los slots con el mismo
        // material de contorno. Asignar un solo material (renderer.material = ...)
        // a una malla multi-submesh deja los slots sobrantes mal definidos, y puede
        // terminar viéndose sólido en vez de un borde fino.
        int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
        Material[] mats = new Material[subMeshCount];
        for (int i = 0; i < subMeshCount; i++) mats[i] = outlineMat;
        outlineRenderer.sharedMaterials = mats;

        outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        outlineRenderer.receiveShadows = false;
    }

    private Material CreateOutlineMaterial()
    {
        Material mat = new Material(Shader.Find("Custom/OutlineHull"));
        mat.SetColor("_BaseColor", outlineColor);
        mat.SetFloat("_OutlineWidth", outlineWidth);
        mat.SetFloat("_EmissionIntensity", emissionIntensity);
        return mat;
    }

    public void SetOutlineVisible(bool visible)
    {
        if (outlineObject != null)
            outlineObject.SetActive(visible);
    }
}