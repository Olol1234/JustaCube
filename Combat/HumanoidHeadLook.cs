using UnityEngine;

// Turns the head (the rig's neck joint) toward a target or a direction, or holds it where it is.
// It only poses the neck. Whoever wants the head to look somewhere calls LookAt, Face, Hold or
// Release; the fields are also read every frame, so they can be tried in the Inspector.
// Assumes the neck's rest rotation is zero, so a zero angle looks straight ahead.
// Runs late, so anything that poses the waist this frame has already done it.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(HumanoidRig))]
public class HumanoidHeadLook : MonoBehaviour
{
    public enum LookMode
    {
        Release,    // ease back to the rest pose
        Target,     // follow a Transform
        Direction,  // face a world direction
        Hold        // keep the current angle, relative to the body
    }

    [Header("What to look at")]
    [SerializeField] private LookMode mode = LookMode.Release;
    [Tooltip("Used by Target mode.")]
    [SerializeField] private Transform target;
    [Tooltip("Used by Direction mode. In world space.")]
    [SerializeField] private Vector3 faceDirection = Vector3.forward;

    [Header("Limits (degrees)")]
    [Tooltip("How far the head turns left or right.")]
    [Range(0f, 180f)] [SerializeField] private float maxYaw = 70f;
    [Tooltip("How far the head looks up or down.")]
    [Range(0f, 90f)] [SerializeField] private float maxPitch = 30f;
    [Tooltip("Further to the side than this counts as behind: the head eases back to the front instead of flipping sides.")]
    [Range(0f, 180f)] [SerializeField] private float giveUpYaw = 130f;

    [Header("Speed")]
    [Tooltip("Degrees per second.")]
    [Min(0f)] [SerializeField] private float turnSpeed = 180f;

    private HumanoidRig rig;
    private float yaw;      // kept here, because the rig resets the neck every frame
    private float pitch;

    public LookMode Mode => mode;

    private void Awake()
    {
        rig = GetComponent<HumanoidRig>();
    }

    private void OnDisable()
    {
        yaw = 0f;
        pitch = 0f;
    }

    public void LookAt(Transform newTarget)
    {
        target = newTarget;
        mode = LookMode.Target;
    }

    public void Face(Vector3 worldDirection)
    {
        faceDirection = worldDirection;
        mode = LookMode.Direction;
    }

    public void Hold() => mode = LookMode.Hold;

    public void Release() => mode = LookMode.Release;

    private void LateUpdate()
    {
        Transform neck = rig.Neck.Pivot;
        if (neck == null) return;

        if (mode != LookMode.Hold)
        {
            Vector2 wanted = WantedAngles(neck);
            float step = turnSpeed * Time.deltaTime;
            yaw = Mathf.MoveTowards(yaw, wanted.x, step);
            pitch = Mathf.MoveTowards(pitch, wanted.y, step);
        }

        // Fully released: leave the neck alone, so another motion can use it.
        if (mode == LookMode.Release && yaw == 0f && pitch == 0f) return;

        rig.Neck.Pose(Quaternion.Euler(pitch, yaw, 0f));
    }

    // x = yaw, y = pitch. Zero means straight ahead.
    private Vector2 WantedAngles(Transform neck)
    {
        Vector3 worldDirection;
        if (mode == LookMode.Target && target != null) worldDirection = target.position - neck.position;
        else if (mode == LookMode.Direction) worldDirection = faceDirection;
        else return Vector2.zero;

        if (worldDirection.sqrMagnitude < 0.0001f) return Vector2.zero;

        // The angles are measured in the parent's space, which is also where the pose is applied.
        Vector3 local = neck.parent.InverseTransformDirection(worldDirection);
        float horizontal = new Vector2(local.x, local.z).magnitude;

        float wantedYaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        if (Mathf.Abs(wantedYaw) > giveUpYaw) return Vector2.zero;

        // A positive X rotation looks down, so a point above gives a negative pitch.
        float wantedPitch = -Mathf.Atan2(local.y, horizontal) * Mathf.Rad2Deg;

        return new Vector2(
            Mathf.Clamp(wantedYaw, -maxYaw, maxYaw),
            Mathf.Clamp(wantedPitch, -maxPitch, maxPitch));
    }
}
