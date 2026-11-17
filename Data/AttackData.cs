using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "TheOneCube/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Damage")]
    [Tooltip("Damage dealt to each target it hits.")]
    [Min(0f)] public float damage = 10f;

    [Header("Flight")]
    [Tooltip("Speed (m/s) along its fixed line. No homing.")]
    [Min(0.1f)] public float speed = 20f;

    [Tooltip("Distance (m) it can travel before it disappears.")]
    [Min(0.1f)] public float range = 30f;

    [Tooltip("Edge length (m) of the cube-shaped projectile. Also the size of its hit area.")]
    [Min(0.05f)] public float size = 0.5f;

    [Header("Hit rules")]
    [Tooltip("How many targets it can hit before it stops. 1 = stops at the first hit.")]
    [Min(1)] public int pierceCount = 1;

    [Tooltip("Does a block (anything on the ground mask) stop it? Off = it flies through walls.")]
    public bool stopsAtBlocks = true;

    [Tooltip("Hit each target at most once. Off = a piercing shot could hit the same target again on later frames.")]
    public bool hitOncePerTarget = true;

    [Header("Use")]
    [Tooltip("Seconds before the same skill can be used again. Read by whatever fires the attack, not by the projectile.")]
    [Min(0f)] public float cooldown = 0.5f;
}
