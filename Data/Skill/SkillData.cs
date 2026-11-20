using UnityEngine;

// Base for every skill asset. Holds what all skills share and one method, Use, that each kind
// implements in its own way. The caller (PlayerAttacker, a boss script) only knows this type,
// so a new kind of skill is a new subclass and nothing else changes.
// Abstract: there is no "SkillData" asset, only assets of subclasses (put CreateAssetMenu on those).
// Data only: never write to it at runtime, one asset is shared by every use.
public abstract class SkillData : ScriptableObject
{
    [Header("Use")]
    [Tooltip("Seconds before the same skill can be used again. Read by whatever fires the skill (the timer lives there, not here).")]
    [Min(0f)] public float cooldown = 0.5f;

    // Fire the skill. The context says who fired, from where, which way, at whom and who it can hurt.
    // Returns true if the skill actually fired (the caller starts the cooldown),
    // false if it was refused (e.g. the cube is airborne): no cooldown then.
    public abstract bool Use(SkillContext ctx);
}
