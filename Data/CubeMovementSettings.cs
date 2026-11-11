using UnityEngine;

[CreateAssetMenu(fileName = "CubeMovementSettings", menuName = "TheOneCube/Cube Movement Settings")]
public class CubeMovementSettings : ScriptableObject
{
    [Tooltip("Seconds it takes to roll one face (90 degrees).")]
    [Min(0.01f)] public float rollDuration = 0.25f;

    [Tooltip("Easing over the roll. X = time (0-1), Y = progress (0-1).")]
    public AnimationCurve rollCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Tilt physics")]
    [Tooltip("Tilt speed (deg/s) the cube starts with when it begins tipping from flat. Raise it if the start feels slow.")]
    [Min(0f)] public float startKick = 60f;

    [Tooltip("Walking: angular acceleration (deg/s^2) from holding a direction key. Must beat Gravity Acceleration * 0.71, or the cube can't tip from flat.")]
    [Min(0f)] public float pushAcceleration = 400f;

    [Tooltip("Sprinting (shift): angular acceleration (deg/s^2) from holding a direction key. Higher than walking, so it ramps up faster.")]
    [Min(0f)] public float sprintPushAcceleration = 700f;

    [Tooltip("Angular acceleration (deg/s^2) of gravity. Pulls the cube flat below 45 degrees and over the edge above it.")]
    [Min(0f)] public float gravityAcceleration = 300f;

    [Tooltip("Fraction of tilt speed lost per second. Stops the cube from rocking forever.")]
    [Min(0f)] public float tiltDamping = 2f;

    [Tooltip("Walking: the push stops adding speed once the tilt speed (deg/s) reaches this. Damping must not hold the speed below it, or it is never reached.")]
    [Min(1f)] public float maxTiltSpeed = 360f;

    [Tooltip("Sprinting (shift): the push stops adding speed once the tilt speed (deg/s) reaches this. The larger of the two max speeds is also the hard cap on any tilt speed.")]
    [Min(1f)] public float sprintMaxTiltSpeed = 500f;

    [Tooltip("How strong the weaker input axis must be, compared to the stronger one, to count as a second key (diagonal zigzag). 1 = needs perfectly equal, lower = easier to trigger.")]
    [Range(0.1f, 1f)] public float diagonalThreshold = 0.5f;

    [Header("Jump")]
    [Tooltip("Jump height in cube heights. 1.7 clears a 1-block step but not a 2-block wall.")]
    [Min(0.1f)] public float jumpHeight = 1.7f;

    [Tooltip("Jumps allowed before landing. 2 = double jump. Walking off a ledge counts as one used.")]
    [Min(1)] public int maxJumps = 2;

    [Tooltip("Downward acceleration (m/s^2) while airborne.")]
    [Min(0.1f)] public float airGravity = 30f;

    [Tooltip("Horizontal speed (m/s) of a jump when a direction is held. No direction = straight up.")]
    [Min(0f)] public float jumpHorizontalSpeed = 4f;

    [Tooltip("Spin speed (deg/s) the cube tumbles at when jumping with a direction. The partial turn at landing makes it stumble.")]
    [Min(0f)] public float jumpSpinSpeed = 360f;

    [Tooltip("Cap on fall speed (m/s), so the ground probe can't be skipped in one frame.")]
    [Min(1f)] public float maxFallSpeed = 25f;

    [Header("Ground detection")]
    [Tooltip("Layers that count as ground. Keep the player cube off these layers.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Extra distance (m) added to the probe reach so it doesn't flicker at the boundary.")]
    [Min(0f)] public float groundSkin = 0.05f;

    [Header("Collision")]
    [Tooltip("How much smaller (m) than the cube the overlap test box is, so touching a floor or wall doesn't count as a hit.")]
    [Min(0f)] public float collisionSkin = 0.02f;

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
