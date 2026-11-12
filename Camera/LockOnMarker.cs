using UnityEngine;

// Shows a reticle over the locked target. Put this on an object that stays active
// (e.g. the Canvas); the reticle itself is a child that gets shown and hidden.
// It only listens to the camera's events, so the camera doesn't know it exists.
// Runs after the camera (higher execution order) so the reticle never lags a frame behind it.
[DefaultExecutionOrder(100)]
public class LockOnMarker : MonoBehaviour
{
    [SerializeField] private ThirdPersonCamera cameraRig;
    [Tooltip("The reticle UI object (e.g. an Image). Shown only while locked.")]
    [SerializeField] private RectTransform reticle;

    private Camera cam;
    private Lockable current;

    private void Awake()
    {
        cam = Camera.main;
        reticle.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        cameraRig.TargetLocked += OnLocked;
        cameraRig.TargetUnlocked += OnUnlocked;
    }

    private void OnDisable()
    {
        cameraRig.TargetLocked -= OnLocked;
        cameraRig.TargetUnlocked -= OnUnlocked;
    }

    private void OnLocked(Lockable target) => current = target;

    private void OnUnlocked()
    {
        current = null;
        reticle.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (current == null) return;

        Vector3 screen = cam.WorldToScreenPoint(current.AimPosition);

        // z < 0 means the point is behind the camera.
        bool visible = screen.z > 0f;
        reticle.gameObject.SetActive(visible);
        if (visible) reticle.position = screen;
    }
}
