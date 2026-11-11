using UnityEngine;
using UnityEngine.AI;

public class RectorGlideMotion : MonoBehaviour
{
    public NavMeshAgent agent;

    [Header("Balanceo al moverse")]
    public float bobHeight   = 0.04f;  // sube y baja
    public float swayAngle   = 3f;     // roll izquierda/derecha
    public float strideSpeed = 1.2f;   // ciclos por metro recorrido

    [Header("Inclinación")]
    public float runSpeed    = 3.3f;   // entre patrulla (2.0) y persecución (4.6)
    public float leanChase   = 10f;    // grados hacia adelante al perseguir
    public float leanPatrol  = 3f;

    [Header("Suavizado")]
    public float blendSpeed  = 6f;

    Vector3 basePos;
    Quaternion baseRot;
    float phase, blend, lean;

    void Reset() => agent = GetComponentInParent<NavMeshAgent>();

    void Start()
    {
        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
        basePos = transform.localPosition;
        baseRot = transform.localRotation;
    }

    void LateUpdate()
    {
        if (agent == null) return;

        float v = agent.velocity.magnitude;
        float moving = v > 0.1f ? 1f : 0f;
        blend = Mathf.MoveTowards(blend, moving, blendSpeed * Time.deltaTime);

        float targetLean = v >= runSpeed ? leanChase : (moving > 0f ? leanPatrol : 0f);
        lean = Mathf.Lerp(lean, targetLean, blendSpeed * Time.deltaTime);

        phase += v * strideSpeed * Time.deltaTime * Mathf.PI * 2f;

        float bob  = Mathf.Abs(Mathf.Sin(phase)) * bobHeight * blend;
        float roll = Mathf.Sin(phase) * swayAngle * blend;

        transform.localPosition = basePos + Vector3.up * bob;
        transform.localRotation = baseRot * Quaternion.Euler(lean, 0f, roll);
    }
}