using UnityEngine;

// The earth spike skill: the cube slams down, then a line of spikes erupts one after another in
// the aimed direction (flattened onto the ground plane).
// Data only: never write to it at runtime, one asset is shared by every use.
// With a CubeSlam on the owner (the player), Use() starts the slam and the spikes are spawned from
// its impact callback. Without one (a tower, the boss) the spikes start right away.
// Either way the EarthSpikeRunner plays the line by itself.
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
    [Tooltip("Prefab of one spike's visual (1x1x1 at scale 1, pivot in the center). Needs no collider, the runner does the hit test. Empty = a plain cube.")]
    public GameObject spikePrefab;

    [Header("Surface")]
    [Tooltip("Layers a spike can rise from (the ground / block layer). A block in the line's path stops it.")]
    public LayerMask groundMask;

    public override bool Use(SkillContext ctx)
    {
        if (ctx.owner == null) return false;

        // The line ignores yaw/pitch of the aim: flatten it onto the ground plane.
        Vector3 flat = Vector3.ProjectOnPlane(ctx.direction, Vector3.up);
        if (flat.sqrMagnitude < 0.0001f)
            flat = Vector3.ProjectOnPlane(ctx.owner.transform.forward, Vector3.up);
        Vector3 direction = flat.normalized;

        // The direction is fixed now. The origin is NOT: it is read at the moment of impact (below),
        // because a cast in the air dives to the ground first, so the cube is somewhere else by then.
        GameObject owner = ctx.owner;
        LayerMask targetMask = ctx.targetMask;

        // The cast motion is optional. An owner without a CubeSlam (a tower, the boss) has no motion
        // to play, so the spikes start right away from its position. The owner's pivot should sit
        // on the ground: the runner looks for ground from just above that point.
        CubeSlam slam = owner.GetComponent<CubeSlam>();
        if (slam == null)
        {
            SpawnSpikes(owner.transform.position, direction, owner, targetMask);
            return true;
        }

        // Play returns false when the slam refuses (already playing, controlled by something else).
        // Passing that on tells the caller not to start the cooldown.
        return slam.Play(motion, () =>
        {
            if (owner == null) return;   // destroyed before the impact (a destroyed object is a fake null)
            SpawnSpikes(owner.transform.position, direction, owner, targetMask);
        });
    }

    // Called by the slam at the moment of impact.
    void SpawnSpikes(Vector3 origin, Vector3 direction, GameObject owner, LayerMask targetMask)
    {
        // The runner plays the whole line by itself and destroys itself when done.
        // Same idea as Projectile.Init: hand it the asset, where, which way, who fired, who it can hurt.
        var runner = new GameObject("EarthSpikeRunner").AddComponent<EarthSpikeRunner>();
        runner.Init(this, origin, direction, owner, targetMask);
    }
}
