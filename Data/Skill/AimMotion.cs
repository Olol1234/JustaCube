using System;
using UnityEngine;

// The numbers for the aimed shot's cast: how the cube rises into its hover. Data only, no logic.
// Serializable, so CubeAim can hold one as a foldout field. More fields (slow-mo, aim window,
// muzzle) are added together with the code that uses them.
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
}
