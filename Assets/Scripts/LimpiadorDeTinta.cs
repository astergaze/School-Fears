using UnityEngine;

[ExecuteAlways]
public class LimpiadorDeTinta : MonoBehaviour
{
    public Material materialDelPiso;
    public string referenciaPosicion = "_PosicionLuz";
    public string referenciaRadio = "_RadioLuz";
    public float radioDelCirculo = 5f;

    [Tooltip("Si se asigna, se usa la posición de este Transform en vez de la propia. " +
             "Asignar acá el mismo 'Spot Light Transform' que mueve TentaclePathManager, " +
             "para que el círculo de tinta siga a la luz real cuando se mueve por el camino.")]
    public Transform fuenteDeLuz;

    // Añadimos esta variable para saber dónde está el piso
    public float alturaDelPiso = 0f;

    void Update()
    {
        if (materialDelPiso != null)
        {
            Transform origen = fuenteDeLuz != null ? fuenteDeLuz : transform;

            // PROYECTAMOS la posición quitando la altura de la luz y usando la del piso
            Vector3 posicionEnElSuelo = new Vector3(origen.position.x, alturaDelPiso, origen.position.z);

            materialDelPiso.SetVector(referenciaPosicion, posicionEnElSuelo);
            materialDelPiso.SetFloat(referenciaRadio, radioDelCirculo);
        }
    }
}