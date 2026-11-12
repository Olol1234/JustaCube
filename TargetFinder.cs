using UnityEngine;

// Answers "which Lockable should be locked onto right now?" and moves nothing.
// Picks the target closest to the screen center (smallest angle from the camera's
// forward), within range of the player and not hidden behind a block.
public class TargetFinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera. Falls back to Camera.main if empty.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("The player cube. Range and line of sight are measured from here.")]
    [SerializeField] private Transform origin;

    [Header("Selection")]
    [SerializeField] private float maxRange = 20f;
    [Tooltip("Max angle (degrees) a target may be away from the screen center.")]
    [SerializeField, Range(1f, 180f)] private float maxAngle = 60f;
    [Tooltip("Layers that hide a target (e.g. the ground/block layers).")]
    [SerializeField] private LayerMask obstructionMask;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    // Returns the best target, or null if none qualifies.
    public Lockable FindBest()
    {
        if (cameraTransform == null || origin == null) return null;

        Lockable best = null;
        float bestAngle = float.MaxValue;
        float maxRangeSqr = maxRange * maxRange;

        foreach (Lockable candidate in Lockable.All)
        {
            Vector3 aim = candidate.AimPosition;

            // Range, measured from the player (squared, to skip the square root).
            if ((aim - origin.position).sqrMagnitude > maxRangeSqr) continue;

            // Screen center: angle between the camera's forward and the direction to the target.
            float angle = Vector3.Angle(cameraTransform.forward, aim - cameraTransform.position);
            if (angle > maxAngle || angle >= bestAngle) continue;

            if (IsObstructed(candidate)) continue;

            best = candidate;
            bestAngle = angle;
        }

        return best;
    }

    // True if a block sits between the player and the target.
    // A hit on the target's own collider doesn't count.
    private bool IsObstructed(Lockable candidate)
    {
        if (!Physics.Linecast(origin.position, candidate.AimPosition, out RaycastHit hit,
                obstructionMask, QueryTriggerInteraction.Ignore))
            return false;

        return !hit.transform.IsChildOf(candidate.transform);
    }

    private void OnDrawGizmosSelected()
    {
        if (origin == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin.position, maxRange);
    }
}
