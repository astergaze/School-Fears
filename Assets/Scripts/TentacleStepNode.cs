using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TentacleStepNode : MonoBehaviour
{
    public int StepIndex { get; private set; }
    private TentaclePathManager manager;

    public void Init(int index, TentaclePathManager pathManager)
    {
        StepIndex = index;
        manager = pathManager;
        
        // Nos aseguramos de que sea Trigger para que el CharacterController lo atraviese y active
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detecta si es el Player (por componente o por tag)
        if (other.GetComponent<Player>() != null || other.CompareTag("Player"))
        {
            manager.OnPlayerReachedStep(StepIndex);
        }
    }
}