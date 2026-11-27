using System;
using UnityEngine;

// One joint of a rig: the pivot that rotates, plus the pose it started in.
// Not called "Joint", because UnityEngine already has a physics class with that name.
[Serializable]
public class RigJoint
{
    [SerializeField] private Transform pivot;

    public Transform Pivot => pivot;
    public Quaternion RestRotation { get; private set; } = Quaternion.identity;
    public Vector3 RestPosition { get; private set; }

    private bool isCaptured;

    // Saves the current local pose as the rest pose. Returns false if no pivot is assigned.
    public bool Capture()
    {
        if (pivot == null) return false;

        RestRotation = pivot.localRotation;
        RestPosition = pivot.localPosition;
        isCaptured = true;
        return true;
    }

    public void ResetToRest()
    {
        if (!isCaptured || pivot == null) return;

        pivot.localRotation = RestRotation;
        pivot.localPosition = RestPosition;
    }

    // Rotates the joint away from its rest pose. The offset turns around the parent's axes.
    public void Pose(Quaternion offset)
    {
        if (!isCaptured || pivot == null) return;

        pivot.localRotation = offset * RestRotation;
    }

    // Moves the joint away from its rest position, in the parent's space.
    public void Shift(Vector3 offset)
    {
        if (!isCaptured || pivot == null) return;

        pivot.localPosition = RestPosition + offset;
    }
}

// The one place that knows a humanoid's skeleton. It saves the rest pose, and every frame puts
// all joints back to it in Update, so whoever poses them in LateUpdate starts from a clean pose.
// It does not animate or move anything else.
// Other scripts should not read rest values in their Awake: the rig may not have captured yet.
public class HumanoidRig : MonoBehaviour
{
    [Header("Body")]
    [SerializeField] private RigJoint hips;
    [SerializeField] private RigJoint waist;
    [SerializeField] private RigJoint neck;

    [Header("Legs")]
    [SerializeField] private RigJoint hipLeft;
    [SerializeField] private RigJoint hipRight;
    [SerializeField] private RigJoint kneeLeft;
    [SerializeField] private RigJoint kneeRight;

    [Header("Feet")]
    [Tooltip("An empty at the centre of the left sole, under the left knee, with zero rotation. Never posed.")]
    [SerializeField] private Transform footLeft;
    [Tooltip("An empty at the centre of the right sole, under the right knee, with zero rotation. Never posed.")]
    [SerializeField] private Transform footRight;
    [Tooltip("Radius of a round sole, so a tilted foot is measured by its edge. 0 = a single point.")]
    [Min(0f)] [SerializeField] private float soleRadius = 0f;

    [Header("Arms")]
    [SerializeField] private RigJoint shoulderLeft;
    [SerializeField] private RigJoint shoulderRight;
    [SerializeField] private RigJoint elbowLeft;
    [SerializeField] private RigJoint elbowRight;

    public RigJoint Hips => hips;
    public RigJoint Waist => waist;
    public RigJoint Neck => neck;
    public RigJoint HipLeft => hipLeft;
    public RigJoint HipRight => hipRight;
    public RigJoint KneeLeft => kneeLeft;
    public RigJoint KneeRight => kneeRight;
    public RigJoint ShoulderLeft => shoulderLeft;
    public RigJoint ShoulderRight => shoulderRight;
    public RigJoint ElbowLeft => elbowLeft;
    public RigJoint ElbowRight => elbowRight;

    public bool HasFeet => footLeft != null && footRight != null;

    // The lowest sole's height in the rest pose, measured from this object.
    public float RestSoleHeight { get; private set; }

    private RigJoint[] joints;

    // The lowest point of either sole in the current pose, measured from this object.
    // Only meaningful when HasFeet is true.
    public float LowestSoleHeight() => Mathf.Min(SoleHeight(footLeft), SoleHeight(footRight));

    private float SoleHeight(Transform foot)
    {
        float centre = transform.InverseTransformPoint(foot.position).y;

        // A tilted round sole reaches lower than its centre.
        float rightY = transform.InverseTransformDirection(foot.right).y;
        float forwardY = transform.InverseTransformDirection(foot.forward).y;

        return centre - soleRadius * Mathf.Sqrt(rightY * rightY + forwardY * forwardY);
    }

    private void Awake()
    {
        joints = new[]
        {
            hips, waist, neck, hipLeft, hipRight, kneeLeft, kneeRight,
            shoulderLeft, shoulderRight, elbowLeft, elbowRight
        };

        Capture(hips, nameof(hips));
        Capture(waist, nameof(waist));
        Capture(neck, nameof(neck));
        Capture(hipLeft, nameof(hipLeft));
        Capture(hipRight, nameof(hipRight));
        Capture(kneeLeft, nameof(kneeLeft));
        Capture(kneeRight, nameof(kneeRight));
        Capture(shoulderLeft, nameof(shoulderLeft));
        Capture(shoulderRight, nameof(shoulderRight));
        Capture(elbowLeft, nameof(elbowLeft));
        Capture(elbowRight, nameof(elbowRight));

        // Measured here, because nothing has posed the body yet.
        if (HasFeet) RestSoleHeight = LowestSoleHeight();
    }

    // Update runs before every LateUpdate, so the reset always happens before any posing.
    private void Update()
    {
        foreach (RigJoint joint in joints) joint.ResetToRest();
    }

    private void Capture(RigJoint joint, string jointName)
    {
        if (!joint.Capture()) Debug.LogWarning($"{name}: rig joint '{jointName}' has no pivot assigned.", this);
    }
}
