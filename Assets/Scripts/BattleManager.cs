using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Animations;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random=UnityEngine.Random;
using System;

public class BattleManager : MonoBehaviour
{
    public LevelList levelList;

    // current level
    public Level level;

    public PostgameManager postgameManager;

    [SerializeField] AudioClip messageSFX, swapSFX;

    /// <summary>
    /// Holds all battler monobehaviours
    /// </summary>
    [HideInInspector] public Battler[] battlers;

    public GameObject battlerPrefab;

    public Transform battlerTransform;

    /// <summary>
    /// Index in battlers[] of the battler the player is currently playing as, other battler will be computer controlled
    /// </summary>
    public int currentPlayerIndex;

    public Battler CurrentPlayer { get {return battlers[currentPlayerIndex];}}
    public Battler CurrentEnemy { get { return battlers[(currentPlayerIndex+1)%battlers.Length]; } }

    public Animator platformAnimator;
    public AnimatorController platformController, triplePlatformController;

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
    public bool selectingTarget;

    public bool isPostgame = false;

    // Start is called before the first frame update
    void Start()
    {
        StartLevel();
    }

    void StartLevel()
    {
        level = levelList.levels[Storage.currentLevel];

        currentPlayerIndex = 0;
        isPostgame = false;

        platformAnimator.SetInteger("PlayerIndex", 0);
        platformAnimator.SetBool("LoweredPlatform", true);
        StartCoroutine(SetBattlersWhenLowered());

        if (level.levelStartDialogue.Length > 0)
        {
            Refresh();
            StartCoroutine(PlayCutscene(level.levelStartDialogue, start: true));
        }
        else
        {
            StartCoroutine(StartBattle());
        }
    }

    void StartNextLevel()
    {
        Storage.currentLevel++;
        StartLevel();
    }

    /// <summary>
    /// Will wait until platforms are fully lowered to set the sprites
    /// </summary>
    /// <returns></returns>
    IEnumerator SetBattlersWhenLowered()
    {
        yield return new WaitUntil(() => platformAnimator.GetCurrentAnimatorStateInfo(0).IsName("Lowered"));

        if (level.battlers.Length >= 3)
        {
            platformAnimator.runtimeAnimatorController = triplePlatformController;
        }
        else
        {
            platformAnimator.runtimeAnimatorController = platformController;
        }

        // Hide un needed battlers
        foreach (Transform child in battlerTransform) child.gameObject.SetActive(false);

        battlers = new Battler[level.battlers.Length];
        for (int i = 0; i < level.battlers.Length; i++)
        {
            BattlerStats battlerStats = level.battlers[i];
            Battler battler = battlerTransform.GetChild(i).GetComponent<Battler>();
            battler.gameObject.SetActive(true);
            battlers[i] = battler;
            battler.StatsSetup(battlerStats);
            // sets all battlers above playerAmount as a target
            if (i >= level.playerAmount) battler.isTarget = true;
        }
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

    IEnumerator StartBattle()
    {
        if (platformRotationAnimator.GetBool("LoweredPlatform"))
        {
            platformRotationAnimator.SetBool("LoweredPlatform", false);
            yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        }

        battlerDisplaysAnimator.SetBool("Hidden", false);
        movesGridAnimator.SetBool("ShowMoves", true);
        Refresh();
        battleLogStartPos = battleLogTargetPos = battleLogTransform.localPosition;
        selectingMove = true;

        SoundManager.Instance.SetBGM(level.bgm);
    }

    private List<Battler> possibleTargets;

    private int selectedMoveIndex;

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

        possibleTargets = new List<Battler>();
        foreach (Battler battler in battlers)
        {
            if (battler != CurrentPlayer && !battler.isDead)
            {
                possibleTargets.Add(battler);
            }
        }

        selectedMoveIndex = moveIndex;
        selectingMove = false;
        if (possibleTargets.Count > 1)
        {
            selectingTarget = true;
        } else if (possibleTargets.Count > 0)
        {
            selectedBattlerIndex = (currentPlayerIndex + 1) % battlers.Length;
            StartCoroutine(EvaluateTurn());
        }        
    }

