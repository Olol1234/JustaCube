using System;
using UnityEngine;

// The numbers for one cast motion of the cube: rise, hang, slam, recover. Data only, no logic.
// Serializable, so a skill asset can hold one as a foldout field. CubeSlam plays it.
[Serializable]
public class SlamMotion
{
    [Header("Control")]
    [Tooltip("Pause: the cube's movement resumes exactly where it was after the cast. Override: the cast cancels the movement and the cube ends up standing flat.")]
    public ControlMode controlMode = ControlMode.Pause;

    [Header("Rise")]
    [Tooltip("How far (m) the cube rises before it slams down.")]
    [Min(0f)] public float riseHeight = 1.5f;

    [Tooltip("Seconds the rise takes.")]
    [Min(0.01f)] public float riseTime = 0.25f;

    [Tooltip("Shape of the rise. 0 = start, 1 = full height.")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Hang")]
    [Tooltip("Seconds the cube waits at the top. A short hang makes the slam read better.")]
    [Min(0f)] public float hangTime = 0.1f;

    [Header("Slam")]
    [Tooltip("Seconds the slam down takes. Short = heavy.")]
    [Min(0.01f)] public float slamTime = 0.12f;

    [Tooltip("Shape of the slam. 0 = top, 1 = back on the ground. Starts slow and speeds up by default.")]
    public AnimationCurve slamCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 0f),
        new Keyframe(1f, 1f, 2f, 2f));

    [Header("Recovery")]
    [Tooltip("Seconds the cube stays still after the impact, before it can move again.")]
    [Min(0f)] public float recoveryTime = 0.15f;
}
