using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "GMTKGameJam2023/Move", order = 0)]
public class Move : ScriptableObject {
    /// <summary>
    /// Required mana to use
    /// </summary>
    public int manaCost;

    public int damage;
}