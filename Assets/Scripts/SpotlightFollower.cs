using UnityEngine;

public class SpotlightFollower : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra el GameObject padre 'tentaculos guias'")]
    [SerializeField] private Transform tentaculosRoot;
    [Tooltip("La Spot Light que se moverá")]
    [SerializeField] private Transform spotLightTransform;

    [Header("Configuración de Ejes")]
    [Tooltip("True: copia X e Y (mantiene Z). False: copia X y Z (mantiene la altura Y del foco).")]
    [SerializeField] private bool useXY = true;

    /// <summary>
    /// Posiciona la luz en el tentáculo según su índice en la lista (0 al 17).
    /// </summary>
    public void MoveToTentacle(int index)
    {
        if (tentaculosRoot == null || spotLightTransform == null) return;
        if (index < 0 || index >= tentaculosRoot.childCount) return;

        Transform target = tentaculosRoot.GetChild(index);
        SetLightPosition(target.position);
    }

    /// <summary>
    /// Posiciona la luz directamente sobre un Transform específico.
    /// </summary>
    public void MoveToTarget(Transform target)
    {
        if (target == null || spotLightTransform == null) return;
        SetLightPosition(target.position);
    }

    private void SetLightPosition(Vector3 targetPos)
    {
        Vector3 currentPos = spotLightTransform.position;

        if (useXY)
        {
            // Mantiene la coordenada Z original de la luz
            spotLightTransform.position = new Vector3(targetPos.x, targetPos.y, currentPos.z);
        }
        else
        {
            // Mantiene la coordenada Y (altura) original de la luz apuntando al suelo
            spotLightTransform.position = new Vector3(targetPos.x, currentPos.y, targetPos.z);
        }
    }
}