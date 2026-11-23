using UnityEngine;

// Shows a crosshair while the player can fire (the aim window of CubeAim) and hides it otherwise,
// so there is still no permanent crosshair. Put this on an object that stays active (the Canvas);
// the reticle is a child that gets shown and hidden.
// It does not position anything: the reticle sits where the shot is aimed, the centre of the
// screen, so anchor it to the middle of the Canvas with position 0. Only the UI knows the aim,
// the aim does not know the UI.
public class UI_AimReticle : MonoBehaviour
{
    [SerializeField] private CubeAim aim;
    [Tooltip("The crosshair UI object (e.g. an Image), a child anchored to the centre of the Canvas. Not the object this script is on.")]
    [SerializeField] private GameObject reticle;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    // WindowRemaining is above 0 only during the aim window, which is exactly when Fire works.
    private void Update()
    {
        if (aim == null || reticle == null) return;

        bool shouldShow = aim.WindowRemaining > 0f;
        if (reticle.activeSelf != shouldShow) SetVisible(shouldShow);
    }

    private void SetVisible(bool visible)
    {
        if (reticle != null) reticle.SetActive(visible);
    }
}
