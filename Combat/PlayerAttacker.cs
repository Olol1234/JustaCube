using UnityEngine;

// Fires the skill in a slot when its key is pressed: checks the cooldown, works out the direction,
// builds a SkillContext and hands it to the skill's Use. It does not know what kind of skill it
// is, and it moves nothing and deals no damage itself.
// Locked on: aims at the target's position at the moment of firing. Unlocked: camera forward.
// (A projectile skill then flies a fixed line, it never steers.)
public class PlayerAttacker : MonoBehaviour
{
    [SerializeField] private InputReader input;
    [SerializeField] private ThirdPersonCamera cameraRig;

    [Tooltip("One skill per slot, in the same order as the InputReader's skill actions. An empty slot does nothing.")]
    [SerializeField] private SkillData[] slots = new SkillData[3];

    [Tooltip("Layers the player's skills can hurt (e.g. Enemy). Do not include the player's own layer.")]
    [SerializeField] private LayerMask targetMask;

    private float[] readyAt;          // Time.time at which each slot may fire again
    private Lockable lockedTarget;
    private ICancellable[] casts;     // the player's interruptible casts (slam, aim...): none may run when a skill starts
    private CubeAim aim;              // optional: tells which frame an aim ended on

    private void Awake()
    {
        casts = GetComponents<ICancellable>();
        aim = GetComponent<CubeAim>();
    }

    private void OnEnable()
    {
        readyAt = new float[slots.Length];

        input.Enable();
        input.SkillPerformed += OnSkill;
        cameraRig.TargetLocked += OnTargetLocked;
        cameraRig.TargetUnlocked += OnTargetUnlocked;
    }

    private void OnDisable()
    {
        input.SkillPerformed -= OnSkill;
        cameraRig.TargetLocked -= OnTargetLocked;
        cameraRig.TargetUnlocked -= OnTargetUnlocked;
        input.Disable();

        lockedTarget = null;
    }

    private void OnTargetLocked(Lockable target) => lockedTarget = target;
    private void OnTargetUnlocked() => lockedTarget = null;

    private void OnSkill(int slot)
    {
        if (slot < 0 || slot >= slots.Length) return;
        if (IsCastPlaying()) return;   // whatever started first finishes first, nothing interrupts it

        // The click that fired or cancelled the aim may also be this skill's key. The aim has already
        // ended by now (or will in this same frame), so IsCastPlaying alone cannot catch it.
        if (aim != null && aim.LastEndFrame == Time.frameCount) return;

        SkillData skill = slots[slot];
        if (skill == null) return;
        if (Time.time < readyAt[slot]) return;

        var ctx = new SkillContext(gameObject, transform.position, GetAimDirection(), lockedTarget, targetMask);

        // A refused cast (e.g. slam while airborne) must not start the cooldown.
        if (!skill.Use(ctx)) return;

        readyAt[slot] = Time.time + skill.cooldown;
    }

    private bool IsCastPlaying()
    {
        for (int i = 0; i < casts.Length; i++)
        {
            if (casts[i].IsPlaying) return true;
        }
        return false;
    }

    private Vector3 GetAimDirection()
    {
        // `!= null` (not `is not null`): a destroyed target is a "fake null" Unity object.
        if (lockedTarget != null)
        {
            Vector3 toTarget = lockedTarget.AimPosition - transform.position;
            if (toTarget.sqrMagnitude > 0.0001f) return toTarget.normalized;
        }

        return cameraRig.transform.forward;
    }
}
