using UnityEngine;

// TEMPORARY, for testing: logs every health change and death to the Console, because Health's
// current value is not shown in the Inspector. Delete this once a health bar exists.
[RequireComponent(typeof(Health))]
public class HealthDebugLog : MonoBehaviour
{
    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Changed += OnChanged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Changed -= OnChanged;
        health.Died -= OnDied;
    }

    private void OnChanged(float current, float max)
    {
        Debug.Log($"{name} health: {current}/{max}", this);
    }

    private void OnDied()
    {
        Debug.Log($"{name} died", this);
    }
}
