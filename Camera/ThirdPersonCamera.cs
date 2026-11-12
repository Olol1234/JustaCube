using UnityEngine;

// Orbits around a target using look input and follows its position only,
// so the camera stays steady while the cube tumbles.
// Has a small state machine: Free (look input orbits) and Locked (the camera
// steers itself toward a Lockable and ignores look input). An Aiming state
// can be added later for shooting.
[RequireComponent(typeof(TargetFinder))]
public class ThirdPersonCamera : MonoBehaviour
{
    private enum CameraState { Free, Locked }

    // Announced so other scripts (e.g. the lock-on marker) can react without the camera knowing them.
    public event System.Action<Lockable> TargetLocked;
    public event System.Action TargetUnlocked;

    [SerializeField] private Transform target;
    [SerializeField] private InputReader input;
    [SerializeField] private CameraSettings settings;

    private TargetFinder finder;
    private CameraState state = CameraState.Free;
    private Lockable lockedTarget;

    private float yaw;
    private float pitch;
    private float yawVelocity;
    private float pitchVelocity;
    private Vector3 focusPoint;
    private Vector3 focusVelocity;

    private void Awake()
    {
        finder = GetComponent<TargetFinder>();
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = settings.startPitch;
        if (target != null) focusPoint = FocusTarget();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.Enable();
            input.LockOnPerformed += OnLockOn;
        }
        if (settings != null && settings.lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.LockOnPerformed -= OnLockOn;
            input.Disable();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Lock key: unlock if locked, otherwise lock onto the best target (if any).
    private void OnLockOn()
    {
        if (state == CameraState.Locked)
        {
            Unlock();
            return;
        }

        Lockable best = finder.FindBest();
        if (best == null) return;

        lockedTarget = best;
        state = CameraState.Locked;
        yawVelocity = 0f;
        pitchVelocity = 0f;
        TargetLocked?.Invoke(best);
    }

    private void Unlock()
    {
        lockedTarget = null;
        state = CameraState.Free;
        TargetUnlocked?.Invoke();
    }

    private void LateUpdate()
    {
        if (target == null || settings == null) return;

        // The target was destroyed while locked.
        if (state == CameraState.Locked && lockedTarget == null) Unlock();

        focusPoint = Vector3.SmoothDamp(focusPoint, FocusTarget(), ref focusVelocity, settings.followSmoothTime);

        switch (state)
        {
            case CameraState.Free: UpdateFree(); break;
            case CameraState.Locked: UpdateLocked(); break;
        }

        Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(
            focusPoint - orbit * Vector3.forward * settings.distance,
            Quaternion.Euler(pitch + settings.tiltOffset, yaw, 0f));
    }

    private void UpdateFree()
    {
        Vector2 look = input != null ? input.Look : Vector2.zero;
        yaw += look.x * settings.sensitivity;
        pitch += look.y * settings.sensitivity * (settings.invertY ? 1f : -1f);
        pitch = Mathf.Clamp(pitch, settings.minPitch, settings.maxPitch);
    }

    // Look input is ignored: yaw and pitch ease toward the direction from the cube to the target.
    private void UpdateLocked()
    {
        Vector3 toTarget = lockedTarget.AimPosition - focusPoint;
        float horizontal = new Vector2(toTarget.x, toTarget.z).magnitude;

        // Directly above or below the cube there is no meaningful yaw, so keep the current one.
        float wantedYaw = horizontal > 0.01f ? Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg : yaw;

        // Looking down gives positive pitch, so a target above the cube gives a negative angle.
        float wantedPitch = -Mathf.Atan2(toTarget.y, horizontal) * Mathf.Rad2Deg + settings.lockPitchOffset;
        wantedPitch = Mathf.Clamp(wantedPitch, settings.minPitch, settings.maxPitch);

        yaw = Mathf.SmoothDampAngle(yaw, wantedYaw, ref yawVelocity, settings.lockTurnSmoothTime);
        pitch = Mathf.SmoothDampAngle(pitch, wantedPitch, ref pitchVelocity, settings.lockTurnSmoothTime);
    }

    private Vector3 FocusTarget() => target.position + Vector3.up * settings.heightOffset;
}
