using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class Battler : MonoBehaviour
{
    public Sprite portraitSprite;
    public Animator spriteAnimator;

    public int maxHp;
    public int hp;

    // public int maxMp;
    // public int mp;

    public string displayName;

    public string coloredName { get { return $"<color=#{nameColor.ToHexString()}>{displayName}</color>"; } }

    /// <summary>
    /// For when this battler's name shows up in the log, what color it should be
    /// </summary>
    public Color nameColor;

    public Move[] moves;

    public List<StatusEffect> statusEffects;

    /// <summary>
    /// Index corresponds to index in moves[]
    /// </summary>
    [HideInInspector] public int[] moveUsesRemaining;

    /// <summary>
    /// When true, this battler must be killed to win the level
    /// </summary>
    public bool isTarget = false;

    [NonSerialized] public bool isDead = false;

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

    public void StatsSetup(BattlerStats stats)
    {
        displayName = stats.displayName;
        hp = stats.hp;
        maxHp = stats.hp;
        moves = stats.moves;
        nameColor = stats.nameColor;
        portraitSprite = stats.portraitSprite;

        spriteAnimator.runtimeAnimatorController = stats.controller;
    }

    void ResetMoveUses()
    {
        moveUsesRemaining = new int[moves.Length];

        for (int i=0; i<moves.Length; i++)
        {
            moveUsesRemaining[i] = moves[i].baseUses;
        }
    }

    public bool HasStatus(string statusName)
    {
        foreach (StatusEffect status in statusEffects)
        {
            if (status.type.statusName.Equals(statusName)) return true;
        }
        return false;
    }

    public StatusEffect GetStatusOfName(string statusName)
    {
        foreach (StatusEffect status in statusEffects)
        {
            if (status.type.statusName.Equals(statusName)) return status;
        }
        return null;
    }
}
