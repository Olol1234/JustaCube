using UnityEngine;

// How move input is turned into a world direction.
public enum CameraMode
{
    CameraRelative, // "up" moves away from the camera view
    WorldRelative,  // "up" always moves along world +Z, whatever the camera does
}

// The player "brain": reads the InputReader and asks the CubeRoller to roll.
// Directions follow the selected CameraMode (camera-relative by default).
[RequireComponent(typeof(CubeRoller))]
public class PlayerCubeInput : MonoBehaviour
{
    [Tooltip("Select the camera mode for player input")]
    [SerializeField] private CameraMode cameraMode;
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
        input.DashPerformed += OnDash;
    }

    private void OnDisable()
    {
        if (input == null) return;
        input.JumpPerformed -= OnJump;
        input.DashPerformed -= OnDash;
        input.Disable();
    }

    // Holding a direction jumps that way (per the selected CameraMode); no direction jumps straight up.
    private void OnJump()
    {
        Vector2 move = input.Move;
        Vector3 direction = move.magnitude < settings.inputDeadzone ? Vector3.zero : ToWorldDirection(move);
        roller.Jump(direction);
    }

    // Holding a direction dashes that way; no direction dashes forward from the camera.
    private void OnDash()
    {
        Vector2 move = input.Move;
        Vector3 direction = move.magnitude < settings.inputDeadzone ? ForwardDirection() : ToWorldDirection(move);
        roller.Dash(direction);
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

    // "Away from the view": the camera's forward on the ground plane (world +Z in WorldRelative).
    private Vector3 ForwardDirection() => ToWorldDirection(Vector2.up);

    private Vector3 ToWorldDirection(Vector2 move)
    {
        if (cameraMode == CameraMode.WorldRelative || cameraTransform == null)
            return new Vector3(move.x, 0f, move.y);

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        return right * move.x + forward * move.y;
    }
}
