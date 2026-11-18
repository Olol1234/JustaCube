using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Moves the cube through a cast motion (rise, hang, slam, recover) while the roller is paused.
// Knows nothing about what the slam is for: it calls onImpact when the cube hits the ground, and
// whoever started it (a skill) decides what happens then.
// Ground only for now: it refuses to start while the cube is airborne.
[RequireComponent(typeof(CubeRoller))]
public class CubeSlam : MonoBehaviour
{
    [Header("Temporary test")]
    [Tooltip("Played by the test key (F) and the 'Play Test Slam' context menu entry. Remove once a skill uses CubeSlam.")]
    [SerializeField] private SlamMotion testMotion = new SlamMotion();

    public bool IsPlaying { get; private set; }

    private CubeRoller roller;
    private Coroutine routine;

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
    }

    // TEMPORARY: F plays the test slam. Reads the keyboard directly, which gameplay code should
    // not do (only InputReader may). Delete this with the test hook.
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) PlayTest();
    }

    // If the cube is switched off mid-cast, hand control back so the roller is never left frozen.
    private void OnDisable()
    {
        Cancel();
    }

    // Starts the motion. Returns false if it can't: already playing, airborne, or the roller is
    // already controlled by something else. onImpact is called once, when the slam lands.
    public bool Play(SlamMotion motion, Action onImpact = null)
    {
        if (motion == null || IsPlaying || roller.IsAirborne) return false;
        if (!roller.BeginControl(motion.controlMode)) return false;

        IsPlaying = true;
        routine = StartCoroutine(Run(motion, onImpact));
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

    [ContextMenu("Play Test Slam")]
    private void PlayTest()
    {
        if (!Application.isPlaying) return;
        Play(testMotion, () => Debug.Log("Slam impact"));
    }
}
