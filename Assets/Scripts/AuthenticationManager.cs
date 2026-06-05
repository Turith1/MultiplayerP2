using UnityEngine;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;

public class AuthenticationManager : MonoBehaviour
{

    async void Start()
    {
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log(AuthenticationService.Instance.PlayerId);

        SceneManager.LoadScene("MainMenu");
    }
}
