using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BattleManager : MonoBehaviour
{
    public LevelList levelList;

    public PostgameManager postgameManager;

    /// <summary>
    /// Holds the two battlers
    /// </summary>
    public Battler[] battlers;

    public GameObject battlerPrefab;

    public Transform battlerTransform;

    /// <summary>
    /// Index in battlers[] of the battler the player is currently playing as, other battler will be computer controlled
    /// </summary>
    public int currentPlayerIndex;

    public Battler CurrentPlayer { get {return battlers[currentPlayerIndex];}}
    public Battler CurrentEnemy { get { return battlers[(currentPlayerIndex+1)%battlers.Length]; } }


    /// <summary>
    /// Transform that holds all the move button children to be updated when swapping battlers
    /// </summary>
    public Transform moveGridTransform, battleLogTransform;

    private Vector2 battleLogStartPos, battleLogTargetPos;

    public GameObject logMessagePrefab;

    private List<MoveButton> moveButtons;

    [SerializeField] private BattlerDisplay[] battlerDisplays;


    [SerializeField] private Animator platformRotationAnimator, battlerDisplaysAnimator, movesGridAnimator, battleLogAnimator;


    [SerializeField] private StatusType poisonStatusType, burnStatusType;

    // True while moves are shown and waiting for player to choose move
    public bool selectingMove;

    public bool isPostgame = false;

    // Start is called before the first frame update
    void Start()
    {
        Level level = levelList.levels[Storage.currentLevel];

        battlers = new Battler[level.battlers.Length];
        for (int i=0; i<level.battlers.Length; i++)
        {
            BattlerStats battlerStats = level.battlers[i];
            Battler battler = battlerTransform.GetChild(i).GetComponent<Battler>();
            battlers[i] = battler;
            battler.StatsSetup(battlerStats);
            // sets all battlers above playerAmount as a target
            if (i > level.playerAmount - 1) battler.isTarget = true;
        }


        Refresh();
        battleLogStartPos = battleLogTargetPos = battleLogTransform.localPosition;
        selectingMove = true;
    }

    Vector2 logVel;
    // Update is called once per frame
    void Update()
    {
        battleLogTransform.localPosition = Vector2.SmoothDamp(battleLogTransform.localPosition, battleLogTargetPos, ref logVel, 0.125f);
        battleLogTransform.localPosition = new Vector2(battleLogTargetPos.x, battleLogTransform.localPosition.y);

        if (Input.GetKeyDown(KeyCode.F1))
        {
            BattleMessage("Yahoo");
        }
    }

    private void OnValidate()
    {
        moveButtons = new List<MoveButton>();

        foreach (Transform child in moveGridTransform)
        {
            moveButtons.Add(child.GetComponent<MoveButton>());
        }

        Refresh();
    }

    /// <summary>
    /// player submits their move and uses it.
    /// Afterwards, opponent will user their decided move,
    /// and then the roles will swap andplayer will play as their enemy
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="move"></param>
    /// <returns>true if move was used successfully, false if use conditions not met (mana cost)</returns>
    public void SubmitMove(int moveIndex) {
        if (!selectingMove) return;
        // if (move.manaCost > CurrentPlayer.mp) return false;

        if (CurrentPlayer.moveUsesRemaining[moveIndex] <= 0) return;

        selectingMove = false;
        StartCoroutine(EvaluateTurn(moveIndex));
    }

    public void Refresh()
    {
        foreach (var moveButton in moveButtons)
        {
            if (moveButton) moveButton.Refresh();
        }

        RefreshBattlerDisplays();
    }

    public void RefreshBattlerDisplays()
    {
        foreach (var battlerDisplay in battlerDisplays)
        {
            if (battlerDisplay) battlerDisplay.Refresh();
        }
    }

    void UseMove(Battler attacker, Battler target, int moveIndex) {
        // don't move if dead or postgame
        if (attacker.isDead || isPostgame)
        {
            Refresh();
            return;
        }

        attacker.moveUsesRemaining[moveIndex]--;
        Move move = attacker.moves[moveIndex];

        attacker.spriteAnimator.Play("Base Layer." + move.useAnimState, 0);

        // Only say "used on {opponent}" if dealing damage or inflicting a status effect onto them
        if (move.damage > 0 || move.opponentEffect.duration > 0)
        {
            BattleMessage($"{attacker.coloredName} uses {move.displayName} on {target.coloredName}");
        } else
        {
            BattleMessage($"{attacker.coloredName} uses {move.displayName}");
        }

        StartCoroutine(DamageAfterAnimation(attacker, target, move));
    }

    IEnumerator DamageAfterAnimation(Battler attacker, Battler target, Move move)
    {
        yield return new WaitUntil(() => attacker.spriteAnimator.IsInTransition(0));

        // attacker.mp -= move.manaCost;
        
        // only play hit animation and dispaly damage in battle log if this deals any damage
        if (move.damage > 0)
        {
            target.hp -= move.damage;

            // Play hit animation on target when damaged
            if (move.hitAnimState.Length > 0) target.spriteAnimator.Play("Base Layer." + move.hitAnimState, 0);

            BattleMessage($"{target.coloredName} took {move.damage} damage!");
            CheckForDeaths();
            Refresh();
        }

        // stop if in postgame
        if (isPostgame) yield break;

        // Inflict status effect on self
        if (move.selfEffect.duration > 0)
        {
            yield return new WaitForSeconds(0.5f);

            string turnsStr = move.selfEffect.duration == 1 ? "turn" : "turns";
            switch (move.selfEffect.type.name)
            {
                case "Poison": BattleMessage($"{attacker.coloredName} was poisoned for {move.selfEffect.duration} {turnsStr}!"); break;
                case "Fire": BattleMessage($"{attacker.coloredName} was burned for {move.selfEffect.duration} {turnsStr}!"); break;
            }

            AddStatus(attacker, move.selfEffect.type, move.selfEffect.duration);
            Refresh();
        }

        // Inflict status effect on opponent
        if (move.opponentEffect.duration > 0)
        {
            yield return new WaitForSeconds(0.5f);

            string turnsStr = move.opponentEffect.duration == 1 ? "turn" : "turns";
            switch (move.opponentEffect.type.name)
            {
                case "Poison": BattleMessage($"{target.coloredName} was poisoned for {move.opponentEffect.duration} {turnsStr}!"); break;
                case "Fire": BattleMessage($"{target.coloredName} was burned for {move.opponentEffect.duration} {turnsStr}!"); break;
            }

            AddStatus(target, move.opponentEffect.type, move.opponentEffect.duration);
            Refresh();
        }
    }

    void CheckForDeaths()
    {
        foreach (Battler battler in battlers)
        {
            // battler has died
            if (battler.hp <= 0)
            {
                BattleMessage($"{battler.coloredName} was slain!");
                battler.isDead = true;
                
                // check if all player or target battlers have died
                CheckForPostgame();
            }
        }
    }

    void CheckForPostgame()
    {
        // check for win, all targets are dead
        bool won = true;
        foreach (Battler battler in battlers)
        {
            if (battler.isTarget && !battler.isDead) won = false;
        }

        // check for lose, all non-targets are dead
        bool lost = true;
        foreach (Battler battler in battlers)
        {
            if (!battler.isTarget && !battler.isDead) lost = false;
        }

        if (won) StartCoroutine(Win());
        if (lost) StartCoroutine(Lose());
    }

    void AddStatus(Battler target, StatusType statusType, int duration)
    {
        foreach (StatusEffect statusEffect in target.statusEffects)
        {
            if (statusEffect.type == statusType)
            {
                statusEffect.duration += duration;
                return;
            }
        }

        StatusEffect newStatusEffect = new StatusEffect();
        newStatusEffect.type = statusType;
        newStatusEffect.duration = duration;

        target.statusEffects.Add(newStatusEffect);
    }

    IEnumerator EvaluateTurn(int playerMoveIndex)
    {
        // temporary only for move list visual to show that one use was used when selecting the move,
        // actual decrement happens later in UseMove
        CurrentPlayer.moveUsesRemaining[playerMoveIndex]--;
        Refresh();
        

        // Wait for the buttons to fade out before using move so that log can show use correctly
        movesGridAnimator.SetBool("ShowMoves", false);
        yield return new WaitUntil(() => movesGridAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        ClearBattleLog();
        battleLogAnimator.SetBool("ShowLog", true);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        // Player uses their move
        CurrentPlayer.moveUsesRemaining[playerMoveIndex]++; // (undo for temp change at start of this method)
        UseMove(CurrentPlayer, CurrentEnemy, playerMoveIndex);

        // Wait for player animation
        yield return new WaitUntil(() => CurrentPlayer.spriteAnimator.IsInTransition(0));
        yield return new WaitForSeconds(1.33f);

        // Enemy selects and uses a move
        // TODO: if enemy has no moves left, have them do nothing, struggle, etc. we'll figure that out later
        UseMove(CurrentEnemy, CurrentPlayer, Random.Range(0, 4));

        // Wait for enemy animation
        yield return new WaitUntil(() => CurrentEnemy.spriteAnimator.IsInTransition(0));
        yield return new WaitForSeconds(1.33f);

        // Tick down status effects
        yield return TickStatusEffects(CurrentPlayer);
        yield return TickStatusEffects(CurrentEnemy);
        yield return new WaitForSeconds(1.25f);

        // === SWAP BEGINS HERE ===
        // Swap which battler is controlled by the character
        currentPlayerIndex = (currentPlayerIndex + 1) % battlers.Length;

        // may need to change this ode once more than 1 enemy is added
        platformRotationAnimator.SetBool("Player1OnRight", currentPlayerIndex == 1);
        battlerDisplaysAnimator.SetBool("Player1OnRight", currentPlayerIndex == 1);
        
        Refresh();

        // Wait until a lil bit through the animation to re show the moves
        yield return new WaitForSeconds(0.25f);

        battleLogAnimator.SetBool("ShowLog", false);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        movesGridAnimator.SetBool("ShowMoves", true);
        selectingMove = true;
    }

    public void ClearBattleLog()
    {
        foreach (Transform child in battleLogTransform)
        {
            Destroy(child.gameObject);
        }

        battleLogTargetPos = battleLogStartPos;
    }

    public void BattleMessage(string text)
    {
        GameObject logMessage = Instantiate(logMessagePrefab, battleLogTransform);

        logMessage.GetComponentInChildren<TextMeshProUGUI>().text = text;

        // If there are 5 or more lines, start scrolling the view
        if (battleLogTransform.childCount >= 5)
        {
            battleLogTargetPos += Vector2.up * 48f;
        }
    }

    /// <summary>
    /// Take damage from poison, burn, etc..., then reduce all status duration by 1
    /// </summary>
    IEnumerator TickStatusEffects(Battler battler)
    {
        for (int i=0; i<battler.statusEffects.Count; i++)
        {
            StatusEffect statusEffect = battler.statusEffects[i];
            // Only tick damage effects and duration if not duration paused if this status was just inflicted this turn.
            if (statusEffect.DurationPaused)
            {
                statusEffect.DurationPaused = false;
            } else
            {
                bool delayAfter = true;
                switch (statusEffect.type.name)
                {
                    case "Poison": StatusDamage(battler, statusEffect, 1); break;
                    case "Fire": StatusDamage(battler, statusEffect, statusEffect.duration); break;
                    default: delayAfter = false; break;
                }

                statusEffect.duration--;
                if (statusEffect.duration <= 0)
                {
                    battler.statusEffects.RemoveAt(i);
                    i--;
                }

                RefreshBattlerDisplays();
                if (delayAfter) yield return new WaitForSeconds(0.8f);
            } 
        }

        yield return new WaitForSeconds(0.2f);
    }

    void StatusDamage(Battler battler, StatusEffect statusEffect, int damage)
    {
        battler.hp -= damage;
        RefreshBattlerDisplays();
        battler.spriteAnimator.Play("Base Layer.Hit", 0);
        
        switch(statusEffect.type.name)
        {
            case "Poison": BattleMessage($"{battler.coloredName} took {damage} poison damage"); break;
            case "Fire": BattleMessage($"{battler.coloredName} took {damage} burn damage"); break;
        }
    }

    private IEnumerator Win()
    {
        // StopAllCoroutines();
        BattleMessage("YOU WON!");
        isPostgame = true;

        yield return new WaitForSeconds(1f);
        StopAllCoroutines();

        StartCoroutine(postgameManager.Win());
    }

    private IEnumerator Lose()
    {
        
        BattleMessage("You were defeated...");
        isPostgame = true;
        
        yield return new WaitForSeconds(1f);
        StopAllCoroutines();

        StartCoroutine(postgameManager.Lose());
    }
}