    private int selectedBattlerIndex;
    public void SubmitTarget(int battlerIndex)
    {
        battlerIndex = (battlerIndex + currentPlayerIndex) % battlers.Length;

        if (!selectingTarget) return;

        Battler selectedTarget = battlers[battlerIndex];
        if (selectedTarget != CurrentPlayer && !selectedTarget.isDead)
        {
            selectedBattlerIndex = battlerIndex;
            selectingTarget = false;
            StartCoroutine(EvaluateTurn());
        }
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
        if (move.useSFX != null) SoundManager.Instance.PlaySound(move.useSFX);

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
        if (move.damage > 0 && !target.HasStatus("Counter"))
        {
            target.hp -= move.damage;

            if (target.hp <= 0)
            {
                target.spriteAnimator.Play("Base Layer.defeat", 0);
            } 
            else
            {
                // Play hit animation on target when damaged
                if (move.hitAnimState.Length > 0)
                {
                    target.spriteAnimator.Play("Base Layer." + move.hitAnimState, 0);
                    
                }
            }
            if (move.hitSFX != null) SoundManager.Instance.PlaySound(move.hitSFX);

            BattleMessage($"{target.coloredName} took {move.damage} damage!");
        }
        // counter move if applicable
        else if (move.damage > 0 && target.HasStatus("Counter"))
        {
            BattleMessage($"{target.coloredName} counters {attacker.coloredName}'s attack!");
            UseMove(target, attacker, 0);
            target.GetStatusOfName("Counter").duration -= 1;
            Refresh();
            RefreshStatusEffects(target);
        }

        CheckForDeaths();
        Refresh();

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

        if (lost) StartCoroutine(Lose());
        else if (won) StartCoroutine(Win());
    }

    void AddStatus(Battler target, StatusType statusType, int duration)
    {
        foreach (StatusEffect statusEffect in target.statusEffects)
        {
            if (statusEffect.type == statusType)
            {
                statusEffect.duration = Math.Min(duration+statusEffect.duration, statusEffect.type.maxDuration);
                return;
            }
        }

        StatusEffect newStatusEffect = new StatusEffect();
        newStatusEffect.type = statusType;
        newStatusEffect.duration = duration;

        target.statusEffects.Add(newStatusEffect);
    }

    IEnumerator EvaluateTurn()
    {
        // temporary only for move list visual to show that one use was used when selecting the move,
        // actual decrement happens later in UseMove
        CurrentPlayer.moveUsesRemaining[selectedMoveIndex]--;
        Refresh();
        

        // Wait for the buttons to fade out before using move so that log can show use correctly
        movesGridAnimator.SetBool("ShowMoves", false);
        yield return new WaitUntil(() => movesGridAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        ClearBattleLog();
        battleLogAnimator.SetBool("ShowLog", true);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        CurrentPlayer.moveUsesRemaining[selectedMoveIndex]++; // (undo for temp change at start of this method)

        foreach (Battler battler in battlers)
        {
            if (battler == CurrentPlayer)
            {
                battler.selectedMove = battler.moves[selectedMoveIndex];
            } else
            {
                List<Move> usableMoves = new List<Move>();
                for (int i = 0; i < battler.moves.Length; i++)
                {
                    if (battler.moveUsesRemaining[i] > 0)
                    {
                        usableMoves.Add(battler.moves[i]);
                    }
                }

                // enemy """"AI""""
                battler.selectedMove = usableMoves[Random.Range(0, usableMoves.Count)];
            }
        }

        // Sort act order by priority
        // if tie, player will go first, or if both enemies, lower index in arr first (i think thats how the sort works)
        List<Battler> actionOrder = new List<Battler>(battlers);
        actionOrder.Sort((b1, b2) =>
        {
            int priorityDifference = b2.selectedMove.priority - b1.selectedMove.priority;
            if (priorityDifference != 0) return priorityDifference;

            if (b1.battlerIndex == currentPlayerIndex && b2.battlerIndex != currentPlayerIndex) return -1;
            if (b2.battlerIndex == currentPlayerIndex && b1.battlerIndex != currentPlayerIndex) return 1;

            return 0;
        });

        foreach (Battler battler in actionOrder) yield return Act(battler);
        
        // Tick down status effects
        yield return TickStatusEffects(CurrentPlayer);
        yield return TickStatusEffects(CurrentEnemy);

        CheckForPostgame();
        if (isPostgame) yield break;

        yield return new WaitForSeconds(1.25f);

        // === SWAP BEGINS HERE ===
        // Swap which battler is controlled by the character
        SoundManager.Instance.PlaySound(swapSFX, pitch:1.05f);
        currentPlayerIndex = (currentPlayerIndex + 1) % battlers.Length;

        // may need to change this ode once more than 1 enemy is added
        battlerDisplaysAnimator.SetInteger("PlayerIndex", currentPlayerIndex);
        platformRotationAnimator.SetInteger("PlayerIndex", currentPlayerIndex);

        Refresh();

        // Wait until a lil bit through the animation to re show the moves
        yield return new WaitForSeconds(0.25f);

        battleLogAnimator.SetBool("ShowLog", false);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        movesGridAnimator.SetBool("ShowMoves", true);
        selectingMove = true;
    }

    IEnumerator Act(Battler attacker)
    {
        if (attacker == CurrentPlayer)
        {
            UseMove(CurrentPlayer, battlers[selectedBattlerIndex], selectedMoveIndex);
            // Wait for player animation
            yield return new WaitUntil(() => CurrentPlayer.spriteAnimator.IsInTransition(0));
            
        } else
        {
            // Enemy selects and uses a move
            // TODO: if enemy has no moves left, have them do nothing, struggle, etc. we'll figure that out later
            UseMove(attacker, CurrentPlayer, Random.Range(0, 4));

            // Wait for enemy animation
            yield return new WaitUntil(() => attacker.spriteAnimator.IsInTransition(0));
        }

        yield return new WaitForSeconds(1.33f);
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

        SoundManager.Instance.PlaySound(messageSFX, volume: 0.25f, pitch: Random.Range(0.9f,1.1f));
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

                RefreshStatusEffects(battler);
                RefreshBattlerDisplays();
                if (delayAfter) yield return new WaitForSeconds(0.8f);
            } 
        }

