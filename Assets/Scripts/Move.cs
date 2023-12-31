using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "GMTKGameJam2023/Move", order = 0)]
public class Move : ScriptableObject {
    public string displayName;

    /// <summary>
    /// Required mana to use
    /// (UNUSED FOR NOW in favor of uses remaining system)
    /// </summary>
    [HideInInspector] public int manaCost;

    public int damage;

    /// <summary>
    /// Damage taken is reduced by this value
    /// </summary>
    public int defense;

    /// <summary>
    /// Status effect to inflict on self, if non-null and duration above 0
    /// </summary>
    public StatusEffect selfEffect;

    /// <summary>
    /// Status effect to inflict to the opponent, if non-null and duration above 0
    /// </summary>
    public StatusEffect opponentEffect;
    
    /// <summary>
    /// Amount of times a battler can use this per battle.
    /// Uses remaining is stored on the battler.
    /// Count is reset to this base value at the start of a battle.
    /// </summary>
    public int baseUses;

    /// <summary>
    /// Name of the anim state to set to in the animator controller of the battler. ("Cast", etc.)
    /// </summary>
    public string useAnimState = "Cast";
    public AudioClip useSFX;

    /// <summary>
    /// Name of the anim state of the target of this move when hit by it
    /// </summary>
    public string hitAnimState = "Hit";
    public AudioClip hitSFX;

    /// <summary>
    /// Will allow this move to activate before other moves with a lower priority
    /// </summary>
    public int priority = 0;
}