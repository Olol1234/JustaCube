using System;
using UnityEngine;

// Decides WHEN a static enemy (a tower) attacks. It does not know how an attack works (the SkillData
// does) and it draws nothing: a later listener can react to the events (a charging glow on
// TelegraphStarted, a sound on Fired).
//
// Idle -> Telegraph -> (fire) -> Recover -> Idle
//  - Idle: checks the distance to the target a few times a second (not every frame, nobody can
//    tell a 0.2 s delay) and starts the telegraph when the target is inside detectionRange.
//  - Telegraph: a short warning. The aim direction is locked when it STARTS, so a player who keeps
//    moving can dodge the shot. That is the whole point of a telegraph.
//  - Recover: a pause before it can look again.
// Enemies hurt the player the same way the player hurts enemies: through a SkillContext whose
// targetMask is the Player layer.
public class EnemyBrain : MonoBehaviour
{
    public enum State { Idle, Telegraph, Recover }

    [Header("Attack")]
    [Tooltip("What to fire. Any skill asset works (the projectile AttackData to start with).")]
    [SerializeField] private SkillData skill;

    [Tooltip("Who it can hurt. Set to the Player layer.")]
    [SerializeField] private LayerMask targetMask;

    [Tooltip("Optional. Where shots start and which way 'forward' is. Empty = this object's position.")]
    [SerializeField] private Transform muzzle;

    [Header("Target")]
    [Tooltip("The player. A scene reference for now. Its Health (on it or a parent) is used to stop when the player dies.")]
    [SerializeField] private Transform target;

    [Header("Timing")]
    [Tooltip("The enemy notices the target inside this distance.")]
    [SerializeField, Min(0f)] private float detectionRange = 20f;

    [Tooltip("Seconds between distance checks while idle.")]
    [SerializeField, Min(0.05f)] private float idleCheckInterval = 0.2f;

    [Tooltip("The warning before a shot. The aim is locked when it starts.")]
    [SerializeField, Min(0f)] private float telegraphTime = 1f;

    [Tooltip("Pause after a shot before it can look for the target again. The skill's own cooldown is not used by enemies.")]
    [SerializeField, Min(0f)] private float recoverTime = 1.5f;

    public State Current { get; private set; } = State.Idle;

    public event Action<State> StateChanged; // new state
    public event Action TelegraphStarted;
    public event Action Fired;

    private Health ownHealth;
    private Health targetHealth;
    private float stateTimer;
    private float idleTimer;
    private Vector3 lockedDirection;

    private void Awake()
    {
        ownHealth = GetComponentInParent<Health>();
        if (target != null) targetHealth = target.GetComponentInParent<Health>();
    }

    private void OnEnable()
    {
        if (ownHealth != null) ownHealth.Died += OnOwnerDied;

        Current = State.Idle;
        // A random start so many towers do not all check in the same frame.
        idleTimer = UnityEngine.Random.Range(0f, idleCheckInterval);
    }

    private void OnDisable()
    {
        if (ownHealth != null) ownHealth.Died -= OnOwnerDied;
    }

    private void OnOwnerDied()
    {
        // A dead enemy stops shooting. What dying looks like is up to another listener.
        enabled = false;
    }

    private void Update()
    {
        if (target == null) return;

        switch (Current)
        {
            case State.Idle:
                UpdateIdle();
                break;
            case State.Telegraph:
                UpdateTelegraph();
                break;
            case State.Recover:
                UpdateRecover();
                break;
        }
    }

    private void UpdateIdle()
    {
        idleTimer -= Time.deltaTime;
        if (idleTimer > 0f) return;
        idleTimer = idleCheckInterval;

        if (!TargetAlive()) return;

        // Squared distance: no square root needed, and nothing from the physics system.
        float sqrDistance = (target.position - transform.position).sqrMagnitude;
        if (sqrDistance > detectionRange * detectionRange) return;

        lockedDirection = target.position - Origin;
        ChangeState(State.Telegraph);
        TelegraphStarted?.Invoke();
    }

    private void UpdateTelegraph()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        // The target died during the warning: no point firing.
        if (!TargetAlive())
        {
            ChangeState(State.Idle);
            return;
        }

        Fire();
        ChangeState(State.Recover);
    }

    private void UpdateRecover()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer > 0f) return;

        ChangeState(State.Idle);
    }

    private void Fire()
    {
        if (skill == null) return;

        // No lock-on target: that is a player feature (Lockable). The enemy aims at a position.
        var ctx = new SkillContext(gameObject, Origin, lockedDirection, null, targetMask);
        if (skill.Use(ctx)) Fired?.Invoke();
    }

    private void ChangeState(State next)
    {
        Current = next;
        stateTimer = next == State.Telegraph ? telegraphTime
                   : next == State.Recover ? recoverTime
                   : 0f;
        StateChanged?.Invoke(next);
    }

    private bool TargetAlive()
    {
        return targetHealth == null || !targetHealth.IsDead;
    }

    private Vector3 Origin => muzzle != null ? muzzle.position : transform.position;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
