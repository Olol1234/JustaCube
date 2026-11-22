using System;
using System.Collections.Generic;
using UnityEngine;

// The ONLY script that writes Time.timeScale. Things that want to change game speed ask this
// asset instead of touching Time themselves, so they cannot undo each other: the aim's slow-mo,
// a pause, and later a hit-stop.
//
// Effective scale: paused = 0, otherwise the SMALLEST active slow-mo request, otherwise 1.
//  - RequestSlowMo returns a handle. Each requester releases its own handle, so two slow-mos do not
//    cancel each other, and a pause on top of a slow-mo goes back to that slow-mo when it ends.
//  - Whoever requests must Release in OnDisable, or the game stays slow if it is disabled mid-way.
//
// It is an asset (no singleton): scripts hold a reference to it in the Inspector. Anything that
// must ignore slow-mo (the aim window timer, camera look) uses Time.unscaledDeltaTime and should
// also stop while Paused (or listen to ScaleChanged).
//
// Not covered here: the pause menu, swapping the input map and audio. A menu just sets Paused.
[CreateAssetMenu(menuName = "TheOneCube/Time Scale Controller", fileName = "TimeScaleController")]
public class TimeScaleController : ScriptableObject
{
    [Tooltip("The fixed physics step at normal speed. Match Project Settings > Time > Fixed Timestep (default 0.02). It is scaled with the game speed so physics stays smooth in slow-mo.")]
    [SerializeField, Min(0.001f)] private float baseFixedDeltaTime = 0.02f;

    [Tooltip("Slow-mo requests are clamped to this minimum, so a bad value cannot freeze the game like a pause.")]
    [SerializeField, Range(0.01f, 1f)] private float minSlowMoScale = 0.05f;

    // Runtime state, not saved. A ScriptableObject keeps its values between play sessions in the
    // Editor, so OnEnable / OnDisable reset it.
    private readonly Dictionary<int, float> slowMoRequests = new Dictionary<int, float>();
    private int nextHandle = 1;
    private bool paused;
    private int testHandle = -1;

    public float Scale { get; private set; } = 1f;

    public bool Paused
    {
        get => paused;
        set
        {
            if (paused == value) return;
            paused = value;
            Apply();
        }
    }

    // The new effective scale, whenever it changes (a slow-mo screen effect can listen to this).
    public event Action<float> ScaleChanged;

    private void OnEnable()
    {
        ResetState();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void ResetState()
    {
        slowMoRequests.Clear();
        paused = false;
        Scale = 1f;
        testHandle = -1;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = baseFixedDeltaTime;
    }

    // Asks for the game to run at this fraction of normal speed (0.2 = five times slower).
    // Returns a handle to give to Release.
    public int RequestSlowMo(float scale)
    {
        int handle = nextHandle++;
        slowMoRequests[handle] = Mathf.Clamp(scale, minSlowMoScale, 1f);
        Apply();
        return handle;
    }

    // Gives a request back. An unknown or already released handle does nothing.
    public void Release(int handle)
    {
        if (!slowMoRequests.Remove(handle)) return;
        Apply();
    }

    private void Apply()
    {
        float target = 1f;
        foreach (float requested in slowMoRequests.Values)
            target = Mathf.Min(target, requested);
        if (paused) target = 0f;

        Time.timeScale = target;

        // A fixed step of 0 is invalid, so keep the last one while paused.
        if (target > 0f) Time.fixedDeltaTime = baseFixedDeltaTime * target;

        if (Mathf.Approximately(target, Scale)) return;
        Scale = target;
        ScaleChanged?.Invoke(Scale);
    }

    // TEMPORARY test entries: right-click the asset's header in the Inspector while playing.
    // Delete them once the aim uses this.
    [ContextMenu("Test: toggle slow-mo 0.2")]
    private void TestToggleSlowMo()
    {
        if (testHandle < 0)
        {
            testHandle = RequestSlowMo(0.2f);
        }
        else
        {
            Release(testHandle);
            testHandle = -1;
        }
    }

    [ContextMenu("Test: toggle pause")]
    private void TestTogglePause()
    {
        Paused = !Paused;
    }
}
