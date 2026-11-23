using UnityEngine;

// Lets the player be pushed. It is the player's IKnockable and the only place that decides what a
// push does to the cube: cancel a running slam, then hand the push to CubeRoller.
//
// Two doors lead to the same ApplyKnockback:
//  - A pure pusher (the weird ground, a bounce pad) finds this component with
//    GetComponentInParent<IKnockable>() and calls it directly. I-frames do not stop that, because a
//    push is not damage.
//  - A damaging attack carries a KnockbackInfo in its DamageInfo. Health raises Damaged only for a
//    hit that really landed, so a hit blocked by i-frames (a dash) pushes nobody.
// Health and the roller do not know this script exists: delete it and the player simply cannot be
// pushed, everything else keeps working.
[RequireComponent(typeof(CubeRoller))]
[RequireComponent(typeof(Health))]
public class PlayerKnockback : MonoBehaviour, IKnockable
{
    private CubeRoller roller;
    private Health health;
    private ICancellable[] casts;   // every interruptible cast on the player (slam, aim...), may be empty

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
        health = GetComponent<Health>();
        casts = GetComponents<ICancellable>();
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
    }

    private void OnDamaged(DamageInfo info)
    {
        ApplyKnockback(info.knockback);
    }

    public void ApplyKnockback(KnockbackInfo info)
    {
        if (info.IsNone) return;

        // A cast is a commitment, but a push interrupts it completely. Cancel gives the cube back
        // to the roller (EndControl), which refuses a push while a skill controls it.
        foreach (ICancellable cast in casts)
        {
            if (cast.IsPlaying) cast.Cancel();
        }

        roller.ApplyKnockback(info);
    }
}
