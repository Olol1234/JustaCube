using UnityEngine;

// Answers "what is under the cube?". Moves nothing and knows nothing about input.
// Casts one ray straight down from the cube's center, so a hit also means the center of
// mass is over ground (the "50% supported" ledge rule).
public class GroundProbe : MonoBehaviour
{
    [SerializeField] private CubeMovementSettings settings;

    public bool IsGrounded { get; private set; }

    // World Y of the surface under the cube. Only meaningful while IsGrounded.
    public float GroundHeight { get; private set; }

    // Center is over ground. Same as IsGrounded for now; will differ if corner probes are added.
    public bool IsSupported => IsGrounded;

    private float HalfSize => transform.localScale.x * 0.5f;

    // Center-to-corner distance (half the diagonal) is the farthest the center can sit above
    // ground, e.g. when the cube lands on a corner.
    private float Reach => HalfSize * Mathf.Sqrt(3f) + settings.groundSkin;

    private void Update()
    {
        Probe();
    }

    public void Probe()
    {
        if (settings == null)
        {
            IsGrounded = false;
            return;
        }

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, Reach,
                settings.groundMask, QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            GroundHeight = hit.point.y;
        }
        else
        {
            IsGrounded = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (settings == null) return;

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * Reach);
    }
}
