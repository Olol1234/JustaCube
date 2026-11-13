using System;
using System.Collections;
using UnityEngine;

// Rolls the cube one face at a time by rotating around its bottom edge.
// Knows nothing about input - anything (player, AI, replay) can call TryRoll.
[RequireComponent(typeof(GroundProbe), typeof(CubeCollisionProbe))]
public class CubeRoller : MonoBehaviour
{
    [SerializeField] private CubeMovementSettings settings;

    public bool IsRolling { get; private set; }
    public bool IsAirborne { get; private set; }
    public bool IsDashing { get; private set; }

    public event Action<Vector3> RollStarted;
    public event Action<Vector3> RollCompleted;
    public event Action Landed;
    public event Action<Vector3> DashStarted;
    public event Action DashEnded;

    private GroundProbe groundProbe;
    private CubeCollisionProbe collisionProbe;
    private Vector3 airVelocity;
    private float verticalVelocity;
    private int jumpsUsed;

    private Vector3 dashDir;
    private Vector3 dashStartPos;
    private float dashElapsed;
    private float dashCooldownTimer;
    private int airDashesUsed;

    private Vector3 tiltDir;
    private Vector3 tiltStartPos;
    private Quaternion tiltStartRot;
    private float tiltAngle;
    private float tiltVelocity;

    private float HalfSize => transform.localScale.x * 0.5f;

    private void Awake()
    {
        groundProbe = GetComponent<GroundProbe>();
        collisionProbe = GetComponent<CubeCollisionProbe>();
    }

    private void Update()
    {
        if (settings == null) return;

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        // A dash takes over completely: no gravity, no tumble, no ground probe until it ends.
        if (IsDashing)
        {
            StepDash(Time.deltaTime);
            return;
        }

        if (IsAirborne)
        {
            StepAir(Time.deltaTime);
            return;
        }

        if (IsRolling) return;

        // Walked or tipped off a ledge: the center is no longer over ground.
        groundProbe.Probe();
        if (!groundProbe.IsSupported) StartFall();
    }

    // Jumps in the given world direction, or straight up if it is zero. Works from the ground,
    // and once more in the air (a double jump) that can change direction. Returns false if no
    // jumps are left.
    public bool Jump(Vector3 direction)
    {
        if (settings == null || IsDashing || jumpsUsed >= settings.maxJumps) return false;

        if (!IsAirborne)
        {
            if (IsRolling) return false;
            groundProbe.Probe();
            if (!groundProbe.IsGrounded) return false;
            IsAirborne = true;
        }

        jumpsUsed++;

        // v = sqrt(2 * g * h): tune the jump by its height instead of guessing a speed.
        float height = settings.jumpHeight * transform.localScale.y;
        verticalVelocity = Mathf.Sqrt(2f * settings.airGravity * height);

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            airVelocity = Vector3.zero;
            return true;
        }

        airVelocity = direction.normalized * settings.jumpHorizontalSpeed;

