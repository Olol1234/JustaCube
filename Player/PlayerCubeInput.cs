using UnityEngine;

// The player "brain": reads the InputReader and asks the CubeRoller to roll.
// Directions are relative to the camera, so "up" always rolls away from the view.
[RequireComponent(typeof(CubeRoller))]
public class PlayerCubeInput : MonoBehaviour
{
    [SerializeField] private InputReader input;
    [SerializeField] private CubeMovementSettings settings;
    [SerializeField] private Transform cameraTransform;

    private CubeRoller roller;

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void OnEnable()
    {
        if (input == null) return;
        input.Enable();
        input.JumpPerformed += OnJump;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.JumpPerformed -= OnJump;
        input.Disable();
    }

    // Holding a direction jumps that way (camera-relative); no direction jumps straight up.
    private void OnJump()
    {
        Vector2 move = input.Move;
        Vector3 direction = move.magnitude < settings.inputDeadzone ? Vector3.zero : ToWorldDirection(move);
        roller.Jump(direction);
    }

    private void Update()
    {
        if (input == null || roller.IsRolling) return;

        Vector2 move = input.Move;
        if (move.magnitude < settings.inputDeadzone)
        {
            roller.Release(Time.deltaTime);
            return;
        }

        roller.Hold(ToWorldDirection(move), Time.deltaTime, input.IsSprinting);
    }

    private Vector3 ToWorldDirection(Vector2 move)
    {
        if (cameraTransform == null) return new Vector3(move.x, 0f, move.y);

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        return right * move.x + forward * move.y;
    }
}
