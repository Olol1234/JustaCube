using System.Collections.Generic;
using UnityEngine;

// Marks an object as something the player can lock onto.
// It holds no behaviour: the target finder reads the registry below,
// and the camera aims at AimPosition.
public class Lockable : MonoBehaviour
{
    [Tooltip("Where the camera aims (e.g. the chest). Falls back to this object's position if empty.")]
    [SerializeField] private Transform aimPoint;

    // Every enabled Lockable. Read-only for others, so the finder needs no scene search.
    private static readonly List<Lockable> all = new List<Lockable>();
    public static IReadOnlyList<Lockable> All => all;

    public Vector3 AimPosition => aimPoint != null ? aimPoint.position : transform.position;

    private void OnEnable() => all.Add(this);

    // Must mirror OnEnable, or a disabled/destroyed target would stay in the list.
    private void OnDisable() => all.Remove(this);
}
