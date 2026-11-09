using UnityEngine;

[CreateAssetMenu(fileName = "CubeMovementSettings", menuName = "TheOneCube/Cube Movement Settings")]
public class CubeMovementSettings : ScriptableObject
{
    [Tooltip("Seconds it takes to roll one face (90 degrees).")]
    [Min(0.01f)] public float rollDuration = 0.25f;

    [Tooltip("Easing over the roll. X = time (0-1), Y = progress (0-1).")]
    public AnimationCurve rollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Tilt physics")]
    [Tooltip("Angular acceleration (deg/s^2) from holding a direction key. Must beat Gravity Acceleration * 0.71, or the cube can't tip from flat.")]
    [Min(0f)] public float pushAcceleration = 400f;

    [Tooltip("Angular acceleration (deg/s^2) of gravity. Pulls the cube flat below 45 degrees and over the edge above it.")]
    [Min(0f)] public float gravityAcceleration = 300f;

    [Tooltip("Fraction of tilt speed lost per second. Stops the cube from rocking forever.")]
    [Min(0f)] public float tiltDamping = 2f;

    [Tooltip("Cap on tilt speed (deg/s), so a long fall can't get out of hand.")]
    [Min(1f)] public float maxTiltSpeed = 360f;

    [Header("Hold to tilt (legacy, no longer used by CubeRoller)")]
    [Tooltip("Degrees per second the cube tilts while a key is held, and also how fast it falls under gravity.")]
    [Min(1f)] public float tiltSpeed = 90f;

    [Tooltip("Tilt angle (degrees) where the cube balances. Below it falls back flat, above it falls over.")]
    [Range(10f, 80f)] public float balanceAngle = 45f;

    [Tooltip("How close (degrees) to the balance angle counts as balanced. Keep it at or above tiltSpeed / framerate.")]
    [Min(0f)] public float balanceTolerance = 2f;

    [Header("Input")]
    [Tooltip("Stick/key magnitude needed before a roll is triggered.")]
    [Range(0f, 1f)] public float inputDeadzone = 0.5f;
}
