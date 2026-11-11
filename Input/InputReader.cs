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

    public event Action<Vector2> MoveChanged;
    public event Action JumpPerformed;

    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
    public bool IsSprinting => sprintAction != null && sprintAction.IsPressed();

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
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
        jumpAction = actions.FindAction(jumpActionPath, throwIfNotFound: true);
        jumpAction.performed += OnJump;
        jumpAction.Enable();
        sprintAction = actions.FindAction(sprintActionPath, throwIfNotFound: true);
        sprintAction.Enable();
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
        moveAction = null;
        lookAction = null;
        jumpAction = null;
        sprintAction = null;
    }

    private void OnMove(InputAction.CallbackContext ctx) => MoveChanged?.Invoke(ctx.ReadValue<Vector2>());
    private void OnJump(InputAction.CallbackContext ctx) => JumpPerformed?.Invoke();
}
