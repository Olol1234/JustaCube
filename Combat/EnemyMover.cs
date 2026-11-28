using UnityEngine;

// Moves and turns an enemy's root. A brain tells it where to go and what to face;
// it knows nothing about the player, attacks or animation.
// Movement and facing are separate requests, and each stays active until it is replaced or stopped.
// Movement is flat and in a straight line (no pathfinding). The CharacterController handles
// walls, small steps and the ground.
// It moves in Update, so scripts that pose the body in LateUpdate see the new position.
[RequireComponent(typeof(CharacterController))]
public class EnemyMover : MonoBehaviour
{
    [Header("Movement")]
    [Min(0f)] [SerializeField] private float moveSpeed = 2f;
    [Min(0.01f)] [SerializeField] private float acceleration = 8f;
    [Min(0.01f)] [SerializeField] private float deceleration = 10f;
    [Tooltip("How close to the move point (measured along the ground) counts as arrived.")]
    [Min(0.01f)] [SerializeField] private float arrivalDistance = 0.2f;

    [Header("Turning")]
    [Tooltip("Degrees per second.")]
    [Min(0f)] [SerializeField] private float turnSpeed = 120f;

    [Header("Gravity")]
    [Min(0f)] [SerializeField] private float gravity = 20f;
    [Tooltip("Small downward speed while grounded, so the controller stays on the floor.")]
    [Min(0f)] [SerializeField] private float groundStickSpeed = 2f;

    [Header("Test (temporary)")]
    [Tooltip("If assigned, walk to and face this object.")]
    [SerializeField] private Transform testTarget;
    [Min(0f)] [SerializeField] private float testStopDistance = 4f;

    private CharacterController controller;

    private Vector3 movePoint;
    private Vector3 facePoint;
    private bool hasMovePoint;
    private bool hasFacePoint;

    private Vector3 horizontalVelocity;
    private float verticalSpeed;

    // --- Requests ---

    public void MoveTo(Vector3 point)
    {
        movePoint = point;
        hasMovePoint = true;
    }

    public void StopMoving()
    {
        hasMovePoint = false;
    }

    public void Face(Vector3 point)
    {
        facePoint = point;
        hasFacePoint = true;
    }

    public void StopFacing()
    {
        hasFacePoint = false;
    }

    // --- Unity ---

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void OnDisable()
    {
        StopMoving();
        StopFacing();
        horizontalVelocity = Vector3.zero;
        verticalSpeed = 0f;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f || !controller.enabled) return; // paused, or nothing to move with

        RunTest();

        UpdateFacing(deltaTime);
        UpdateHorizontalVelocity(deltaTime);
        UpdateVerticalSpeed(deltaTime);

        // One Move per frame, so isGrounded describes the whole movement.
        Vector3 velocity = horizontalVelocity + Vector3.up * verticalSpeed;
        controller.Move(velocity * deltaTime);
    }

    // --- Movement ---

    private void UpdateFacing(float deltaTime)
    {
        if (!hasFacePoint) return;

        Vector3 direction = FlatOffsetTo(facePoint);
        if (direction.sqrMagnitude < 0.0001f) return; // the point is right above or below

        Quaternion wanted = Quaternion.LookRotation(direction, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * deltaTime);
    }

    private void UpdateHorizontalVelocity(float deltaTime)
    {
        Vector3 desired = DesiredVelocity();
        bool speedingUp = desired.sqrMagnitude > horizontalVelocity.sqrMagnitude;
        float rate = speedingUp ? acceleration : deceleration;

        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desired, rate * deltaTime);
    }

    private Vector3 DesiredVelocity()
    {
        if (!hasMovePoint) return Vector3.zero;

        Vector3 offset = FlatOffsetTo(movePoint);
        float distance = offset.magnitude;
        if (distance <= arrivalDistance) return Vector3.zero;

        // The fastest speed that can still brake in the distance left. Without this, a low
        // deceleration would carry it past the point and make it walk back and forth.
        float brakingSpeed = Mathf.Sqrt(2f * deceleration * (distance - arrivalDistance * 0.5f));
        float speed = Mathf.Min(moveSpeed, brakingSpeed);

        return offset / distance * speed;
    }

    private void UpdateVerticalSpeed(float deltaTime)
    {
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -groundStickSpeed;

        verticalSpeed -= gravity * deltaTime;
    }

    // From this object to the point, ignoring height.
    private Vector3 FlatOffsetTo(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset;
    }

    // --- Temporary test: delete once the brain drives the mover ---

    // Uses only the public requests, the same way the brain will.
    private void RunTest()
    {
        if (testTarget == null) return;

        Face(testTarget.position);

        if (FlatOffsetTo(testTarget.position).magnitude > testStopDistance)
            MoveTo(testTarget.position);
        else
            StopMoving();
    }
}
