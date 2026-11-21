using System;
using UnityEngine;

// Decides what dying means for the player: switch off everything the player can do, but leave
// the body to the roller so it keeps its momentum, falls and settles. Health only announces
// Died; this listener turns that into PlayerDied, so a shatter effect, a game over screen or a
// respawn can listen later without this script knowing about them.
[RequireComponent(typeof(Health))]
public class PlayerDeath : MonoBehaviour
{
    // Raised once when the player dies. Hook effects and UI here, not into this script.
    public event Action PlayerDied;

    public bool IsDead { get; private set; }

    private Health health;
    private CubeRoller roller;
    private CubeSlam slam;
    private PlayerCubeInput moveInput;
    private PlayerAttacker attacker;

    private void Awake()
    {
        health = GetComponent<Health>();
        roller = GetComponent<CubeRoller>();
        slam = GetComponent<CubeSlam>();
        moveInput = GetComponent<PlayerCubeInput>();
        attacker = GetComponent<PlayerAttacker>();
    }

    private void OnEnable()
    {
        IsDead = false;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        if (IsDead) return;
        IsDead = true;

        // Cancel first, so the roller is never left frozen under a skill's control.
        if (slam != null)
        {
            slam.Cancel();
            slam.enabled = false;
        }
        if (attacker != null) attacker.enabled = false;
        if (moveInput != null) moveInput.enabled = false;

        PlayerDied?.Invoke();
    }

    // PlayerCubeInput normally calls Release every frame no key is held, which is what lets a
    // tipped cube settle. With the input off, this does it instead, so a cube that dies
    // mid-tilt falls back flat (or tips over) rather than freezing at an angle.
    private void Update()
    {
        if (IsDead && roller != null) roller.Release(Time.deltaTime);
    }
}
