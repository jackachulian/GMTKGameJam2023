// using UnityEditor.Animations;
using UnityEngine;

[CreateAssetMenu(fileName = "BattlerStats", menuName = "GMTKGameJam2023/Battler Stats", order = 0)]
public class BattlerStats : ScriptableObject
{
    public string displayName;

    public int hp;

    public Move[] moves;

    public Color nameColor;

    /// <summary>
    /// controls the sprite for this character, and can animate the sprite image's position, scale, etc
    /// </summary>
    public RuntimeAnimatorController controller;

    public Sprite portraitSprite;
}