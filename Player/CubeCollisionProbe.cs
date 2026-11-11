using UnityEngine;

// Answers "would the cube hit a block if it were here?". Moves nothing and knows nothing about
// input. Tests a box the size of the cube (slightly shrunk) at any position and rotation.
public class CubeCollisionProbe : MonoBehaviour
{
    [SerializeField] private CubeMovementSettings settings;

    // Half of the cube's side, minus the skin. The skin makes the test box a bit smaller than the
    // cube, so resting on the floor or touching a wall is not counted as a hit.
    private float HalfExtent => transform.localScale.x * 0.5f - settings.collisionSkin;

    // True if the cube, placed at this position and rotation, would overlap a block.
    public bool WouldOverlap(Vector3 position, Quaternion rotation)
    {
        if (settings == null) return false;

        return Physics.CheckBox(position, Vector3.one * HalfExtent, rotation,
            settings.groundMask, QueryTriggerInteraction.Ignore);
    }
}
