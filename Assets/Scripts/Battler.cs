using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Battler : MonoBehaviour
{
    public Sprite portraitSprite;
    public Animator spriteAnimator;

    public int maxHp;
    public int hp;

    public int maxMp;
    public int mp;

    public string displayName {  get { return name; } }

    public string coloredName { get { return $"<color=#{nameColor.ToHexString()}>{displayName}</color>"; } }

    /// <summary>
    /// For when this battler's name shows up in the log, what color it should be
    /// </summary>
    public Color nameColor;

    public Move[] moves;

    public StatusEffect[] statusEffects;

    /// <summary>
    /// Index corresponds to index in moves[]
    /// </summary>
    [HideInInspector] public int[] moveUsesRemaining;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        ResetMoveUses();
    }

    void ResetMoveUses()
    {
        moveUsesRemaining = new int[moves.Length];

        for (int i=0; i<moves.Length; i++)
        {
            moveUsesRemaining[i] = moves[i].baseUses;
        }
    }
}
