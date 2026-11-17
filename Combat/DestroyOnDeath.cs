using UnityEngine;

// Decides what dying means for this object: destroy it. Health only announces Died, so this
// listener can be swapped (disable, play an effect, drop loot, game over) without touching Health.
[RequireComponent(typeof(Health))]
public class DestroyOnDeath : MonoBehaviour
{
    [Tooltip("Seconds to wait before destroying, e.g. to let a death effect play. 0 = immediately.")]
    [SerializeField, Min(0f)] private float delay = 0f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Died -= OnDied;
    }

    private void OnDied()
    {
        Destroy(gameObject, delay);
    }
}
