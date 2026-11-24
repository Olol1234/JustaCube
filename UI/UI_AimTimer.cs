using UnityEngine;
using UnityEngine.UI;

// A bar that drains over the aim window of CubeAim, so the player can see how long is left.
// Put this on an object that stays active (the Canvas); barRoot is a child that gets shown and
// hidden. It does not position anything: place and style the bar in the Canvas. Only the UI knows
// the aim, the aim does not know the UI.
public class UI_AimTimer : MonoBehaviour
{
    [SerializeField] private CubeAim aim;
    [Tooltip("The bar's Image (Image Type Filled, needs a sprite). Normally a child of barRoot.")]
    [SerializeField] private Image fill;
    [Tooltip("The whole bar (background + fill), shown only during the aim window. Not the object this script is on.")]
    [SerializeField] private GameObject barRoot;

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    // WindowRemaining is above 0 only during the aim window, and counts down on unscaled time.
    private void Update()
    {
        if (aim == null || fill == null || barRoot == null) return;

        float remaining = aim.WindowRemaining;
        bool shouldShow = remaining > 0f;
        if (barRoot.activeSelf != shouldShow) SetVisible(shouldShow);
        if (!shouldShow) return;

        float length = aim.WindowLength;
        fill.fillAmount = length > 0f ? Mathf.Clamp01(remaining / length) : 0f;
    }

    private void SetVisible(bool visible)
    {
        if (barRoot != null) barRoot.SetActive(visible);
    }
}
