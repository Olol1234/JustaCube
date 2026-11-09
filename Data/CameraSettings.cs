using UnityEngine;

[CreateAssetMenu(fileName = "CameraSettings", menuName = "TheOneCube/Camera Settings")]
public class CameraSettings : ScriptableObject
{
    [Header("Framing")]
    [Tooltip("Distance from the look-at point.")]
    [Min(0.5f)] public float distance = 6f;

    [Tooltip("Offset above the target the camera looks at.")]
    public float heightOffset = 0.5f;

    [Tooltip("Extra downward tilt in degrees, applied after positioning. Use it to aim lower when Height Offset is high.")]
    [Range(-45f, 45f)] public float tiltOffset = 0f;

    [Header("Orbit")]
    [Tooltip("Degrees per unit of look input.")]
    public float sensitivity = 0.15f;
    public bool invertY;
    [Range(-89f, 89f)] public float minPitch = 5f;
    [Range(-89f, 89f)] public float maxPitch = 70f;
    public float startPitch = 25f;

    [Header("Follow")]
    [Tooltip("Seconds to catch up with the target. 0 = locked on.")]
    [Min(0f)] public float followSmoothTime = 0.12f;

    [Header("Cursor")]
    public bool lockCursor = true;
}
