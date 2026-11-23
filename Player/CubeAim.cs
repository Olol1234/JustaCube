using System;
using System.Collections;
using UnityEngine;

// The aimed shot's cast. Right-click (the Aim action) starts it, right-click again cancels, and a
// landed hit also ends it. While it plays the cube is under the roller's Override control: it
// rises and hovers (no gravity), and when it ends the roller finds no ground and lets it fall.
// After the rise comes the aim window: slow-mo, the camera freed for manual aim, and a timer on
// real (unscaled) time. When the window runs out the aim ends like a cancel. Firing is added as
// a separate step. Listeners (visuals, UI) hook into AimStarted / AimEnded.
[RequireComponent(typeof(CubeRoller))]
public class CubeAim : MonoBehaviour, ICancellable
{
    // Raised when the cast starts and when it ends, however it ends.
    public event Action AimStarted;
    public event Action AimEnded;

    [SerializeField] private InputReader input;
    [SerializeField] private TimeScaleController timeScale;   // optional: empty = no slow-mo
    [SerializeField] private ThirdPersonCamera aimCamera;      // optional: empty = the camera is not freed
    [SerializeField] private AimMotion motion = new AimMotion();

    public bool IsPlaying { get; private set; }

    // Seconds left in the aim window, for a timer bar. 0 outside the aim phase.
    public float WindowRemaining { get; private set; }

    private CubeRoller roller;
    private Health health;      // optional: a landed hit ends the aim
    private Coroutine routine;
    private int slowMoHandle = -1;   // -1 = no slow-mo request held

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.Enable();
            input.AimPerformed += OnAimPressed;
        }
        if (health != null) health.Damaged += OnDamaged;
    }

    // If the cube is switched off mid-aim (death, a disabled component), hand control back so the
    // roller is never left frozen.
    private void OnDisable()
    {
        if (health != null) health.Damaged -= OnDamaged;
        if (input != null)
        {
            input.AimPerformed -= OnAimPressed;
            input.Disable();
        }
        End();
    }

    // The aim key toggles: start if idle, cancel if playing.
    private void OnAimPressed()
    {
        if (IsPlaying) End();
        else Play();
    }

    // A hit that landed (Health raises Damaged only for hits that were not blocked by i-frames).
    private void OnDamaged(DamageInfo info)
    {
        End();
    }

    // Returns false if it can't start: already playing, or the roller refuses control (a slam is
    // running, the cube is stunned).
    public bool Play()
    {
        if (motion == null || IsPlaying) return false;
        if (!roller.BeginControl(ControlMode.Override)) return false;

        IsPlaying = true;
        routine = StartCoroutine(Run());
        AimStarted?.Invoke();
        return true;
    }

    public void Cancel()
    {
        End();
    }

    // The single way out. Fire, cancel, timeout, a hit and OnDisable all end here, so slow-mo can
    // never be left on and the camera can never be left free.
    private void End()
    {
        if (!IsPlaying) return;

        if (routine != null) StopCoroutine(routine);
        routine = null;
        IsPlaying = false;
        WindowRemaining = 0f;

        if (slowMoHandle >= 0 && timeScale != null) timeScale.Release(slowMoHandle);
        slowMoHandle = -1;
        if (aimCamera != null) aimCamera.ExitAim();

        // Override: the roller rests the cube on the ground, or lets it fall if there is none.
        if (roller != null) roller.EndControl();
        AimEnded?.Invoke();
    }

    private IEnumerator Run()
    {
        Vector3 start = transform.position;
        Vector3 top = start + Vector3.up * motion.riseHeight;
        Quaternion flat = NearestFlat(transform.rotation);

        // Rise. The position is computed from the start each frame, so it is exact at any framerate.
        float elapsed = 0f;
        while (elapsed < motion.riseTime)
        {
            elapsed += Time.deltaTime;
            float t = motion.riseCurve.Evaluate(Mathf.Clamp01(elapsed / motion.riseTime));
            transform.position = Vector3.LerpUnclamped(start, top, t);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, flat,
                motion.flattenSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = top;
        transform.rotation = flat;

        // Aim window: slow-mo and a free camera, for a fixed number of real seconds. The timer uses
        // unscaled time (or the window would stretch with the slow-mo) and stands still while the
        // game is paused.
        if (timeScale != null) slowMoHandle = timeScale.RequestSlowMo(motion.slowMoScale);
        if (aimCamera != null) aimCamera.EnterAim();

        WindowRemaining = motion.aimWindow;
        while (WindowRemaining > 0f)
        {
            if (timeScale == null || !timeScale.Paused) WindowRemaining -= Time.unscaledDeltaTime;
            yield return null;
        }

        // The window ran out: same as a cancel.
        End();
    }

    // The orientation with every axis a multiple of 90 degrees nearest to `rot`. The same helper
    // exists privately in CubeSlam: if a third script needs it, move it somewhere shared.
    private static Quaternion NearestFlat(Quaternion rot)
    {
        Vector3 e = rot.eulerAngles;
        return Quaternion.Euler(
            Mathf.Round(e.x / 90f) * 90f,
            Mathf.Round(e.y / 90f) * 90f,
            Mathf.Round(e.z / 90f) * 90f);
    }
}