        // Tumble toward the jump direction. A new direction means a new spin axis.
        Vector3 spinDir = SnapToCardinal(direction);
        if (spinDir != tiltDir) tiltVelocity = settings.jumpSpinSpeed;
        else tiltVelocity = Mathf.Max(tiltVelocity, settings.jumpSpinSpeed);
        tiltDir = spinDir;
        return true;
    }

    // Dashes in the given world direction: a fixed-distance flat burst that keeps the cube's
    // orientation (a tipped cube slides tipped, then carries on tilting from where it stopped).
    // Works from the ground and in the air (limited by maxAirDashes). Returns false if a dash
    // isn't allowed right now. A zero direction is refused:
    // picking a default (e.g. forward from the camera) is the caller's job.
    public bool Dash(Vector3 direction)
    {
        if (settings == null || IsDashing || IsRolling || dashCooldownTimer > 0f) return false;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;

        if (IsAirborne)
        {
            if (airDashesUsed >= settings.maxAirDashes) return false;
            airDashesUsed++;
        }

        IsDashing = true;
        dashDir = direction.normalized;
        dashStartPos = transform.position;
        dashElapsed = 0f;

        DashStarted?.Invoke(dashDir);
        return true;
    }

    // Slides the cube along dashDir. The position is computed from the start each frame (not
    // added to the last one), so the dash always covers exactly dashDistance whatever the
    // framerate. dashCurve shapes how the distance is spread over dashDuration.
    // A block in the way ends the dash early (plain stop, no sliding along the wall).
    private void StepDash(float dt)
    {
        dashElapsed += dt;
        float t = Mathf.Clamp01(dashElapsed / settings.dashDuration);
        float progress = settings.dashCurve.Evaluate(t);

        Vector3 target = dashStartPos + dashDir * (settings.dashDistance * progress);

        if (!MoveDashTo(target) || t >= 1f) EndDash();
    }

    // Moves the cube toward target in pieces of at most half a cube, testing each one, so a
    // fast dash (or a low framerate) can't jump over a thin wall. Returns false if a block
    // stopped it. On a hit it narrows down the last piece, so the cube ends up against the
    // block instead of leaving a gap.
    private bool MoveDashTo(Vector3 target)
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Already touching something: don't block, so the cube can always move out of it.
        bool alreadyInside = collisionProbe.WouldOverlap(pos, rot);

        Vector3 delta = target - pos;
        float distance = delta.magnitude;
        if (distance < 0.0001f) return true;

        int steps = Mathf.CeilToInt(distance / HalfSize);
        Vector3 step = delta / steps;

        for (int i = 0; i < steps; i++)
        {
            Vector3 next = pos + step;

            if (!alreadyInside && collisionProbe.WouldOverlap(next, rot))
            {
                // Bisect between the free spot (lo) and the blocked one (hi).
                Vector3 lo = pos;
                Vector3 hi = next;
                for (int j = 0; j < 6; j++)
                {
                    Vector3 mid = (lo + hi) * 0.5f;
                    if (collisionProbe.WouldOverlap(mid, rot)) hi = mid;
                    else lo = mid;
                }

                transform.position = lo;
                return false;
            }

            pos = next;
        }

        transform.position = pos;
        return true;
    }

    private void EndDash()
    {
        // The tilt is computed from a flat reference position. Move that reference by however far
        // the dash went, so the tilt carries on from here instead of snapping back to the start.
        tiltStartPos += transform.position - dashStartPos;

        IsDashing = false;
        dashCooldownTimer = settings.dashCooldown;
        DashEnded?.Invoke();
    }

    // Left the ground without jumping. Counts as one jump used.
    private void StartFall()
    {
        IsAirborne = true;
        airVelocity = Vector3.zero;
        verticalVelocity = 0f;
        jumpsUsed = Mathf.Max(jumpsUsed, 1);
    }

    private void StepAir(float dt)
    {
        verticalVelocity = Mathf.Max(verticalVelocity - settings.airGravity * dt, -settings.maxFallSpeed);

        MoveInAir(dt);

        // Keep tumbling around the center on the same axis the ground tilt used.
        // A spin step that would swing a corner into a block is skipped, so the cube can't get
        // stuck in a wall. (If it is already touching something, spinning is allowed.)
        if (tiltDir != Vector3.zero)
        {
            Quaternion spun = Quaternion.AngleAxis(tiltVelocity * dt, Vector3.Cross(Vector3.up, tiltDir)) * transform.rotation;
            bool inside = collisionProbe.WouldOverlap(transform.position, transform.rotation);
            if (inside || !collisionProbe.WouldOverlap(transform.position, spun))
                transform.rotation = spun;
        }

        if (verticalVelocity > 0f) return;

        groundProbe.Probe();
        if (groundProbe.IsGrounded && LowestPointY() <= groundProbe.GroundHeight) LandFromAir();
    }

    // Moves the cube one axis at a time, so it slides along a wall instead of sticking to it.
    // A blocked axis loses its speed (hard stop). Falling is not checked here: landing does that.
    private void MoveInAir(float dt)
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;

        // Already touching something (e.g. the floor at the start of a jump): don't block, so
        // the cube can always move out of it.
        bool alreadyInside = collisionProbe.WouldOverlap(pos, rot);

        Vector3 stepX = new Vector3(airVelocity.x * dt, 0f, 0f);
        if (!alreadyInside && collisionProbe.WouldOverlap(pos + stepX, rot)) airVelocity.x = 0f;
        else pos += stepX;

        Vector3 stepZ = new Vector3(0f, 0f, airVelocity.z * dt);
        if (!alreadyInside && collisionProbe.WouldOverlap(pos + stepZ, rot)) airVelocity.z = 0f;
        else pos += stepZ;

        // Only a ceiling is blocked here. Coming down onto a block is handled by landing.
        Vector3 stepY = new Vector3(0f, verticalVelocity * dt, 0f);
        if (verticalVelocity > 0f && !alreadyInside && collisionProbe.WouldOverlap(pos + stepY, rot)) verticalVelocity = 0f;
        else pos += stepY;

        transform.position = pos;
    }

    // World Y of the lowest point of the (possibly tilted) cube.
    private float LowestPointY()
    {
        float extent = HalfSize * (Mathf.Abs(transform.right.y) + Mathf.Abs(transform.up.y) + Mathf.Abs(transform.forward.y));
        return transform.position.y - extent;
    }

    // Touched down. Find the nearest flat orientation and how far the cube is tipped away from
    // it, then hand over to the normal tilt code, which decides if it falls back or tips over.
    private void LandFromAir()
    {
        Vector3 spinAxis = tiltDir != Vector3.zero ? Vector3.Cross(Vector3.up, tiltDir) : Vector3.zero;

        Quaternion flat = SnapRotation(transform.rotation);
        (transform.rotation * Quaternion.Inverse(flat)).ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle = 360f - angle;
            axis = -axis;
        }

        // Only a tip around a horizontal axis can be continued as a tilt.
        axis.y = 0f;
        if (angle < 0.5f || axis.sqrMagnitude < 0.0001f)
        {
            angle = 0f;
            axis = spinAxis != Vector3.zero ? spinAxis : Vector3.right;
        }
        axis.Normalize();

        // Keep the spin direction relative to the (possibly flipped) tilt axis.
        tiltVelocity = spinAxis != Vector3.zero ? tiltVelocity * Mathf.Sign(Vector3.Dot(spinAxis, axis)) : 0f;

        // Cross(axis, up) is the direction whose tilt rotates around this axis.
        tiltDir = SnapToCardinal(Vector3.Cross(axis, Vector3.up));
        tiltAngle = angle;
        tiltStartRot = flat;

        // Solve for the flat-reference position that makes ApplyTilt reproduce the current
        // center, so the cube doesn't pop. Then rest the pivot edge on the ground.
        Vector3 v = (tiltDir + Vector3.down) * HalfSize;
        Quaternion step = Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, tiltDir));
        tiltStartPos = transform.position + step * v - v;
        tiltStartPos.y = groundProbe.GroundHeight + HalfSize;

        IsAirborne = false;
        verticalVelocity = 0f;
        airVelocity = Vector3.zero;
        jumpsUsed = 0;
        airDashesUsed = 0;
        ApplyTilt();

        Landed?.Invoke();
    }

    public bool TryRoll(Vector3 direction)
    {
        if (IsRolling || IsAirborne || settings == null) return false;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return false;

        StartCoroutine(Roll(SnapToCardinal(direction)));
        return true;
    }

    // Pushes the cube while the player holds a direction key. Call every frame the key is held.
    // Same direction as the current tilt pushes it over, the opposite direction pushes it back
    // (and over to the other side once flat), a sideways key applies no push so gravity decides.
    public void Hold(Vector3 direction, float dt, bool sprint = false)
    {
        if (IsRolling || IsAirborne || IsDashing || settings == null) return;

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
        if (tiltAngle <= 0f && tiltVelocity <= 0f)
        {
            BeginTilt(primary);
            tiltVelocity = settings.startKick;
        }

        bool holdsTiltDir = tiltDir == primary || tiltDir == secondary;
        bool holdsOpposite = -tiltDir == primary || -tiltDir == secondary;

        float push = 0f;
        if (holdsTiltDir) push = 1f;
        else if (holdsOpposite) push = -1f;

        // Zigzag: with two keys held, each landing switches to the other key's direction,
        // so the cube staircases diagonally.
        Vector3 nextDir = (secondary != Vector3.zero && tiltDir == primary) ? secondary : primary;

        Step(push, nextDir, dt, sprint);
    }

    // Call every frame no key is held. Only gravity (and leftover momentum) acts on the cube.
    public void Release(float dt)
    {
        if (IsRolling || IsAirborne || IsDashing || settings == null) return;

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
    private void Step(float push, Vector3 nextDir, float dt, bool sprint = false)
    {
        // Gravity pulls flat below 45 degrees and over the edge above it (unstable balance at 45).
        float gravity = settings.gravityAcceleration * Mathf.Sin((tiltAngle - 45f) * Mathf.Deg2Rad);

        // Walking and sprinting differ in how hard the key pushes and the speed it pushes up to.
        float acceleration = sprint ? settings.sprintPushAcceleration : settings.pushAcceleration;
        float maxSpeed = sprint ? settings.sprintMaxTiltSpeed : settings.maxTiltSpeed;

        // Past its max speed the push adds nothing. Letting go of shift doesn't snap the speed
        // down: damping slowly brings it back under the walking max.
        float pushForce = push * acceleration;
        if (push > 0f && tiltVelocity >= maxSpeed) pushForce = 0f;

        tiltVelocity += (pushForce + gravity) * dt;
        tiltVelocity *= Mathf.Max(0f, 1f - settings.tiltDamping * dt);

        float hardCap = Mathf.Max(settings.maxTiltSpeed, settings.sprintMaxTiltSpeed);
        tiltVelocity = Mathf.Clamp(tiltVelocity, -hardCap, hardCap);

        float previousAngle = tiltAngle;
        tiltAngle += tiltVelocity * dt;

        // Tipping further into a block: stay at the old angle and lose the speed. Only checked
        // while the angle grows, so a cube that is already touching something can still back off.
        if (tiltAngle > previousAngle && IsTiltBlocked(Mathf.Min(tiltAngle, 90f)))
        {
            tiltAngle = previousAngle;
            tiltVelocity = 0f;
            return;
        }

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

        // The leftover angle could push the new face into a block right away: start flat instead.
        if (IsTiltBlocked(tiltAngle))
        {
            tiltAngle = 0f;
            tiltVelocity = 0f;
        }
        ApplyTilt();

        RollStarted?.Invoke(landedDir);
        RollCompleted?.Invoke(landedDir);
    }

    // The position and rotation the cube would have at a given tilt angle. Moves nothing.
    private void GetTiltPose(float angle, out Vector3 position, out Quaternion rotation)
    {
        Vector3 pivot = tiltStartPos + (tiltDir + Vector3.down) * HalfSize;
        Quaternion step = Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, tiltDir));
        position = pivot + step * (tiltStartPos - pivot);
        rotation = step * tiltStartRot;
    }

    private void ApplyTilt()
    {
        GetTiltPose(tiltAngle, out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(position, rotation);
    }

    // Would the cube overlap a block at this tilt angle?
    private bool IsTiltBlocked(float angle)
    {
        GetTiltPose(angle, out Vector3 position, out Quaternion rotation);
        return collisionProbe.WouldOverlap(position, rotation);
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
