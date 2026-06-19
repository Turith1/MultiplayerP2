using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        LobbyManager.Instance.spawnPoints = spawnPoints;
    }
}
