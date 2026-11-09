using UnityEngine;

// Orbits around a target using look input and follows its position only,
// so the camera stays steady while the cube tumbles.
public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private InputReader input;
    [SerializeField] private CameraSettings settings;

    private float yaw;
    private float pitch;
    private Vector3 focusPoint;
    private Vector3 focusVelocity;

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = settings.startPitch;
        if (target != null) focusPoint = FocusTarget();
    }

    private void OnEnable()
    {
        input?.Enable();
        if (settings != null && settings.lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        input?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void LateUpdate()
    {
        if (target == null || settings == null) return;

        Vector2 look = input != null ? input.Look : Vector2.zero;
        yaw += look.x * settings.sensitivity;
        pitch += look.y * settings.sensitivity * (settings.invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch, settings.minPitch, settings.maxPitch);

        focusPoint = Vector3.SmoothDamp(focusPoint, FocusTarget(), ref focusVelocity, settings.followSmoothTime);

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(
            focusPoint - orbit * Vector3.forward * settings.distance,
            Quaternion.Euler(pitch + settings.tiltOffset, yaw, 0f));
    }

    private Vector3 FocusTarget() => target.position + Vector3.up * settings.heightOffset;
}
