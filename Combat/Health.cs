using System;
using UnityEngine;

// Holds one object's current health and announces what happens to it. Knows nothing about who
// listens (UI, sound, effects, death handling), so any of them can be added or deleted freely.
// It does not destroy anything itself: a listener on Died decides what dying means.
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    [Tooltip("Seconds of invulnerability after taking a hit (i-frames). 0 = off, so a hit can land every frame. The player uses about 0.6.")]
    [SerializeField, Min(0f)] private float hitInvulnerability = 0f;

    // Two ways to be invulnerable: a timer (after a hit, or Grant) and holds (something that lasts
    // as long as an action does, like a dash). A hold must always be released by whoever took it.
    private float invulnerableUntil;
    private int invulnerableHolds;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => invulnerableHolds > 0 || Time.time < invulnerableUntil;

    public event Action<DamageInfo> Damaged;
    public event Action<float> Healed;           // amount actually healed
    public event Action<float, float> Changed;   // current, max (for health bars)
    public event Action Died;

    // OnEnable, not Awake: a pooled object that is re-enabled starts with full health again.
    private void OnEnable()
    {
        Current = maxHealth;
        IsDead = false;
        invulnerableUntil = 0f;
        invulnerableHolds = 0;
        Changed?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(DamageInfo info)
    {
        // Dead objects ignore hits, so two hits in one frame can't raise Died twice.
        if (IsDead || info.amount <= 0f) return;

        // A hit during i-frames does nothing at all: no damage and no Damaged event, so listeners
        // (knockback, hit flash, sound) never fire for it either.
        if (IsInvulnerable) return;

        Current = Mathf.Max(Current - info.amount, 0f);
        if (hitInvulnerability > 0f) GrantInvulnerability(hitInvulnerability);
        Damaged?.Invoke(info);
        Changed?.Invoke(Current, maxHealth);

        if (Current > 0f) return;

        IsDead = true;
        Died?.Invoke();
    }

    // Invulnerable for this many seconds from now. A shorter request never cuts a longer one short.
    public void GrantInvulnerability(float seconds)
    {
        if (seconds <= 0f) return;
        invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
    }

    // Invulnerable until released. Counted, so two sources holding at once do not cancel each other.
    public void HoldInvulnerable()
    {
        invulnerableHolds++;
    }

    public void ReleaseInvulnerable()
    {
        invulnerableHolds = Mathf.Max(0, invulnerableHolds - 1);
    }

    // Changes the max at runtime (an item, a skin, a heart container). Current stays as it is when the
    // max goes up (the new part starts empty, like Elden Ring) and is clamped when it goes down.
    // Works on a dead object too, but never revives it.
    public void SetMaxHealth(float newMax)
    {
        // [Min] on the field only guards the Inspector, so guard code calls here (bars divide by Max).
        newMax = Mathf.Max(newMax, 1f);
        if (Mathf.Approximately(newMax, maxHealth)) return;

        maxHealth = newMax;
        Current = Mathf.Min(Current, maxHealth);
        Changed?.Invoke(Current, maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        float healed = Mathf.Min(amount, maxHealth - Current);
        if (healed <= 0f) return;

        Current += healed;
        Healed?.Invoke(healed);
        Changed?.Invoke(Current, maxHealth);
    }
}
