// Implemented by anything that can be pushed (the player, later a heavy mob). A pusher finds it with
// GetComponentInParent<IKnockable>() and calls ApplyKnockback; it never needs to know what the
// target is. Not implementing it is how something is immune (the boss, a tower): there is no flag
// to switch off. Damage is not part of this: a thing either hurts (Health) or pushes (this).
public interface IKnockable
{
    void ApplyKnockback(KnockbackInfo info);
}
