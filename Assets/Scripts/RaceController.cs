using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class RaceController : NetworkBehaviour
{
    [SerializeField] private TextMeshProUGUI playerMessage;
    [SerializeField] private int totalCheckpoints;

    private List<ulong> racePositions = new();
    private bool raceFinished = false;

    private Dictionary<ulong, int> playerCheckpoint = new Dictionary<ulong, int>();

    public TextMeshProUGUI playerPositionText;

    private void Awake()
    {
        LobbyManager.Instance.raceController = this;
        if(NetworkManager.Singleton.IsServer)
            playerMessage.text = "Your lobby code: " + LobbyManager.Instance.code;
    }

    [ClientRpc]
    public void StartCountdownClientRpc()
    {
        Debug.Log("Countdown started!");
        StartCoroutine(CountDownAndStart());
    }

    private IEnumerator CountDownAndStart()
    {
        playerMessage.text = "3!";
        yield return new WaitForSeconds(1.5f);

        playerMessage.text = "2!";
        yield return new WaitForSeconds(1.5f);

        playerMessage.text = "1!";
        yield return new WaitForSeconds(1.5f);

        playerMessage.text = "GO!";
        LobbyManager.Instance.raceStarted = true;

        yield return new WaitForSeconds(1.5f);
        playerMessage.text = "";
    }

    public void PlayerPassedCheckpoint(ulong clientId, int checkpointIndex)
    {
        if (!playerCheckpoint.ContainsKey(clientId))
            playerCheckpoint.Add(clientId, 0);

        int lastCheckpoint = playerCheckpoint[clientId];

        if (checkpointIndex == 0 && lastCheckpoint == totalCheckpoints)
        {
            PlayerFinished(clientId);
            return;
        }

        playerCheckpoint[clientId] = checkpointIndex;

        UpdatePositions();
    }

    private void UpdatePositions()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        List<KeyValuePair<ulong, int>> sortedPlayers =
            playerCheckpoint
            .OrderByDescending(x => x.Value)
            .ToList();

        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            ulong clientId = sortedPlayers[i].Key;

            SendPositionClientRpc(
                i + 1,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[]
                        {
                            clientId
                        }
                    }
                });
        }
    }

    [ClientRpc]
    private void SendPositionClientRpc(int position, ClientRpcParams clientRpcParams = default)
    {
        playerPositionText.text = position.ToString();
    }

    public void PlayerFinished(ulong winnerId)
    {
        if (raceFinished)
            return;

        raceFinished = true;

        ShowWinnerClientRpc(winnerId);

        EndRace();
    }

    [ClientRpc]
    private void ShowWinnerClientRpc(ulong winnerId)
    {
        ulong myId = NetworkManager.Singleton.LocalClientId;

        if (myId == winnerId)
        {
            playerMessage.text = "YOU WIN!";
        }
        else
        {
            playerMessage.text = "YOU LOSE!";
        }
    }

    private async void EndRace()
    {
        await Task.Delay(2000);

        await LobbyManager.Instance.LeaveLobbyAsync();
    }
}