        yield return new WaitForSeconds(0.2f);
    }

    void RefreshStatusEffects(Battler battler)
    {
        for (int i = 0; i < battler.statusEffects.Count; i++)
        {
            StatusEffect statusEffect = battler.statusEffects[i];
            if (statusEffect.duration <= 0)
            {
                battler.statusEffects.RemoveAt(i);
                i--;
            }
        }
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
        SoundManager.Instance.StopBGM();

        yield return new WaitForSeconds(1f);
        StopAllCoroutines();

        postgameManager.Win();

        Level level = levelList.levels[Storage.currentLevel];

        if (level.levelEndDialogue.Length > 0)
        {
            StartCoroutine(PlayCutscene(level.levelEndDialogue, end: true));
        } else
        {
            StartNextLevel();
        }

    }

    private IEnumerator Lose()
    {
        BattleMessage("You were slain...");
        isPostgame = true;
        SoundManager.Instance.StopBGM();

        postgameManager.Lose();

        yield return new WaitForSeconds(1f);
        StopAllCoroutines();
    }

    /// <summary>
    /// Split the passed string into lines and display them one by one with delay between to the battle log.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="start">if battle should be started after cutscene</param>
    /// <param name="end">if next level should be transitioned to after this battle</param>
    /// <returns></returns>
    IEnumerator PlayCutscene(string text, bool start = false, bool end = false)
    {
        battlerDisplaysAnimator.SetBool("Hidden", true);

        if (movesGridAnimator.GetBool("ShowMoves"))
        {
            movesGridAnimator.SetBool("ShowMoves", false);
            yield return new WaitUntil(() => movesGridAnimator.IsInTransition(0));
        }
        ClearBattleLog();
        battleLogAnimator.SetBool("ShowLog", true);
        yield return new WaitForSeconds(1f);

        string[] lines = text.Split('\n');
        foreach (string line in lines)
        {
            switch (line)
            {
                case "{RaisePlatform}":
                    platformRotationAnimator.SetBool("LoweredPlatform", false);
                    break;

                case "{LowerPlatform}":
                    platformRotationAnimator.SetBool("LoweredPlatform", true);
                    break;

                // time constraint moment
                case "{Wait1}":
                    yield return new WaitForSeconds(1);
                    break;

                case "{Wait0.5}":
                    yield return new WaitForSeconds(0.5f);
                    break;

                default:
                    BattleMessage(line);
                    yield return new WaitForSeconds(1.5f);
                    break;
            }
                
            
        }
        new WaitForSeconds(2f);

        battleLogAnimator.SetBool("ShowLog", false);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));

        if (start)
        {
            yield return StartBattle();
        }

        if (end)
        {
            StartNextLevel();
        }
    }
}
