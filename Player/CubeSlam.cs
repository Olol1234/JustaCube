using System;
using System.Collections;
using UnityEngine;

// Moves the cube through a cast motion (rise, hang, slam, recover) while the roller is paused.
// Knows nothing about what the slam is for: it calls onImpact when the cube hits the ground, and
// whoever started it (a skill) decides what happens then.
// Cast on the ground: rise, hang, slam back down (the roller is paused, its movement carries on
// afterwards). Cast in the air: hang briefly, then dive straight down until something solid is hit
// or the dive runs out of distance or time (the roller is overridden, so over a pit the cube just
// falls on normally). onImpact fires only if the dive actually hit something.
[RequireComponent(typeof(CubeRoller))]
public class CubeSlam : MonoBehaviour
{
    // The dive box is a little smaller than the cube, so a wall brushing the side does not count as
    // a hit on the way down.
    private const float BoxShrink = 0.95f;

    public bool IsPlaying { get; private set; }

    private CubeRoller roller;
    private Coroutine routine;

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
    }

    // If the cube is switched off mid-cast, hand control back so the roller is never left frozen.
    private void OnDisable()
    {
        Cancel();
    }

    // Starts the motion. Returns false if it can't: already playing, or the roller is already
    // controlled by something else. onImpact is called once, when the slam or dive lands.
    public bool Play(SlamMotion motion, Action onImpact = null)
    {
        if (motion == null || IsPlaying) return false;

        // Read this before BeginControl: Override clears the roller's airborne state.
        bool airborne = roller.IsAirborne;

        // The dive always overrides: the cube ends somewhere new, so the roller must not restore
        // its old pose afterwards.
        ControlMode mode = airborne ? ControlMode.Override : motion.controlMode;
        if (!roller.BeginControl(mode)) return false;

        IsPlaying = true;
        routine = StartCoroutine(airborne ? RunDive(motion, onImpact) : Run(motion, onImpact));
        return true;
    }

    // Stops the motion where it is and gives the cube back to the roller.
    public void Cancel()
    {
        if (!IsPlaying) return;

        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsPlaying = false;

        if (roller != null) roller.EndControl();
    }

    private IEnumerator Run(SlamMotion motion, Action onImpact)
    {
        Vector3 start = transform.position;
        Vector3 top = start + Vector3.up * motion.riseHeight;

        yield return Tween(start, top, motion.riseTime, motion.riseCurve);
        yield return new WaitForSeconds(motion.hangTime);
        yield return Tween(top, start, motion.slamTime, motion.slamCurve);

        onImpact?.Invoke();

        yield return new WaitForSeconds(motion.recoveryTime);

        routine = null;
        IsPlaying = false;
        roller.EndControl();
    }

    // The airborne version: hang, then dive straight down. Each frame the next piece of the fall is
    // swept with a box the size of the cube, so a thin ledge can't be skipped at high speed.
    private IEnumerator RunDive(SlamMotion motion, Action onImpact)
    {
        Quaternion flat = NearestFlat(transform.rotation);
        Vector3 halfExtents = transform.localScale * 0.5f * BoxShrink;
        LayerMask mask = roller.GroundMask;

        // Hang: the cube stays in the air and straightens up.
        float hang = 0f;
        while (hang < motion.diveHangTime)
        {
            hang += Time.deltaTime;
            Flatten(flat, motion.diveFlattenSpeed);
            yield return null;
        }

        float travelled = 0f;
        float elapsed = 0f;
        bool hitSomething = false;

        while (travelled < motion.maxDiveDistance && elapsed < motion.maxDiveTime)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            Flatten(flat, motion.diveFlattenSpeed);

            float step = Mathf.Min(motion.diveSpeed * dt, motion.maxDiveDistance - travelled);

            if (Physics.BoxCast(transform.position, halfExtents, Vector3.down, out RaycastHit hit,
                    transform.rotation, step, mask, QueryTriggerInteraction.Ignore))
            {
                // Rest the cube flat on the surface that was hit. A hit at distance 0 (the box
                // already touched something) reports no useful point, so stay where it is.
                Vector3 pos = transform.position;
                if (hit.distance > 0f) pos.y = hit.point.y + transform.localScale.y * 0.5f;
                transform.position = pos;
                transform.rotation = flat;
                hitSomething = true;
                break;
            }

            transform.position += Vector3.down * step;
            travelled += step;
            yield return null;
        }

        if (hitSomething)
        {
            onImpact?.Invoke();
            yield return new WaitForSeconds(motion.recoveryTime);
        }

        // Override: the roller rests the cube on the ground, or lets it fall if there is none.
        routine = null;
        IsPlaying = false;
        roller.EndControl();
    }

    // Turns the cube toward `target` at a fixed speed, so it straightens without a pop.
    private void Flatten(Quaternion target, float degreesPerSecond)
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target,
            degreesPerSecond * Time.deltaTime);
    }

    // The nearest orientation with every axis a multiple of 90 degrees (same idea as the roller's
    // snap, which is private there).
    private static Quaternion NearestFlat(Quaternion rot)
    {
        Vector3 e = rot.eulerAngles;
        return Quaternion.Euler(
            Mathf.Round(e.x / 90f) * 90f,
            Mathf.Round(e.y / 90f) * 90f,
            Mathf.Round(e.z / 90f) * 90f);
    }

    // Moves from one point to another over `duration`. The position is computed from the start
    // each frame (not added to the last one), so it is exact whatever the framerate.
    private IEnumerator Tween(Vector3 from, Vector3 to, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = curve.Evaluate(Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.LerpUnclamped(from, to, t);
            yield return null;
        }

        transform.position = to;
    }
}
