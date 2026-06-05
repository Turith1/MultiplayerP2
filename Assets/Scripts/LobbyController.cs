using UnityEngine;
using TMPro;

public class LobbyController : MonoBehaviour
{

    [SerializeField]
    TMP_InputField lobbyCodeInput;

    LobbyManager lobbyManager;

    private void Start()
    {
        lobbyManager = FindAnyObjectByType<LobbyManager>();
    }

    public async void CreateLobby()
    {
        await lobbyManager.CreateLobby();
    }

    public async void JoinLobby()
    {
        Debug.Log(lobbyCodeInput.text);
        await lobbyManager.JoinLobby(lobbyCodeInput.text);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
