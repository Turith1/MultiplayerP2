using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    private UnityTransport transport;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    public async Task<string> CreateRelay(int maxPlayers)
    {
        Allocation allocation =
            await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

        string joinCode =
            await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        //NetworkManager.Singleton.StartHost();

        return joinCode;
    }

    public async Task JoinRelay(string joinCode)
    {
        JoinAllocation allocation =
            await RelayService.Instance.JoinAllocationAsync(joinCode);

        transport.SetClientRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.HostConnectionData
        );

        //NetworkManager.Singleton.StartClient();
    }
}
