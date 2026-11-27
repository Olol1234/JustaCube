using UnityEngine;

// Keeps the lowest foot on the floor by moving the hips up or down after the body is posed.
// It does not detect the ground. "The floor" is where the lowest sole is in the rest pose,
// measured from this object, so whatever moves the object has to keep it on the real ground.
// It only fixes the lowest foot, and does not stop feet sliding.
// Runs last, so every script that poses the legs or hips has to run before it.
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(HumanoidRig))]
public class HumanoidFootGrounding : MonoBehaviour
{
    [Tooltip("1 = the lowest foot stays on the floor, 0 = off (for example while airborne).")]
    [Range(0f, 1f)] [SerializeField] private float weight = 1f;

    private HumanoidRig rig;

    public float Weight
    {
        get => weight;
        set => weight = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        rig = GetComponent<HumanoidRig>();
    }

    private void Start()
    {
        if (!rig.HasFeet) Debug.LogWarning($"{name}: the rig has no foot points assigned.", this);
    }

    private void LateUpdate()
    {
        Transform hips = rig.Hips.Pivot;
        if (!rig.HasFeet || hips == null || weight <= 0f) return;

        float correction = (rig.RestSoleHeight - rig.LowestSoleHeight()) * weight;

        // Added to where the hips already are, so other hip movement is kept.
        // The rig resets the hips every Update, so this never piles up.
        hips.position += transform.TransformVector(Vector3.up * correction);
    }
}
