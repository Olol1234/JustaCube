using UnityEngine;
using UnityEngine.UI;

// Shows one Health as a filled bar. Knows nothing about where it sits (above a mob's head, in the
// corner for the player, at the bottom of the screen for a boss) or when it is visible: other
// scripts decide that. Gameplay never knows this exists, it only listens to Health's events.
public class UI_HealthBar : MonoBehaviour
{
    [Tooltip("Whose health to show. Leave empty to set it from code with Bind (e.g. a spawned mob).")]
    [SerializeField] private Health health;

    [Tooltip("An Image set to Image Type = Filled (Horizontal). It needs a sprite, even a plain white one, or it cannot fill.")]
    [SerializeField] private Image fill;

    [Tooltip("Optional. Shown while alive and hidden on death. Use the bar's background object, not the object this script is on, or it would switch itself off.")]
    [SerializeField] private GameObject barRoot;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // Points the bar at a different Health (a mob that just spawned, the boss when the fight starts).
    public void Bind(Health newHealth)
    {
        Unsubscribe();
        health = newHealth;

        // Before OnEnable the bar is not listening yet, and OnEnable will do both steps itself.
        if (isActiveAndEnabled)
        {
            Subscribe();
            Refresh();
        }
    }

    private void Subscribe()
    {
        if (health == null) return;
        health.Changed += OnChanged;
        health.Died += OnDied;
    }

    private void Unsubscribe()
    {
        if (health == null) return;
        health.Changed -= OnChanged;
        health.Died -= OnDied;
    }

    private void OnChanged(float current, float max)
    {
        Refresh();
    }

    private void OnDied()
    {
        Refresh();
    }

    // Reads the numbers straight from Health instead of waiting for an event: Health raises its
    // first Changed in its own OnEnable, which may run before this bar is listening.
    private void Refresh()
    {
        if (health == null)
        {
            if (barRoot != null) barRoot.SetActive(false);
            return;
        }

        if (fill != null) fill.fillAmount = health.Current / health.Max;
        if (barRoot != null) barRoot.SetActive(!health.IsDead);
    }
}
