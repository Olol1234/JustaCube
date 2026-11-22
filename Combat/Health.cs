using System;
using UnityEngine;

// Holds one object's current health and announces what happens to it. Knows nothing about who
// listens (UI, sound, effects, death handling), so any of them can be added or deleted freely.
// It does not destroy anything itself: a listener on Died decides what dying means.
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;

    public float Max => maxHealth;
    public float Current { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<DamageInfo> Damaged;
    public event Action<float> Healed;           // amount actually healed
    public event Action<float, float> Changed;   // current, max (for health bars)
    public event Action Died;

    // OnEnable, not Awake: a pooled object that is re-enabled starts with full health again.
    private void OnEnable()
    {
        Current = maxHealth;
        IsDead = false;
        Changed?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(DamageInfo info)
    {
        // Dead objects ignore hits, so two hits in one frame can't raise Died twice.
        if (IsDead || info.amount <= 0f) return;

        Current = Mathf.Max(Current - info.amount, 0f);
        Damaged?.Invoke(info);
        Changed?.Invoke(Current, maxHealth);

        if (Current > 0f) return;

        IsDead = true;
        Died?.Invoke();
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
