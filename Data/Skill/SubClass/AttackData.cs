using UnityEngine;

// A projectile skill: spawns a Projectile that flies a fixed straight line and damages what it
// touches. The numbers live here, the flying and hitting live in Projectile.
// `cooldown` is inherited from SkillData.
[CreateAssetMenu(fileName = "AttackData", menuName = "TheOneCube/Attack Data")]
public class AttackData : SkillData
{
    [Header("Damage")]
    [Tooltip("Damage dealt to each target it hits.")]
    [Min(0f)] public float damage = 10f;

    [Header("Flight")]
    [Tooltip("Speed (m/s) along its fixed line. No homing.")]
    [Min(0.1f)] public float speed = 20f;

    [Tooltip("Distance (m) it can travel before it disappears.")]
    [Min(0.1f)] public float range = 30f;

    [Tooltip("Edge length (m) of the cube-shaped projectile. Also the size of its hit area.")]
    [Min(0.05f)] public float size = 0.5f;

    [Header("Hit rules")]
    [Tooltip("How many targets it can hit before it stops. 1 = stops at the first hit.")]
    [Min(1)] public int pierceCount = 1;

    [Tooltip("Does a block (anything on the ground mask) stop it? Off = it flies through walls.")]
    public bool stopsAtBlocks = true;

    [Tooltip("Hit each target at most once. Off = a piercing shot could hit the same target again on later frames.")]
    public bool hitOncePerTarget = true;

    [Header("Spawn")]
    [Tooltip("What gets spawned. Different attacks can use different looks. The prefab needs a Projectile component.")]
    public Projectile projectilePrefab;

    [Tooltip("How far (m) in front of the firer the shot starts, so it begins clear of the cube (a sweep does not detect colliders it already overlaps).")]
    [Min(0f)] public float spawnOffset = 1.2f;

    public override bool Use(SkillContext ctx)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: no projectile prefab assigned.", this);
            return false;
        }

        Vector3 spawnPos = ctx.origin + ctx.direction * spawnOffset;

        Projectile shot = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(ctx.direction));
        shot.Init(this, ctx.direction, ctx.owner, ctx.targetMask);
        return true;
    }
}
