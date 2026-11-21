using UnityEngine;

// Keeps a world-space UI (a mob's health bar) flat to the screen, like a sticker on the lens, so
// it never turns edge-on or backwards as the camera orbits. Moves nothing, only rotates.
public class UI_Billboard : MonoBehaviour
{
    private Transform cam;

    // Start, not Awake: the camera object may not be ready yet when this one wakes up.
    private void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    // LateUpdate runs after every Update, so after the camera has moved this frame. In Update the
    // bar would use last frame's camera pose and lag behind.
    private void LateUpdate()
    {
        if (cam == null) return;

        // Copying the camera's rotation (not LookAt) keeps the bar upright and readable. LookAt on
        // a world-space Canvas would show it mirrored, because its front faces away from its target.
        transform.rotation = cam.rotation;
    }
}
