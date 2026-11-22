using UnityEngine;

// One push, passed from whatever pushes (a pusher, an attack) to whatever can be pushed (IKnockable).
// Data only, readonly, like DamageInfo. Add fields (stun time, upward kick) only when something needs them.
public readonly struct KnockbackInfo
{
    // Which way to push, always a unit vector (or zero, which means "no push").
    public readonly Vector3 direction;

    // How hard. What this number means in units is up to the receiver (the player's receiver treats
    // it as the launch speed). 0 or less = no knockback at all, and the movement is not interrupted.
    public readonly float strength;

    // Extra seconds the receiver stays stunned (cannot act) after it recovers from the push, on top
    // of the receiver's own base recovery time. 0 = just the base. A heavy attack sets a bigger one.
    public readonly float stunTime;

    public bool IsNone => strength <= 0f || direction == Vector3.zero;

    public KnockbackInfo(Vector3 direction, float strength, float stunTime = 0f)
    {
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        this.strength = strength;
        this.stunTime = Mathf.Max(0f, stunTime);
    }

    // The "off" value, so DamageInfo and attackers can default to it.
    public static KnockbackInfo None => new KnockbackInfo(Vector3.zero, 0f);
}
