using System.Collections.Generic;
using UnityEngine;

// An area that hurts whatever Health is inside it, every tickInterval seconds. Used for hazards
// (lava, spikes) and, with a huge damage and a big flat box under the map, as a kill plane.
// It uses the project's one damage method: a physics query on a layer mask, then Health.TakeDamage.
// No trigger events, so the scripted player (no real physics movement) is found the same way as
// everything else. The BoxCollider is only a size you can drag in the Scene view.
[RequireComponent(typeof(BoxCollider))]
public class DamageZone : MonoBehaviour
{
    [Tooltip("Damage dealt to each Health every tick. Use a huge number for a kill plane.")]
    [SerializeField, Min(0f)] private float damage = 10f;

    [Tooltip("Seconds between hits while something is inside.")]
    [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;

    [Tooltip("Who can be hurt. Set it to the Player layer. Never the layers of blocks.")]
    [SerializeField] private LayerMask targetMask;

    private BoxCollider box;
    private float timer;

    // Allocated once, so polling creates no garbage.
    private readonly Collider[] buffer = new Collider[16];
    // A target with several colliders must be hurt once per tick, not once per collider.
    private readonly HashSet<Health> hitThisTick = new HashSet<Health>();

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true; // only a size: nothing physical should happen here
    }

    private void OnEnable()
    {
        timer = 0f; // the first check happens right away
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = tickInterval;

        DamageOverlapping();
    }

    private void DamageOverlapping()
    {
        // The box is in the zone's local space, so scale and rotation of the object are respected.
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
        halfExtents = new Vector3(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y), Mathf.Abs(halfExtents.z));

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, transform.rotation, targetMask);

        hitThisTick.Clear();
        for (int i = 0; i < count; i++)
        {
            Health health = buffer[i].GetComponentInParent<Health>();
            if (health == null) continue;
            if (!hitThisTick.Add(health)) continue; // already hurt this tick through another collider

            health.TakeDamage(new DamageInfo(damage, gameObject));
        }
    }

    // Shows the zone in the Scene view, because the BoxCollider is a trigger with no visible mesh.
    private void OnDrawGizmos()
    {
        BoxCollider b = box != null ? box : GetComponent<BoxCollider>();
        if (b == null) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
