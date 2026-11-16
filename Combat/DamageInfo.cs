using UnityEngine;

// One hit's details, passed from the attacker to Health. Data only: no logic, no events.
// Readonly, so nobody can change the damage halfway through a call. Add fields (direction,
// damage type, knockback) when something actually needs them.
public readonly struct DamageInfo
{
    public readonly float amount;

    // Who dealt it. May be destroyed by the time it is read (e.g. a projectile that vanished
    // on hit), so check `source != null` before using it.
    public readonly GameObject source;

    public DamageInfo(float amount, GameObject source = null)
    {
        this.amount = amount;
        this.source = source;
    }
}
