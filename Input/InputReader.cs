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

    public event Action<Vector2> MoveChanged;

    public Vector2 Move => moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;

    private InputAction moveAction;
    private InputAction lookAction;
    private int users;

    // Ref-counted so several listeners can share one reader safely.
    public void Enable()
    {
        if (users++ > 0) return;

        moveAction = actions.FindAction(moveActionPath, throwIfNotFound: true);
        lookAction = actions.FindAction(lookActionPath, throwIfNotFound: true);

        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        moveAction.Enable();
        lookAction.Enable();
    }

    public void Disable()
    {
        if (users == 0 || --users > 0) return;

        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        moveAction.Disable();
        lookAction.Disable();
        moveAction = null;
        lookAction = null;
    }

    private void OnMove(InputAction.CallbackContext ctx) => MoveChanged?.Invoke(ctx.ReadValue<Vector2>());
}
