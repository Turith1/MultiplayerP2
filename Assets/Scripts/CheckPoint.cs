using UnityEngine;
using Unity.Netcode;

public class CheckPoint : MonoBehaviour
{
    public int checkpointIndex;
    [SerializeField] private RaceController raceController;

    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        CarController car = other.GetComponent<CarController>();

        if (car == null)
            return;

        raceController.PlayerPassedCheckpoint(car.OwnerClientId,checkpointIndex);
    }
}
