using UnityEngine;

/// <summary>
/// Cada frame, mide la distancia (en el plano XZ) entre el jugador y la posición
/// lógica de la luz (TentaclePathManager.GetGroundIndicatorPosition()). Si esa
/// distancia supera "Max Follow Distance" durante más de "Mismatch Grace Time"
/// segundos seguidos, dispara Game Over.
///
/// Importante: "Max Follow Distance" debería coincidir con "Radio Del Circulo" de
/// LimpiadorDeTinta, así el límite de muerte es exactamente el borde del círculo de
/// tinta que el jugador ve en el piso - sin esto, "ver que estás en el círculo" y
/// "estar a salvo" podían no coincidir (por ejemplo, con pisos/tiles más chicos que
/// el radio visual del círculo).
///
/// Enganchar TentaclePathManager.onChaseStarted -> FloorSyncGameOver.Activate() en el Inspector.
/// </summary>
public class FloorSyncGameOver : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío (o queda 'Missing' por venir de otra escena), se busca en runtime por tag 'Player' - así funciona tanto probando la escena suelta como jugando el flujo completo desde el menú, donde el Player es el singleton persistente de otra escena.")]
    [SerializeField] private Transform player;
    [SerializeField] private TentaclePathManager pathManager;

    [Header("Distancia de Seguimiento")]
    [Tooltip("Distancia XZ máxima tolerada entre el jugador y la luz. Igualar al 'Radio Del Circulo' de LimpiadorDeTinta.")]
    [SerializeField] private float maxFollowDistance = 5f;
    [Tooltip("Tiempo (seg) que se tolera estar fuera del radio antes de matar, para dar margen a transiciones cortas")]
    [SerializeField] private float mismatchGraceTime = 2.5f;

    private bool active = false;
    private float mismatchTimer = 0f;

    public void Activate() => active = true;

    private void Update()
    {
        // El Player puede ser el singleton persistente de otra escena - si la referencia
        // serializada está rota (Missing) o nunca se asignó, lo buscamos por tag.
        if (player == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) player = playerGO.transform;
        }

        if (!active || player == null || pathManager == null) return;

        Vector3? lightGroundPos = pathManager.GetGroundIndicatorPosition();
        if (lightGroundPos == null) { mismatchTimer = 0f; return; }

        float xzDistance = Vector2.Distance(
            new Vector2(player.position.x, player.position.z),
            new Vector2(lightGroundPos.Value.x, lightGroundPos.Value.z));

        if (Time.frameCount % 15 == 0)
        {
            Debug.Log($"[FloorSync] distXZ={xzDistance:F2} maxFollowDistance={maxFollowDistance} mismatchTimer={mismatchTimer:F2}");
        }

        if (xzDistance > maxFollowDistance)
        {
            mismatchTimer += Time.deltaTime;
            if (mismatchTimer >= mismatchGraceTime)
            {
                Debug.LogWarning($"[FloorSync] GAME OVER disparado. distXZ={xzDistance:F2} > maxFollowDistance={maxFollowDistance}");
                pathManager.ExternalGameOver();
            }
        }
        else
        {
            mismatchTimer = 0f;
        }
    }
}