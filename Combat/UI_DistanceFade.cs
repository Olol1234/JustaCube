using UnityEngine;

// Fades a world-space UI (a mob's health bar) out with distance from the camera: fully visible
// when near, fading over the last `fadeRange` metres, gone at `showDistance`. Only changes the
// CanvasGroup's alpha, so it knows nothing about health.
[RequireComponent(typeof(CanvasGroup))]
public class UI_DistanceFade : MonoBehaviour
{
    [Tooltip("Distance (m) from the camera at which the bar is completely gone.")]
    [SerializeField, Min(0.1f)] private float showDistance = 15f;

    [Tooltip("How many metres before showDistance the fade starts. 0 = no fade, the bar just pops off.")]
    [SerializeField, Min(0f)] private float fadeRange = 5f;

    private CanvasGroup group;
    private Transform cam;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    // Start, not Awake: the camera object may not be ready yet when this one wakes up.
    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (cam == null) return;

        float distance = Vector3.Distance(cam.position, transform.position);

        // 1 up to (showDistance - fadeRange), then falls to 0 at showDistance.
        // With no fade range, just on or off.
        float alpha = fadeRange > 0f
            ? Mathf.Clamp01((showDistance - distance) / fadeRange)
            : (distance <= showDistance ? 1f : 0f);

        group.alpha = alpha;
    }
}
