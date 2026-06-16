using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class CarController : NetworkBehaviour
{
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private float drag = 4f;

    private CharacterController controller;

    private float currentSpeed;
    private float throttleInput;
    private float steeringInput;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        SimulateVehicle();
    }

    private void SimulateVehicle()
    {
        // Acceleration
        currentSpeed += throttleInput * acceleration * Time.fixedDeltaTime;

        // Natural deceleration
        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            0f,
            drag * Time.fixedDeltaTime);

        currentSpeed = Mathf.Clamp(
            currentSpeed,
            -maxSpeed * 0.5f,
            maxSpeed);

        // Steering scales with speed
        float speedFactor = Mathf.Abs(currentSpeed) / maxSpeed;

        transform.Rotate(
            Vector3.up,
            steeringInput * turnSpeed * speedFactor * Time.fixedDeltaTime);

        Vector3 movement =
            transform.forward * currentSpeed * Time.fixedDeltaTime;

        controller.Move(movement);
    }

    [ServerRpc]
    public void SubmitInputServerRpc(float throttle, float steering)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steeringInput = Mathf.Clamp(steering, -1f, 1f);
    }
}
