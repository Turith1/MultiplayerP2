using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Netcode;
using System.Linq;
using Unity.Services.Authentication;
using System;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public Lobby currentLobby;

    public bool IsHost;
    public string code;
    private bool heartbeatRunning;
    private bool _isLeavingLobby;
    private bool isJoiningLobby = false;
    private bool isCreatingLobby = false;
    private bool isGameStarting = false;

    [SerializeField]
    private int maxPlayers = 4;
    [SerializeField]
    private GameObject jotunCharacterPrefab;
    [SerializeField]
    private GameObject priestCharacterPrefab;
    [SerializeField]
    private GameObject wisper;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public async Task CreateLobby()
    {
        if (isCreatingLobby)
            return;

        isCreatingLobby = true;

        try
        {
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                "My Lobby",
                4
            );

            Debug.Log($"Lobby created: {currentLobby.LobbyCode}");
            code = currentLobby.LobbyCode;

            // 1. Create Relay
            string relayCode = await RelayManager.Instance.CreateRelay(maxPlayers);

            NetworkManager.Singleton.StartHost();

            HeartBeat();

            // 2. Store relay code inside Lobby data (VERY important)
            var updateOptions = new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                {
                    "relayCode",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        relayCode
                    )
                }
            }
            };

            await LobbyService.Instance.UpdateLobbyAsync(currentLobby.Id, updateOptions);

            // 3. Load lobby scene (NGO already running)
            //SceneManager.LoadScene("LobbyScene");
            NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);

            IsHost = true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        finally
        {
            isCreatingLobby = false;
        }
    }

    public async Task JoinLobby(string code)
    {
        if (isJoiningLobby)
            return;

        isJoiningLobby = true;

        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

            Debug.Log($"Joined lobby: {currentLobby.Name}");

            // 1. Get relay code from lobby
            string relayCode = currentLobby.Data["relayCode"].Value;

            // 3. Join Relay (NGO client starts here)
            await RelayManager.Instance.JoinRelay(relayCode);

            NetworkManager.Singleton.StartClient();

            IsHost = false;

            // 2. Load lobby scene first (optional but cleaner UX)
            //SceneManager.LoadScene("LobbyScene");
            //NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        finally
        {
            isJoiningLobby = false;
        }
    }

    public async Task LeaveLobbyAsync()
    {
        if (_isLeavingLobby)
            return;

        _isLeavingLobby = true;

        try
        {
            Debug.Log("Leaving Lobby...");

            if (NetworkManager.Singleton.IsHost)
            {
                if (currentLobby != null)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
            }
            else
            {
                if (currentLobby != null)
                {
                    await LobbyService.Instance.RemovePlayerAsync(
                        currentLobby.Id,
                        AuthenticationService.Instance.PlayerId
                    );
                }
            }

            ShutdownNetwork();

            currentLobby = null;

            await Task.Delay(100);

            SceneManager.LoadScene("MainMenu");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Lobby leave failed: {e.Message}");
        }
        finally
        {
            _isLeavingLobby = false;
        }
    }

    async void HeartBeat()
    {
        if (heartbeatRunning)
            return;

        heartbeatRunning = true;

        while (currentLobby != null)
        {
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Heartbeat failed: {e.Message}");
                break;
            }

            await Task.Delay(15000);
        }

        heartbeatRunning = false;
    }

    public bool AreAllPlayersReady()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId == NetworkManager.Singleton.LocalClientId)
                continue;

            if (client.PlayerObject == null)
                return false;

            PlayerLobbyState playerState =
                client.PlayerObject.GetComponent<PlayerLobbyState>();

            if (playerState == null)
                return false;

            if (!playerState.IsReady.Value)
            {
                return false;
            }
        }

        return true;
    }


    public void TryStartGame()
    {
        if (isGameStarting)
            return;

        if (!NetworkManager.Singleton.IsHost)
            return;

        isGameStarting = true;

        try
        {
            Debug.Log("HOST TRYING TO START GAME");

            if (!AreAllPlayersReady())
            {
                Debug.Log("Not everyone is ready.");
                return;
            }

            List<ulong> clients =
                NetworkManager.Singleton.ConnectedClientsIds.ToList();

            ulong jotunClientId =
                clients[UnityEngine.Random.Range(0, clients.Count)];

            foreach (ulong clientId in clients)
            {
                SpawnGameplayCharacter(clientId, jotunClientId);
            }
        }
        finally
        {
            isGameStarting = false;
        }
    }

    public void SpawnGameplayCharacter(ulong clientId, ulong jotunClientId)
    {
        NetworkObject oldPlayer =
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (oldPlayer != null)
        {
            oldPlayer.Despawn(true);
        }

        GameObject prefabToSpawn =
            (clientId == jotunClientId)
                ? jotunCharacterPrefab
                : priestCharacterPrefab;

        GameObject obj = Instantiate(prefabToSpawn);

        obj.GetComponent<NetworkObject>()
            .SpawnAsPlayerObject(clientId, true);

        HideCursorClientRpc();
    }

    [ClientRpc]
    private void HideCursorClientRpc()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShutdownNetwork()
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    private async void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null)
            return;

        // Ignore disconnects from other players
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        // Ignore intentional leave
        if (_isLeavingLobby)
            return;

        Debug.Log("Unexpected disconnect detected.");

        await HandleConnectionLostAsync();
    }


    private async Task HandleConnectionLostAsync()
    {
        try
        {
            ShutdownNetwork();

            currentLobby = null;

            await Task.Yield();

            SceneManager.LoadScene("MainMenu");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
