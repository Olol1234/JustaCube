using UnityEngine;

// The earth spike skill: the cube slams down, then a line of spikes erupts one after another in
// the aimed direction (flattened onto the ground plane).
// Data only: never write to it at runtime, one asset is shared by every use.
// Use() only starts the slam. The spikes are spawned from the slam's impact callback and run
// themselves (the runner script, still to be written).
[CreateAssetMenu(menuName = "TheOneCube/Earth Spike Data", fileName = "EarthSpikeData")]
public class EarthSpikeData : SkillData
{
    [Header("Cast")]
    [Tooltip("The cube's own motion while casting (rise, hang, slam, recovery).")]
    public SlamMotion motion = new SlamMotion();

    [Header("Line")]
    [Tooltip("How many spikes in the line.")]
    [Min(1)] public int segmentCount = 5;
    [Tooltip("Distance between two spikes (world units). Around the cube's width makes a solid wall.")]
    [Min(0.1f)] public float segmentSpacing = 1f;
    [Tooltip("Where the first spike appears, in front of the cube's feet (world units).")]
    [Min(0f)] public float startOffset = 1f;

    [Header("Timing")]
    [Tooltip("Seconds between one spike starting and the next. 0 = all at once.")]
    [Min(0f)] public float staggerDelay = 0.1f;
    [Tooltip("Seconds for a spike to rise from below the ground to full height.")]
    [Min(0.01f)] public float riseTime = 0.2f;
    [Tooltip("Shape of the rise (0 = hidden, 1 = full height).")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Seconds a spike stays up (this is when it can hurt).")]
    [Min(0f)] public float holdTime = 0.4f;
    [Tooltip("Seconds for a spike to sink back down.")]
    [Min(0.01f)] public float lowerTime = 0.3f;

    [Header("Spike")]
    [Tooltip("Size of one spike at full height (width, height, depth).")]
    public Vector3 spikeSize = new Vector3(1f, 1.5f, 1f);
    [Min(0f)] public float damage = 20f;
    [Tooltip("Prefab of one spike's visual. Needs no collider, the runner does the hit test.")]
    public GameObject spikePrefab;

    public override void Use(SkillContext ctx)
    {
        if (ctx.owner == null) return;

        CubeSlam slam = ctx.owner.GetComponent<CubeSlam>();
        if (slam == null)
        {
            Debug.LogWarning($"{name}: the owner has no CubeSlam, the skill cannot be cast.", this);
            return;
        }

        // The line ignores yaw/pitch of the aim: flatten it onto the ground plane.
        Vector3 flat = Vector3.ProjectOnPlane(ctx.direction, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.ProjectOnPlane(ctx.owner.transform.forward, Vector3.up);
        Vector3 direction = flat.normalized;

        // Captured now: by the time the slam lands, the cube may have been moved by the roller.
        Vector3 origin = ctx.origin;
        GameObject owner = ctx.owner;
        LayerMask targetMask = ctx.targetMask;

        // False when the slam refuses (already playing, airborne, controlled). Nothing happens then.
        // TODO: tell the caller, so the cooldown does not start on a cast that never happened.
        slam.Play(motion, () => SpawnSpikes(origin, direction, owner, targetMask));
    }

    // Called by the slam at the moment of impact.
    void SpawnSpikes(Vector3 origin, Vector3 direction, GameObject owner, LayerMask targetMask)
    {
        // TODO: spawn the spike runner here and hand it this asset, the direction, the owner and the mask
        // (same idea as Projectile.Init).
        Debug.Log($"{name}: impact, {segmentCount} spikes toward {direction}.", this);
    }
}
