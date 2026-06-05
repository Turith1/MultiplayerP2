using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.UI;
using System.Collections;
//using StarterAssets;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    public Button mainButton;
    public GameObject escUI;
    public GameObject cam;
    private bool isUIOpen = false;
    //public StarterAssetsInputs input;
    //public ClientPlayerMove movement;
    [SerializeField] private TMP_Text mainButtonText;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text lobbyCode;


    private void OnEnable()
    {
        StartCoroutine(InitLobbyUI());
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;


        NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
    }

    public void OnStartGamePressed()
    {
        LobbyManager.Instance.TryStartGame();
    }

    public void UpdatePlayerReadyState(
        ulong clientId,
        bool ready
    )
    {
        Debug.Log($"Player {clientId} ready: {ready}");

        UpdateMainButton();
        
    }

    public void OnMainButtonPressed()
    {
        // HOST
        if (NetworkManager.Singleton.IsHost)
        {
            if (LobbyManager.Instance.AreAllPlayersReady())
            {
                LobbyManager.Instance.TryStartGame();
            }

            return;
        }

        // CLIENT
        bool currentReady =
            PlayerLobbyState.LocalPlayer.IsReady.Value;

        PlayerLobbyState.LocalPlayer.SetReadyServerRpc(!currentReady);
    }

    public void UpdateMainButton()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            if (LobbyManager.Instance.AreAllPlayersReady())
            {
                buttonText.text = "Start Game";
            }
            else
            {
                buttonText.text = "Waiting...";
            }
        }
        else
        {
            var player = PlayerLobbyState.LocalPlayer;

            if (player == null)
                return;

            mainButtonText.text =
                player.IsReady.Value
                    ? "Unready"
                    : "Ready";
        }
    }

    private void OnClientChanged(ulong clientId)
    {
        UpdateMainButton();
    }

    private IEnumerator InitLobbyUI()
    {
        yield return new WaitUntil(() => LobbyManager.Instance != null);
        yield return new WaitUntil(() => PlayerLobbyState.LocalPlayer != null);
        yield return new WaitUntil(() => !string.IsNullOrEmpty(LobbyManager.Instance.code));

        UpdateMainButton();

        lobbyCode.text =
            "Lobby code: " + LobbyManager.Instance.code;
    }

    public void EscPressed()
    {
        isUIOpen = !isUIOpen;

        escUI.SetActive(isUIOpen);

        Cursor.visible = isUIOpen;

        Cursor.lockState = isUIOpen
            ? CursorLockMode.Confined
            : CursorLockMode.Locked;

        //movement.enabled = !isUIOpen;

        //input.esc = false;
    }

    public async void LeaveLobby()
    {
        await LobbyManager.Instance.LeaveLobbyAsync();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
    }
}
