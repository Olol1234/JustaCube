using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Single entry point for player input. Gameplay scripts reference this asset
// instead of touching the Input System directly.
[CreateAssetMenu(fileName = "InputReader", menuName = "TheOneCube/Input Reader")]
public class InputReader : ScriptableObject
{
    [SerializeField] private InputActionAsset actions;
    [SerializeField] private string moveActionPath = "Player/Move";
    [SerializeField] private string lookActionPath = "Player/Look";
    [SerializeField] private string jumpActionPath = "Player/Jump";
    [SerializeField] private string sprintActionPath = "Player/Sprint";
    [SerializeField] private string lockOnActionPath = "Player/LockOn";
    [SerializeField] private string dashActionPath = "Player/Dash";

    // One action per skill slot, in slot order. The index is what SkillPerformed reports.
    [SerializeField] private string[] skillActionPaths = { "Player/Attack", "Player/Attack2", "Player/Attack3" };

    public event Action<Vector2> MoveChanged;
    public event Action JumpPerformed;
    public event Action LockOnPerformed;
    public event Action DashPerformed;
    public event Action<int> SkillPerformed;   // slot index (0, 1, 2)

    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    public bool IsSprinting => sprintAction != null && sprintAction.IsPressed();

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction lockOnAction;
    private InputAction dashAction;
    private InputAction[] skillActions;
    private Action<InputAction.CallbackContext>[] skillHandlers;   // kept so Disable can unsubscribe the same ones
    private int users;

    // Ref-counted so several listeners can share one reader safely.
    public void Enable()
    {
        if (users++ > 0) return;

        moveAction = actions.FindAction(moveActionPath, throwIfNotFound: true);
        lookAction = actions.FindAction(lookActionPath, throwIfNotFound: true);
        jumpAction = actions.FindAction(jumpActionPath, throwIfNotFound: true);

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        moveAction.Enable();
        lookAction.Enable();
        jumpAction.performed += OnJump;
        jumpAction.Enable();
        sprintAction = actions.FindAction(sprintActionPath, throwIfNotFound: true);
        sprintAction.Enable();
        lockOnAction = actions.FindAction(lockOnActionPath, throwIfNotFound: true);
        lockOnAction.performed += OnLockOn;
        lockOnAction.Enable();
        dashAction = actions.FindAction(dashActionPath, throwIfNotFound: true);
        dashAction.performed += OnDash;
        dashAction.Enable();

        skillActions = new InputAction[skillActionPaths.Length];
        skillHandlers = new Action<InputAction.CallbackContext>[skillActionPaths.Length];
        for (int i = 0; i < skillActionPaths.Length; i++)
        {
            int slot = i;   // copy, so each handler keeps its own slot number
            skillActions[i] = actions.FindAction(skillActionPaths[i], throwIfNotFound: true);
            skillHandlers[i] = ctx => SkillPerformed?.Invoke(slot);
            skillActions[i].performed += skillHandlers[i];
            skillActions[i].Enable();
        }
    }

    public void Disable()
    {
        if (users == 0 || --users > 0) return;

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        moveAction.Disable();
        lookAction.Disable();
        jumpAction.performed -= OnJump;
        jumpAction.Disable();
        sprintAction.Disable();
        lockOnAction.performed -= OnLockOn;
        lockOnAction.Disable();
        dashAction.performed -= OnDash;
        dashAction.Disable();
        for (int i = 0; i < skillActions.Length; i++)
        {
            skillActions[i].performed -= skillHandlers[i];
            skillActions[i].Disable();
        }
        moveAction = null;
        lookAction = null;
        jumpAction = null;
        sprintAction = null;
        lockOnAction = null;
        dashAction = null;
        skillActions = null;
        skillHandlers = null;
    }

    private void OnMove(InputAction.CallbackContext ctx) => MoveChanged?.Invoke(ctx.ReadValue<Vector2>());
    private void OnJump(InputAction.CallbackContext ctx) => JumpPerformed?.Invoke();
    private void OnLockOn(InputAction.CallbackContext ctx) => LockOnPerformed?.Invoke();
    private void OnDash(InputAction.CallbackContext ctx) => DashPerformed?.Invoke();
}
