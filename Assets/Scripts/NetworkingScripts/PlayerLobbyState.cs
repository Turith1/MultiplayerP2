using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;

public class PlayerLobbyState : NetworkBehaviour
{
    public static PlayerLobbyState LocalPlayer { get; private set; }

    public NetworkVariable<bool> IsReady =
        new NetworkVariable<bool>(false);

    public NetworkVariable<FixedString32Bytes> PlayerName =
        new NetworkVariable<FixedString32Bytes>();

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalPlayer = this;
        }

        IsReady.OnValueChanged += OnReadyChanged;

        StartCoroutine(DelayUIUpdate());
    }

    public override void OnNetworkDespawn()
    {
        IsReady.OnValueChanged -= OnReadyChanged;
    }

    private void OnReadyChanged(bool previous, bool current)
    {
        LobbyUI.Instance.UpdatePlayerReadyState(
            OwnerClientId,
            current
        );
    }

    [ServerRpc]
    public void SetReadyServerRpc(bool ready)
    {
        IsReady.Value = ready;
    }

    private IEnumerator DelayUIUpdate()
    {
        yield return null; // wait 1 frame for UI + scene init

        if (!IsOwner) yield break;
        if (LobbyUI.Instance == null) yield break;

        LobbyUI.Instance.UpdateMainButton();
    }
}
