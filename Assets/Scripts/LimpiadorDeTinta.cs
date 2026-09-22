using UnityEngine;

[ExecuteAlways]
public class LimpiadorDeTinta : MonoBehaviour
{
    public Material materialDelPiso;
    public string referenciaPosicion = "_PosicionLuz";
    public string referenciaRadio = "_RadioLuz";
    public float radioDelCirculo = 5f;

    // Añadimos esta variable para saber dónde está el piso
    public float alturaDelPiso = 0f;

    void Update()
    {
        if (materialDelPiso != null)
        {
            // PROYECTAMOS la posición quitando la altura de la luz y usando la del piso
            Vector3 posicionEnElSuelo = new Vector3(transform.position.x, alturaDelPiso, transform.position.z);

            materialDelPiso.SetVector(referenciaPosicion, posicionEnElSuelo);
            materialDelPiso.SetFloat(referenciaRadio, radioDelCirculo);
        }
    }
}