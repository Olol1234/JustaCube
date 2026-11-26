using UnityEngine;

// The numbers for one humanoid walk (golem, zombie...), read by HumanoidWalkPose.
// One asset per style, so enemies can share a walk or swap to another one.
// Data only: never write to it at runtime, one asset can be shared by several enemies.
[CreateAssetMenu(fileName = "HumanoidWalkStyle", menuName = "TheOneCube/Humanoid Walk Style")]
public class HumanoidWalkStyle : ScriptableObject
{
    [Header("Cycle")]
    [Tooltip("Distance covered in one full cycle (two steps). Raise it if you raise Leg Swing. It depends on the leg length.")]
    [Min(0.1f)] public float cycleLength = 6.3f;
    [Tooltip("How fast the pose eases in when walking starts and out when it stops.")]
    [Min(0.1f)] public float blendSpeed = 4f;
    [Tooltip("Moving slower than this counts as standing still.")]
    [Min(0f)] public float minWalkSpeed = 0.1f;

    [Header("Swing (degrees)")]
    public float legSwing = 25f;
    [Tooltip("Positive bends like a human knee, negative bends the other way.")]
    public float kneeBend = 40f;
    public float armSwing = 20f;
    [Tooltip("How far the arms are lowered from the T-pose. 0 for a body built with its arms already down.")]
    public float armDownAngle = 80f;

    [Header("Body")]
    [Tooltip("How far the body drops at full stride.")]
    public float bobHeight = 0.3f;
}
