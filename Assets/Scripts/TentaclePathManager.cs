using System.Collections;
using UnityEngine;

public class TentaclePathManager : MonoBehaviour
{
    [Header("Referencias Principales")]
    [Tooltip("Arrastra el GameObject 'tentaculos guias'")]
    [SerializeField] private Transform tentaculosRoot;
    [Tooltip("Arrastra la Spot Light")]
    [SerializeField] private Transform spotLightTransform;

    [Header("Indicador Visual en Suelo")]
    [Tooltip("Material para el círculo/plano que marca el tentáculo activo")]
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private Vector3 indicatorScale = new Vector3(1.5f, 0.01f, 1.5f);
    [SerializeField] private float groundYOffset = 0.05f;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private bool useHorizontalXZ = true;

    private int currentStepIndex = 0;
    private Coroutine moveRoutine;
    private GameObject groundIndicator;

    private void Start()
    {
        CreateGroundIndicator();
        InitializeNodes();
        UpdatePositions(instant: true);
    }

    private void CreateGroundIndicator()
    {
        // Genera un plano circular/cuadrado en el suelo como feedback visual
        groundIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        groundIndicator.name = "ActiveStep_Highlight";

        // Removemos el collider para que no bloquee ni interfiera con el CharacterController
        Collider col = groundIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        if (indicatorMaterial != null)
        {
            groundIndicator.GetComponent<MeshRenderer>().material = indicatorMaterial;
        }

        groundIndicator.transform.localScale = indicatorScale;
    }

    private void InitializeNodes()
    {
        if (tentaculosRoot == null) return;

        for (int i = 0; i < tentaculosRoot.childCount; i++)
        {
            Transform child = tentaculosRoot.GetChild(i);

            // Aseguramos que tenga BoxCollider como trigger
            BoxCollider box = child.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = child.gameObject.AddComponent<BoxCollider>();
            }

            box.isTrigger = true;
            // Ajustamos el tamaño y centro para que toque el suelo donde pasa el player
            box.size = new Vector3(2f, 2.5f, 2f);
            box.center = new Vector3(0, 1f, 0);

            TentacleStepNode node = child.GetComponent<TentacleStepNode>();
            if (node == null)
                node = child.gameObject.AddComponent<TentacleStepNode>();

            node.Init(i, this);
        }
    }

    public void OnPlayerReachedStep(int stepIndex)
    {
        if (stepIndex == currentStepIndex)
        {
            currentStepIndex++;

            if (currentStepIndex < tentaculosRoot.childCount)
            {
                UpdatePositions(!smoothTransition);
            }
            else
            {
                OnPathCompleted();
            }
        }
    }

    private void UpdatePositions(bool instant)
    {
        if (tentaculosRoot == null) return;

        Transform target = tentaculosRoot.GetChild(currentStepIndex);
        Vector3 targetPos = target.position;

        // 1. Posición de la Spot Light (mantiene altura de techo)
        Vector3 lightDest = spotLightTransform != null ? spotLightTransform.position : Vector3.zero;
        if (useHorizontalXZ && spotLightTransform != null)
        {
            lightDest = new Vector3(targetPos.x, spotLightTransform.position.y, targetPos.z);
        }

        // 2. Posición del indicador de suelo (pegado a la base del nodo)
        Vector3 groundDest = new Vector3(targetPos.x, targetPos.y + groundYOffset, targetPos.z);

        if (instant)
        {
            if (spotLightTransform != null) spotLightTransform.position = lightDest;
            if (groundIndicator != null) groundIndicator.transform.position = groundDest;
        }
        else
        {
            if (moveRoutine != null) StopCoroutine(moveRoutine);
            moveRoutine = StartCoroutine(MoveSmoothly(lightDest, groundDest));
        }
    }

    private IEnumerator MoveSmoothly(Vector3 lightDest, Vector3 groundDest)
    {
        while (groundIndicator != null && Vector3.Distance(groundIndicator.transform.position, groundDest) > 0.05f)
        {
            if (spotLightTransform != null)
            {
                spotLightTransform.position = Vector3.MoveTowards(
                    spotLightTransform.position, 
                    lightDest, 
                    moveSpeed * Time.deltaTime
                );
            }

            groundIndicator.transform.position = Vector3.MoveTowards(
                groundIndicator.transform.position, 
                groundDest, 
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (spotLightTransform != null) spotLightTransform.position = lightDest;
        if (groundIndicator != null) groundIndicator.transform.position = groundDest;
    }

    private void OnPathCompleted()
    {
        Debug.Log("Camino completado.");
        if (groundIndicator != null) Destroy(groundIndicator);
    }
}