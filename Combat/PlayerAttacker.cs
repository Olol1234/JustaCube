using UnityEngine;

// Fires the skill in a slot when its key is pressed: checks the cooldown, works out the direction
// and spawns a Projectile. It moves nothing and deals no damage itself (Projectile does that).
// Locked on: aims at the target's position at the moment of firing. Unlocked: camera forward.
// Either way the shot then flies a fixed line, it never steers.
public class PlayerAttacker : MonoBehaviour
{
    [SerializeField] private InputReader input;
    [SerializeField] private ThirdPersonCamera cameraRig;

    [Tooltip("One attack per skill slot, in the same order as the InputReader's skill actions. An empty slot does nothing.")]
    [SerializeField] private AttackData[] slots = new AttackData[3];

    [SerializeField] private Projectile projectilePrefab;

    [Tooltip("Layers the player's shots can hurt (e.g. Enemy). Do not include the player's own layer.")]
    [SerializeField] private LayerMask targetMask;

    [Tooltip("How far (m) in front of the cube the shot starts, so it begins clear of the cube.")]
    [SerializeField, Min(0f)] private float spawnOffset = 1.2f;

    private float[] readyAt;          // Time.time at which each slot may fire again
    private Lockable lockedTarget;

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

        AttackData data = slots[slot];
        if (data == null) return;
        if (Time.time < readyAt[slot]) return;

        Vector3 direction = GetAimDirection();
        Vector3 spawnPos = transform.position + direction * spawnOffset;

        Projectile shot = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
        shot.Init(data, direction, gameObject, targetMask);

        readyAt[slot] = Time.time + data.cooldown;
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
