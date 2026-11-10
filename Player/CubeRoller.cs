using System;
using System.Collections;
using UnityEngine;

// Rolls the cube one face at a time by rotating around its bottom edge.
// Knows nothing about input - anything (player, AI, replay) can call TryRoll.
public class CubeRoller : MonoBehaviour
{
    [SerializeField] private CubeMovementSettings settings;

    public bool IsRolling { get; private set; }

    public event Action<Vector3> RollStarted;
    public event Action<Vector3> RollCompleted;

    private Vector3 tiltDir;
    private Vector3 tiltStartPos;
    private Quaternion tiltStartRot;
    private float tiltAngle;
    private float tiltVelocity;

    private float HalfSize => transform.localScale.x * 0.5f;

    public bool TryRoll(Vector3 direction)
    {
        if (IsRolling || settings == null) return false;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;

        StartCoroutine(Roll(SnapToCardinal(direction)));
        return true;
    }

    // Pushes the cube while the player holds a direction key. Call every frame the key is held.
    // Same direction as the current tilt pushes it over, the opposite direction pushes it back
    // (and over to the other side once flat), a sideways key applies no push so gravity decides.
    public void Hold(Vector3 direction, float dt)
    {
        if (IsRolling || settings == null) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            Release(dt);
            return;
        }

        // Two adjacent keys (e.g. A+W) give a main and a secondary cardinal direction.
        Vector3 primary = SnapToCardinal(direction);
        Vector3 secondary = GetSecondaryDirection(direction, primary);

        // Resting flat: the key chooses which edge the cube tips over.
        if (tiltAngle <= 0f && tiltVelocity <= 0f) BeginTilt(primary);

        bool holdsTiltDir = tiltDir == primary || tiltDir == secondary;
        bool holdsOpposite = -tiltDir == primary || -tiltDir == secondary;

        float push = 0f;
        if (holdsTiltDir) push = 1f;
        else if (holdsOpposite) push = -1f;

        // Zigzag: with two keys held, each landing switches to the other key's direction,
        // so the cube staircases diagonally.
        Vector3 nextDir = (secondary != Vector3.zero && tiltDir == primary) ? secondary : primary;

        Step(push, nextDir, dt);
    }

    // Call every frame no key is held. Only gravity (and leftover momentum) acts on the cube.
    public void Release(float dt)
    {
        if (IsRolling || settings == null) return;

        if (tiltAngle <= 0f && tiltVelocity <= 0f)
        {
            tiltVelocity = 0f;
            return;
        }

        Step(0f, Vector3.zero, dt);
    }

    // The weaker axis of the input as a cardinal direction, or zero if it is too weak to count
    // as a second key. "Too weak" is set by diagonalThreshold (weaker / stronger component).
    private Vector3 GetSecondaryDirection(Vector3 direction, Vector3 primary)
    {
        bool primaryIsX = primary.x != 0f;
        float main = primaryIsX ? direction.x : direction.z;
        float other = primaryIsX ? direction.z : direction.x;

        if (Mathf.Abs(other) < Mathf.Abs(main) * settings.diagonalThreshold) return Vector3.zero;

        return primaryIsX
            ? new Vector3(0f, 0f, Mathf.Sign(other))
            : new Vector3(Mathf.Sign(other), 0f, 0f);
    }

    // One physics step of the tilt. push is +1 / 0 / -1 along tiltDir; nextDir is the direction
    // the cube heads in after it lands (zero keeps the current one).
    private void Step(float push, Vector3 nextDir, float dt)
    {
        // Gravity pulls flat below 45 degrees and over the edge above it (unstable balance at 45).
        float gravity = settings.gravityAcceleration * Mathf.Sin((tiltAngle - 45f) * Mathf.Deg2Rad);

        tiltVelocity += (push * settings.pushAcceleration + gravity) * dt;
        tiltVelocity *= Mathf.Max(0f, 1f - settings.tiltDamping * dt);
        tiltVelocity = Mathf.Clamp(tiltVelocity, -settings.maxTiltSpeed, settings.maxTiltSpeed);
        tiltAngle += tiltVelocity * dt;

        if (tiltAngle >= 90f)
        {
            Land(nextDir);
            return;
        }

        if (tiltAngle <= 0f)
        {
            if (push < 0f)
            {
                // Rocked back through flat while holding the opposite key: tip the other way.
                float overshoot = -tiltAngle;
                float speed = -tiltVelocity;
                Vector3 flipDir = -tiltDir;
                tiltAngle = 0f;
                ApplyTilt();
                BeginTilt(flipDir);
                tiltAngle = overshoot;
                tiltVelocity = Mathf.Max(0f, speed);
            }
            else
            {
                // Settled back flat.
                tiltAngle = 0f;
                tiltVelocity = 0f;
            }
        }

        ApplyTilt();
    }

    private void BeginTilt(Vector3 direction)
    {
        tiltDir = direction;
        tiltStartPos = transform.position;
        tiltStartRot = transform.rotation;
    }

    // The cube tipped all the way over: clean up float error, then re-base so the new face is
    // the flat reference. Tilt velocity is kept, so holding a key keeps the cube walking.
    private void Land(Vector3 nextDir)
    {
        float overshoot = tiltAngle - 90f;
        Vector3 landedDir = tiltDir;

        tiltAngle = 90f;
        ApplyTilt();
        transform.rotation = SnapRotation(transform.rotation);

        BeginTilt(nextDir != Vector3.zero ? nextDir : landedDir);
        tiltAngle = Mathf.Clamp(overshoot, 0f, 90f);
        ApplyTilt();

        RollStarted?.Invoke(landedDir);
        RollCompleted?.Invoke(landedDir);
    }

    private void ApplyTilt()
    {
        Vector3 pivot = tiltStartPos + (tiltDir + Vector3.down) * HalfSize;
        Quaternion step = Quaternion.AngleAxis(tiltAngle, Vector3.Cross(Vector3.up, tiltDir));
        transform.SetPositionAndRotation(
            pivot + step * (tiltStartPos - pivot),
            step * tiltStartRot);
    }

    private IEnumerator Roll(Vector3 direction)
    {
        IsRolling = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        RollStarted?.Invoke(direction);

        Vector3 pivot = startPos + (direction + Vector3.down) * HalfSize;
        Vector3 axis = Vector3.Cross(Vector3.up, direction);

        float duration = settings.rollDuration;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = settings.rollCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            Quaternion step = Quaternion.AngleAxis(90f * t, axis);

            transform.SetPositionAndRotation(
                pivot + step * (startPos - pivot),
                step * startRot);
            yield return null;
        }

        // Snap to clean values so float error doesn't build up over many rolls.
        Quaternion finalStep = Quaternion.AngleAxis(90f, axis);
        transform.SetPositionAndRotation(
            pivot + finalStep * (startPos - pivot),
            SnapRotation(finalStep * startRot));

        IsRolling = false;
        RollCompleted?.Invoke(direction);
    }

    private static Vector3 SnapToCardinal(Vector3 dir)
    {
        return Mathf.Abs(dir.x) > Mathf.Abs(dir.z)
            ? new Vector3(Mathf.Sign(dir.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }

    private static Quaternion SnapRotation(Quaternion rot)
    {
        Vector3 e = rot.eulerAngles;
        return Quaternion.Euler(
            Mathf.Round(e.x / 90f) * 90f,
            Mathf.Round(e.y / 90f) * 90f,
            Mathf.Round(e.z / 90f) * 90f);
    }
}
