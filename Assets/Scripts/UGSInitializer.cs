using UnityEngine;
using Unity.Services.Core;

public class UGSInitializer : MonoBehaviour
{

    private async void Awake()
    {
        await UnityServices.InitializeAsync();
    }
}
