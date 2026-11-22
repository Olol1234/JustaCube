using UnityEngine;

// Keeps a health bar's width proportional to the max health, so a bigger max (an item, a skin, a
// heart container) makes a longer bar, like Elden Ring. Only the player's bar needs this. It does not
// touch the fill: UI_HealthBar does that. Put it next to UI_HealthBar and point both at the same Health.
// The bar's pivot X should be 0 (anchored left) so it grows to the right, and its Background and Fill
// should stretch to the parent so they follow the width.
public class UI_HealthBarWidth : MonoBehaviour
{
    [Tooltip("Whose max health sets the width.")]
    [SerializeField] private Health health;

    [Tooltip("The bar's RectTransform (the object that holds the background and the fill). Leave empty to use this object.")]
    [SerializeField] private RectTransform target;

    [Tooltip("Width in UI pixels for each point of max health. 100 HP at 3 gives 300 pixels.")]
    [SerializeField, Min(0.01f)] private float pixelsPerHP = 3f;

    [Tooltip("The bar never grows wider than this, so many upgrades cannot push it off the screen.")]
    [SerializeField, Min(1f)] private float maxWidth = 600f;

    private void Awake()
    {
        if (target == null) target = transform as RectTransform;
    }

    private void OnEnable()
    {
        if (health == null) return;
        health.Changed += OnChanged;

        // Health's first Changed can fire before we listen, so read the max directly.
        Apply(health.Max);
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Changed -= OnChanged;
    }

    private void OnChanged(float current, float max)
    {
        Apply(max);
    }

    private void Apply(float max)
    {
        if (target == null) return;

        float width = Mathf.Min(max * pixelsPerHP, maxWidth);
        target.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }
}
