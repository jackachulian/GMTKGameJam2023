using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "GMTKGameJam2023/Move", order = 0)]
public class Move : ScriptableObject {
    /// <summary>
    /// Required mana to use
    /// (UNUSED FOR NOW in favor of uses remaining system)
    /// </summary>
    [HideInInspector] public int manaCost;

    public int damage;
    
    /// <summary>
    /// Amount of times a battler can use this per battle.
    /// Uses remaining is stored on the battler.
    /// Count is reset to this base value at the start of a battle.
    /// </summary>
    public int baseUses;
}