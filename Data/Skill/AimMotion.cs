using System;
using UnityEngine;

// The numbers for the aimed shot's cast: the rise into the hover and the aim window. Data only,
// no logic. Serializable, so CubeAim can hold one as a foldout field. More fields (muzzle) are
// added together with the code that uses them.
[Serializable]
public class AimMotion
{
    [Header("Rise")]
    [Tooltip("How far (m) the cube rises into its hover.")]
    [Min(0f)] public float riseHeight = 2f;

    [Tooltip("Seconds the rise takes.")]
    [Min(0.01f)] public float riseTime = 0.3f;

    [Tooltip("Shape of the rise. 0 = start, 1 = full height.")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("How fast (degrees per second) a tilted cube turns flat while it rises, so it does not hover at an angle.")]
    [Min(0f)] public float flattenSpeed = 720f;

    [Header("Aim window")]
    [Tooltip("Game speed while aiming (0.2 = five times slower). Needs a TimeScaleController on CubeAim.")]
    [Range(0.05f, 1f)] public float slowMoScale = 0.2f;

    [Tooltip("Real seconds (not slowed down) the player has to fire before the aim ends on its own.")]
    [Min(0.5f)] public float aimWindow = 4f;
}
