using System.Collections.Generic;
using UnityEngine;

// Plays one earth spike cast: a line of spikes that erupt one after another, hold, then sink.
// Spawned by EarthSpikeData at the moment of impact (not placed in the scene). One runner handles
// the whole line, so there is one Update for all spikes. The numbers come from the asset.
// Each spike finds the surface under it with a raycast, so the line follows steps and ledges.
public class EarthSpikeRunner : MonoBehaviour
{
    private class Spike
    {
        public float startTime;     // seconds after the cast when this spike starts rising
        public float groundY;       // surface height under this spike
        public Vector3 xz;          // horizontal position (y unused)
        public Transform visual;    // created when the spike starts, destroyed when it is done
        public bool finished;
    }

    // Shared by every runner: avoids allocating a new array for each hit test.
    private static readonly Collider[] overlapBuffer = new Collider[16];

    // How far above the line's origin the ground ray starts, and how far down it looks.
    private const float rayStartHeight = 2f;
    private const float rayLength = 6f;

    private EarthSpikeData data;
    private GameObject source;
    private LayerMask targetMask;
    private Quaternion rotation;
    private readonly List<Spike> spikes = new List<Spike>();
    private readonly HashSet<Health> alreadyHit = new HashSet<Health>();
    private float elapsed;
    private bool initialized;

    // Call right after creating the runner. `origin` = where the cast started (the cube),
    // `dir` = flat direction of the line, `targets` = layers this cast can hurt.
    public void Init(EarthSpikeData spikeData, Vector3 origin, Vector3 dir, GameObject owner, LayerMask targets)
    {
        data = spikeData;
        source = owner;
        targetMask = targets;
        rotation = Quaternion.LookRotation(dir);

        for (int i = 0; i < data.segmentCount; i++)
        {
            Vector3 flatPos = origin + dir * (data.startOffset + i * data.segmentSpacing);
            Vector3 rayStart = flatPos + Vector3.up * rayStartHeight;

            // Starting inside a block means a wall is in the way: the line stops here.
            if (Physics.CheckSphere(rayStart, 0.1f, data.groundMask, QueryTriggerInteraction.Ignore)) break;

            // No ground under this spike (a pit): skip it, the line carries on past the gap.
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, rayLength,
                    data.groundMask, QueryTriggerInteraction.Ignore))
                continue;

            spikes.Add(new Spike
            {
                startTime = i * data.staggerDelay,
                groundY = hit.point.y,
                xz = flatPos,
            });
        }

        elapsed = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;

        elapsed += Time.deltaTime;
        bool allFinished = true;

        for (int i = 0; i < spikes.Count; i++)
        {
            Spike spike = spikes[i];
            if (spike.finished) continue;

            float t = elapsed - spike.startTime;
            if (t < 0f)
            {
                allFinished = false;
                continue;
            }

            float riseEnd = data.riseTime;
            float holdEnd = riseEnd + data.holdTime;
            float lowerEnd = holdEnd + data.lowerTime;

            if (t >= lowerEnd)
            {
                if (spike.visual != null) Destroy(spike.visual.gameObject);
                spike.finished = true;
                continue;
            }

            allFinished = false;
            if (spike.visual == null) spike.visual = CreateVisual();

            // 0 = buried (top level with the ground), 1 = fully up (bottom level with the ground).
            float rise;
            if (t < riseEnd) rise = data.riseCurve.Evaluate(t / data.riseTime);
            else if (t < holdEnd) rise = data.riseCurve.Evaluate(1f);
            else rise = data.riseCurve.Evaluate(1f - (t - holdEnd) / data.lowerTime);

            float centerY = spike.groundY + data.spikeSize.y * (rise - 0.5f);
            Vector3 center = new Vector3(spike.xz.x, centerY, spike.xz.z);
            spike.visual.SetPositionAndRotation(center, rotation);

            // Dangerous while rising and holding, harmless while sinking.
            if (t < holdEnd) HitTargets(center);
        }

        if (allFinished) Destroy(gameObject);
    }

    private Transform CreateVisual()
    {
        GameObject go;
        if (data.spikePrefab != null)
        {
            go = Instantiate(data.spikePrefab);
        }
        else
        {
            // No prefab assigned: a plain cube so the skill can be tested without wiring anything.
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
        }
        go.transform.localScale = data.spikeSize;
        return go.transform;
    }

    private void HitTargets(Vector3 center)
    {
        int count = Physics.OverlapBoxNonAlloc(
            center,
            data.spikeSize * 0.5f,
            overlapBuffer,
            rotation,
            targetMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Health health = overlapBuffer[i].GetComponentInParent<Health>();
            if (health == null || health.gameObject == source) continue;

            // Each target is hurt at most once per cast, by the whole line.
            if (!alreadyHit.Add(health)) continue;

            health.TakeDamage(new DamageInfo(data.damage, source));
        }
    }
}
