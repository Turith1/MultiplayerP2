using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerInputs : NetworkBehaviour
{
    private MultiplayerP2 controls;
    private CarController car;

    private Vector2 currentInput;

    [SerializeField] private CinemachineCamera cineCam;

    private void Awake()
    {
        controls = new MultiplayerP2();
        car = GetComponent<CarController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            cineCam.gameObject.SetActive(true);

            controls.Player.Move.performed += OnMove;
            controls.Player.Move.canceled += OnMove;

            controls.Enable();
        }
        else
        {
            cineCam.gameObject.SetActive(false);
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        currentInput = ctx.ReadValue<Vector2>();

        car.SubmitInputServerRpc(
            currentInput.y,
            currentInput.x);
    }

    public override void OnNetworkDespawn()
    {
        controls.Player.Move.performed -= OnMove;
        controls.Player.Move.canceled -= OnMove;

        controls.Disable();
    }
}
