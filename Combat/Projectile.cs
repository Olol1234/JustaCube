using System;
using System.Collections.Generic;
using UnityEngine;

// A cube that flies in a straight line and damages what it touches. It only moves and hits:
// the caller (player or boss script) decides the direction and who can be hurt, and the numbers
// come from an AttackData asset. It never reads the lock-on target, so there is no homing.
public class Projectile : MonoBehaviour
{
    [Tooltip("Layers that stop the shot when AttackData.stopsAtBlocks is on (the ground / block layer).")]
    [SerializeField] private LayerMask blockMask;

    // Shared by every projectile: avoids allocating a new array for each sweep.
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private static readonly Comparer<RaycastHit> byDistance =
        Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    private AttackData data;
    private Vector3 direction;
    private GameObject source;
    private LayerMask targetMask;
    private float travelled;
    private int hitCount;
    private bool initialized;

    // Targets already hit, so a target with several colliders is not hit twice in one sweep.
    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();

    // Call right after spawning. `dir` must be worked out by the caller (camera forward, or the
    // direction to the locked target at the moment of firing). `targets` = layers this shot can hurt.
    public void Init(AttackData attackData, Vector3 dir, GameObject owner, LayerMask targets)
    {
        data = attackData;
        direction = dir.normalized;
        source = owner;
        targetMask = targets;

        transform.localScale = Vector3.one * data.size;
        transform.rotation = Quaternion.LookRotation(direction);

        travelled = 0f;
        hitCount = 0;
        alreadyHit.Clear();
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        float step = Mathf.Min(data.speed * Time.deltaTime, data.range - travelled);
        if (step <= 0f)
        {
            Despawn();
            return;
        }

        // A sweep (not a point check) so a fast, small shot cannot skip over a thin target in one frame.
        LayerMask mask = data.stopsAtBlocks ? (targetMask | blockMask) : targetMask;
        int count = Physics.BoxCastNonAlloc(
            transform.position,
            Vector3.one * (data.size * 0.5f),
            direction,
            hitBuffer,
            transform.rotation,
            step,
            mask,
            QueryTriggerInteraction.Ignore);

        // The buffer is unordered, so sort by distance: the closest thing in the path comes first.
        Array.Sort(hitBuffer, 0, count, byDistance);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            int layerBit = 1 << hit.collider.gameObject.layer;

            if (data.stopsAtBlocks && (blockMask.value & layerBit) != 0)
            {
                // Hit a block: stop at the surface.
                transform.position += direction * hit.distance;
                Despawn();
                return;
            }

            if ((targetMask.value & layerBit) == 0) continue;

            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == null || health.gameObject == source) continue;
            if (data.hitOncePerTarget && !alreadyHit.Add(health)) continue;

            health.TakeDamage(new DamageInfo(data.damage, source));
            hitCount++;

            if (hitCount >= data.pierceCount)
            {
                transform.position += direction * hit.distance;
                Despawn();
                return;
            }
        }

        transform.position += direction * step;
        travelled += step;
    }

    // One place to change when pooling is added later.
    private void Despawn()
    {
        initialized = false;
        Destroy(gameObject);
    }
}
