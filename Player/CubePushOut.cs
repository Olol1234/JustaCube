using UnityEngine;

// Eases the cube out of an enemy it overlaps, for example after an enemy walks into it.
// This is not knockback: there is no hop or stun, and nothing the cube is doing is cancelled.
// It pushes flat, away from the enemy's center, and stops at walls.
[RequireComponent(typeof(CubeRoller))]
public class CubePushOut : MonoBehaviour
{
    [Tooltip("What pushes the cube out (Enemy). With nothing selected, this does nothing.")]
    [SerializeField] private LayerMask pushMask;
    [Tooltip("Units per second. Keep it faster than the enemies walk.")]
    [Min(0f)] [SerializeField] private float pushSpeed = 5f;
    [Tooltip("Shrinks the test box on every side. Match Collision Skin in the movement settings.")]
    [Min(0f)] [SerializeField] private float skin = 0.05f;

    private readonly Collider[] hits = new Collider[8];
    private CubeRoller roller;

    // The cube's side is its scale. The skin keeps resting on the floor from counting as a hit.
    private Vector3 HalfExtents => Vector3.one * (transform.localScale.x * 0.5f - skin);

    private void Awake()
    {
        roller = GetComponent<CubeRoller>();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || roller.IsControlled) return; // paused, or a skill moves the cube

        int count = Physics.OverlapBoxNonAlloc(transform.position, HalfExtents, hits,
            transform.rotation, pushMask, QueryTriggerInteraction.Ignore);
        if (count == 0) return;

        Vector3 offset = PushDirection(count) * (pushSpeed * deltaTime);
        roller.Shift(StopAtWalls(offset));
    }

    // Flat direction away from everything overlapped.
    private Vector3 PushDirection(int count)
    {
        Vector3 total = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Vector3 away = transform.position - hits[i].bounds.center;
            away.y = 0f;
            total += away.normalized;
        }

        // Lined up with the center, or two pushes cancelling out: any flat direction will do.
        if (total.sqrMagnitude < 0.0001f)
        {
            total = hits[0].transform.forward;
            total.y = 0f;
        }

        return total.normalized;
    }

    // The part of the offset that doesn't go into a wall. One axis at a time, so the cube can
    // still slide along a wall.
    private Vector3 StopAtWalls(Vector3 offset)
    {
        // Everything solid except what is doing the pushing: the cube already overlaps that.
        int wallMask = roller.GroundMask & ~pushMask;
        Vector3 allowed = Vector3.zero;

        Vector3 stepX = new Vector3(offset.x, 0f, 0f);
        if (!HitsWall(transform.position + stepX, wallMask)) allowed += stepX;

        Vector3 stepZ = new Vector3(0f, 0f, offset.z);
        if (!HitsWall(transform.position + allowed + stepZ, wallMask)) allowed += stepZ;

        return allowed;
    }

    private bool HitsWall(Vector3 position, int wallMask)
    {
        return Physics.CheckBox(position, HalfExtents, transform.rotation, wallMask,
            QueryTriggerInteraction.Ignore);
    }
}
