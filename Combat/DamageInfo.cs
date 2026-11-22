using UnityEngine;

// One hit's details, passed from the attacker to Health. Data only: no logic, no events.
// Readonly, so nobody can change the damage halfway through a call. Add fields (damage type, ...)
// when something actually needs them.
public readonly struct DamageInfo
{
    public readonly float amount;

    // Who dealt it. May be destroyed by the time it is read (e.g. a projectile that vanished
    // on hit), so check `source != null` before using it.
    public readonly GameObject source;

    // An optional push that comes with the hit. The default (nothing) means damage only, so every
    // existing `new DamageInfo(amount, source)` call keeps working. Health ignores this field: a
    // listener on Health.Damaged (the player's knockback receiver) applies it, which is why a hit
    // blocked by i-frames pushes nobody.
    public readonly KnockbackInfo knockback;

    public DamageInfo(float amount, GameObject source = null, KnockbackInfo knockback = default)
    {
        this.amount = amount;
        this.source = source;
        this.knockback = knockback;
    }
}
