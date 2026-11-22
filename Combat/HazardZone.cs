using System.Collections.Generic;
using UnityEngine;

// An area that hurts and/or pushes whatever is inside it, every tickInterval seconds. One script for
// lava and spikes (damage), bounce pads and wind (push), a spiked wall (both) and, with a huge
// damage and a big flat box under the map, a kill plane.
// It uses the project's one query method: a physics query on a layer mask, no trigger events.
// The BoxCollider is only a size you can drag in the Scene view.
//
// Each target is handled in exactly one way, so nothing is pushed twice:
//  - damage > 0: an attack. Health.TakeDamage gets a DamageInfo that carries the push, and the
//    target's knockback receiver applies it when Health.Damaged fires. A hit blocked by i-frames
//    (a dash, the moments after a hit) therefore does no damage and no push.
//  - damage = 0 and knockbackStrength > 0: a pure push. It calls IKnockable directly, and i-frames
//    do not stop it, because a push is not damage.
//  - both 0: it does nothing.
[RequireComponent(typeof(BoxCollider))]
public class HazardZone : MonoBehaviour
{
    public enum PushDirection
    {
        [Tooltip("Away from the zone's center, flattened onto the ground (a bumper).")]
        AwayFromCenter,
        [Tooltip("Always along the zone's blue (forward) axis, flattened onto the ground (wind, a conveyor).")]
        ZoneForward
    }

    [Header("Damage")]
    [Tooltip("Damage dealt to each Health every tick. 0 = no damage (a pure push if knockback is set). Use a huge number for a kill plane.")]
    [SerializeField, Min(0f)] private float damage = 10f;

    [Header("Knockback")]
    [Tooltip("Launch speed of the push (m/s). 0 = no push. With damage the push rides on the hit; without damage it is a pure push.")]
    [SerializeField, Min(0f)] private float knockbackStrength = 0f;

    [Tooltip("Extra stun seconds on top of the receiver's own recovery time.")]
    [SerializeField, Min(0f)] private float knockbackStunTime = 0f;

    [SerializeField] private PushDirection pushDirection = PushDirection.AwayFromCenter;

    [Header("Timing and targets")]
    [Tooltip("Seconds between hits while something is inside. With a push, keep it longer than the stun, or a target still flying is pushed again.")]
    [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;

    [Tooltip("Who is affected. Set it to the Player layer. Never the layers of blocks.")]
    [SerializeField] private LayerMask targetMask;

    private BoxCollider box;
    private float timer;

    // Allocated once, so polling creates no garbage.
    private readonly Collider[] buffer = new Collider[16];
    // A target with several colliders must be handled once per tick, not once per collider.
    private readonly HashSet<object> handledThisTick = new HashSet<object>();

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

        AffectOverlapping();
    }

    private void AffectOverlapping()
    {
        bool hurts = damage > 0f;
        bool pushes = knockbackStrength > 0f;
        if (!hurts && !pushes) return;

        // The box is in the zone's local space, so scale and rotation of the object are respected.
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 halfExtents = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
        halfExtents = new Vector3(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y), Mathf.Abs(halfExtents.z));

        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, transform.rotation, targetMask);

        handledThisTick.Clear();
        for (int i = 0; i < count; i++)
        {
            Collider hit = buffer[i];
            KnockbackInfo knockback = BuildKnockback(hit.bounds.center, center);

            if (hurts)
            {
                Health health = hit.GetComponentInParent<Health>();
                if (health == null || !handledThisTick.Add(health)) continue;

                health.TakeDamage(new DamageInfo(damage, gameObject, knockback));
            }
            else
            {
                IKnockable knockable = hit.GetComponentInParent<IKnockable>();
                if (knockable == null || !handledThisTick.Add(knockable)) continue;

                knockable.ApplyKnockback(knockback);
            }
        }
    }

    // Direction is flattened: the receiver adds its own hop, and a zone should not shove a cube
    // into the ground or the sky.
    private KnockbackInfo BuildKnockback(Vector3 targetPosition, Vector3 zoneCenter)
    {
        if (knockbackStrength <= 0f) return KnockbackInfo.None;

        Vector3 direction = pushDirection == PushDirection.ZoneForward
            ? transform.forward
            : targetPosition - zoneCenter;

        direction.y = 0f;

        // A target exactly at the center has no "away": fall back to the zone's forward axis.
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0f;
        }

        return new KnockbackInfo(direction, knockbackStrength, knockbackStunTime);
    }

    // Shows the zone in the Scene view, because the BoxCollider is a trigger with no visible mesh.
    // Red = damage, blue = pure push, purple = both. An arrow shows a fixed push direction.
    private void OnDrawGizmos()
    {
        BoxCollider b = box != null ? box : GetComponent<BoxCollider>();
        if (b == null) return;

        bool hurts = damage > 0f;
        bool pushes = knockbackStrength > 0f;
        Color color = hurts && pushes ? new Color(0.8f, 0.2f, 1f)
                    : pushes ? new Color(0.2f, 0.5f, 1f)
                    : new Color(1f, 0.2f, 0.2f);

        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = color;
        Gizmos.DrawWireCube(b.center, b.size);
        Gizmos.matrix = old;

        if (pushes && pushDirection == PushDirection.ZoneForward)
        {
            Vector3 start = transform.TransformPoint(b.center);
            Vector3 flatForward = transform.forward;
            flatForward.y = 0f;
            Gizmos.color = color;
            Gizmos.DrawRay(start, flatForward.normalized * 2f);
        }
    }
}
