using UnityEngine;

// Everything a skill needs at the moment it fires. Built fresh by the caller (PlayerAttacker, or a
// boss script) on every use and passed to SkillData.Use. Data only: no logic, never stored in an
// asset (it holds scene objects). Add fields only when several skills could use them.
public readonly struct SkillContext
{
    // Who fired. May be destroyed later, so check `owner != null` before using it.
    public readonly GameObject owner;

    // Where the skill starts (world position). The caller decides, e.g. the cube's position.
    public readonly Vector3 origin;

    // Which way it is aimed, already worked out by the caller. Always normalized (length 1),
    // the constructor guarantees it, so skills never have to normalize it again.
    public readonly Vector3 direction;

    // The locked-on target, or null. Most skills ignore it. A destroyed target is a "fake null",
    // so the constructor turns it into a real null and `target != null` is always safe.
    public readonly Lockable target;

    // Layers this skill can hurt. Comes from the caller (the player hurts Enemy, a boss hurts
    // the player), never from the asset, so one asset works for both sides.
    public readonly LayerMask targetMask;

    public SkillContext(GameObject owner, Vector3 origin, Vector3 direction, Lockable target, LayerMask targetMask)
    {
        this.owner = owner;
        this.origin = origin;
        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        this.target = target != null ? target : null;
        this.targetMask = targetMask;
    }
}
