using System;
using System.Collections;
using UnityEngine;

// The aimed shot's cast. Right-click (the Aim action) starts it, right-click again cancels, and a
// landed hit also ends it. While it plays the cube is under the roller's Override control: it
// rises and hovers (no gravity), and when it ends the roller finds no ground and lets it fall.
// After the rise comes the aim window: slow-mo, the camera freed for manual aim, and a timer on
// real (unscaled) time. When the window runs out the aim ends like a cancel.
// Left-click (the Fire action) during the window fires the equipped shot: an AttackData asset,
// aimed along the ray through the centre of the screen, spawned by the same Use() call the
// enemies use. The shot's own cooldown starts when it is fired, and the aim ends with it, so the
// cube falls. Listeners (visuals, UI) hook into AimStarted / Fired / AimEnded.
[RequireComponent(typeof(CubeRoller))]
public class CubeAim : MonoBehaviour, ICancellable
{
    // Raised when the cast starts and when it ends, however it ends.
    public event Action AimStarted;
    public event Action Fired;      // a shot was spawned (just before AimEnded)
    public event Action AimEnded;

    // A target closer than this (m) to the cube is ignored for aiming, so a wall right against
    // the lens cannot flip the shot backwards.
    private const float MinAimDistance = 1f;

    [SerializeField] private InputReader input;
    [SerializeField] private TimeScaleController timeScale;   // optional: empty = no slow-mo
    [SerializeField] private ThirdPersonCamera aimCamera;      // optional: empty = the camera is not freed
    [SerializeField] private AimMotion motion = new AimMotion();

    [Header("Shot")]
    [Tooltip("The projectile that is fired. Changed in the loadout menu later. Its cooldown starts when it is fired.")]
    [SerializeField] private AttackData shot;
    [Tooltip("What the shot can hurt. Set to the Enemy layer. An empty mask hits nothing.")]
    [SerializeField] private LayerMask targetMask;
    [Tooltip("What the aim ray lands on to find the aim point: blocks and enemies (Ground + Enemy). Not the Player layer.")]
    [SerializeField] private LayerMask aimMask;
    [Tooltip("How far (m) the aim ray looks. Past this, the shot aims at a far point in the same direction.")]
    [SerializeField, Min(1f)] private float maxAimDistance = 100f;

    public bool IsPlaying { get; private set; }

    // Seconds left in the aim window, for a timer bar. 0 outside the aim phase.
    public float WindowRemaining { get; private set; }

    private CubeRoller roller;
    private Health health;      // optional: a landed hit ends the aim
    private Camera viewCamera;  // the camera whose screen centre is the aim point
    private Coroutine routine;
    private int slowMoHandle = -1;   // -1 = no slow-mo request held
    private bool inWindow;           // true only during the aim window: the only time Fire does anything
    private float shotReadyTime;

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
        health = GetComponent<Health>();
        viewCamera = aimCamera != null ? aimCamera.GetComponent<Camera>() : Camera.main;
    }

    private void OnEnable()
    {
        if (input != null)
        {
            input.Enable();
            input.AimPerformed += OnAimPressed;
            input.FirePerformed += OnFirePressed;
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
            input.FirePerformed -= OnFirePressed;
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

    // Fire only counts during the aim window. Outside it the click is not ours (a skill on the same
    // key handles it), and during the rise it is simply too early.
    private void OnFirePressed()
    {
        if (!inWindow || shot == null) return;

        Vector3 origin = transform.position;
        var ctx = new SkillContext(gameObject, origin, GetShotDirection(origin), null, targetMask);

        // A refused shot (no prefab on the asset) keeps the aim going and starts no cooldown.
        if (!shot.Use(ctx)) return;

        shotReadyTime = Time.time + shot.cooldown;
        Fired?.Invoke();
        End();
    }

    // The direction from the cube to whatever the centre of the screen points at. The camera ray
    // is first moved up to the cube's depth, so a wall between the camera and the cube cannot
    // become the aim point. Nothing hit = a far point along the ray.
    private Vector3 GetShotDirection(Vector3 origin)
    {
        if (viewCamera == null) return transform.forward;

        Ray ray = viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float along = Mathf.Max(0f, Vector3.Dot(origin - ray.origin, ray.direction));
        Vector3 rayStart = ray.origin + ray.direction * along;

        Vector3 aimPoint = Physics.Raycast(rayStart, ray.direction, out RaycastHit hit, maxAimDistance,
                aimMask, QueryTriggerInteraction.Ignore)
            ? hit.point
            : rayStart + ray.direction * maxAimDistance;

        Vector3 toPoint = aimPoint - origin;
        return toPoint.sqrMagnitude < MinAimDistance * MinAimDistance ? ray.direction : toPoint.normalized;
    }

    // A hit that landed (Health raises Damaged only for hits that were not blocked by i-frames).
    private void OnDamaged(DamageInfo info)
    {
        End();
    }

    // Returns false if it can't start: already playing, the shot is still cooling down, or the
    // roller refuses control (a slam is running, the cube is stunned).
    public bool Play()
    {
        if (motion == null || IsPlaying) return false;
        if (shot != null && Time.time < shotReadyTime) return false;
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
        inWindow = false;
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

        inWindow = true;
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
