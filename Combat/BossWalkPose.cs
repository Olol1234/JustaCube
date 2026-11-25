using UnityEngine;

// Poses the boss's joints into a walk, based on how far the boss has actually moved.
// It never moves the boss itself (apart from the temporary test walk at the bottom).
public class BossWalkPose : MonoBehaviour
{
    [Header("Joints")]
    [SerializeField] private Transform hips;
    [SerializeField] private Transform hipLeft;
    [SerializeField] private Transform hipRight;
    [SerializeField] private Transform kneeLeft;
    [SerializeField] private Transform kneeRight;
    [SerializeField] private Transform shoulderLeft;
    [SerializeField] private Transform shoulderRight;

    [Header("Cycle")]
    [Tooltip("Distance covered in one full cycle (two steps). Raise it if you raise Leg Swing.")]
    [SerializeField, Min(0.1f)] private float cycleLength = 6.3f;
    [Tooltip("How fast the pose eases in when walking starts and out when it stops.")]
    [SerializeField, Min(0.1f)] private float blendSpeed = 4f;
    [Tooltip("Moving slower than this counts as standing still.")]
    [SerializeField, Min(0f)] private float minWalkSpeed = 0.1f;

    [Header("Swing (degrees)")]
    [SerializeField] private float legSwing = 25f;
    [SerializeField] private float kneeBend = 40f;
    [SerializeField] private float armSwing = 20f;
    [Tooltip("How far the arms are lowered from the T-pose.")]
    [SerializeField] private float armDownAngle = 80f;

    [Header("Body")]
    [Tooltip("How far the body drops at full stride.")]
    [SerializeField] private float bobHeight = 0.3f;

    [Header("Debug")]
    [SerializeField] private float phase;

    [Header("Test (temporary)")]
    [SerializeField] private float testSpeed = 2f;
    private bool testWalking;

    private Quaternion hipLeftRest;
    private Quaternion hipRightRest;
    private Quaternion kneeLeftRest;
    private Quaternion kneeRightRest;
    private Quaternion shoulderLeftRest;
    private Quaternion shoulderRightRest;
    private Vector3 hipsRestPosition;

    private Vector3 lastPosition;
    private float walkWeight; // 0 = standing, 1 = full walk

    private void Awake()
    {
        hipLeftRest = hipLeft.localRotation;
        hipRightRest = hipRight.localRotation;
        kneeLeftRest = kneeLeft.localRotation;
        kneeRightRest = kneeRight.localRotation;
        shoulderLeftRest = shoulderLeft.localRotation;
        shoulderRightRest = shoulderRight.localRotation;
        hipsRestPosition = hips.localPosition;
    }

    private void OnEnable()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        MoveTest();
    }

    // LateUpdate, so any movement done in Update (by any script) is already applied.
    private void LateUpdate()
    {
        AdvanceCycle(Time.deltaTime);
        ApplyPose();
    }

    private void AdvanceCycle(float deltaTime)
    {
        Vector3 moved = transform.position - lastPosition;
        lastPosition = transform.position;
        moved.y = 0f; // falling or rising shouldn't move the legs

        if (deltaTime <= 0f) return; // paused

        float distance = moved.magnitude;
        phase = Mathf.Repeat(phase + distance / cycleLength * Mathf.PI * 2f, Mathf.PI * 2f);

        bool isWalking = distance / deltaTime > minWalkSpeed;
        walkWeight = Mathf.MoveTowards(walkWeight, isWalking ? 1f : 0f, blendSpeed * deltaTime);
    }

    private void ApplyPose()
    {
        float sin = Mathf.Sin(phase);
        float cos = Mathf.Cos(phase);

        // Negative X swings a hanging limb forward. The left leg moves forward while cos > 0.
        float leftHip = -sin * legSwing * walkWeight;
        float rightHip = sin * legSwing * walkWeight;

        // A knee only bends while its own leg is moving forward.
        float leftKnee = Mathf.Max(0f, cos) * kneeBend * walkWeight;
        float rightKnee = Mathf.Max(0f, -cos) * kneeBend * walkWeight;

        hipLeft.localRotation = Swing(leftHip) * hipLeftRest;
        hipRight.localRotation = Swing(rightHip) * hipRightRest;
        kneeLeft.localRotation = Swing(leftKnee) * kneeLeftRest;
        kneeRight.localRotation = Swing(rightKnee) * kneeRightRest;

        // Lower the arm first, then swing it opposite to the leg on the same side.
        float arm = sin * armSwing * walkWeight;
        shoulderLeft.localRotation = Swing(arm) * Quaternion.Euler(0f, 0f, armDownAngle) * shoulderLeftRest;
        shoulderRight.localRotation = Swing(-arm) * Quaternion.Euler(0f, 0f, -armDownAngle) * shoulderRightRest;

        // Highest when the legs pass each other, lowest at full stride.
        float drop = (1f - Mathf.Abs(cos)) * bobHeight * walkWeight;
        hips.localPosition = hipsRestPosition + Vector3.down * drop;
    }

    private static Quaternion Swing(float degrees)
    {
        return Quaternion.Euler(degrees, 0f, 0f);
    }

    // --- Temporary test walk: delete once the boss has real movement ---

    [ContextMenu("Test: toggle walk")]
    private void ToggleTestWalk()
    {
        testWalking = !testWalking;
    }

    private void MoveTest()
    {
        if (!testWalking) return;
        transform.position += transform.forward * (testSpeed * Time.deltaTime);
    }
}
