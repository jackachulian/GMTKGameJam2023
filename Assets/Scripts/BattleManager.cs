using System.Collections;
using System.Collections.Generic;
using TMPro;
// using UnityEditor.Animations;
// using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random=UnityEngine.Random;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    public GameObject youIcon;

    public Transform battlerTransform;

    /// <summary>
    /// Index in battlers[] of the battler the player is currently playing as, other battler will be computer controlled
    /// </summary>
    public int currentPlayerIndex;

    public Battler CurrentPlayer { get {return battlers[currentPlayerIndex];}}
    public Battler CurrentEnemy { get { return battlers[(currentPlayerIndex+1)%battlers.Length]; } }

    public Animator platformAnimator;
    public RuntimeAnimatorController platformController, triplePlatformController;

    public RuntimeAnimatorController battlerDisplayController, triplebattlerDisplayController;

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

    /// <summary>
    /// Color to dim everything else when selecting a battler.
    /// </summary>
    [SerializeField] private Color dimColor;

    [SerializeField] private Graphic[] targetSelectDim;
    [SerializeField] private SpriteRenderer[] targetSelectSpriteDim;

    // True while moves are shown and waiting for player to choose move
    public bool selectingMove;
    public bool selectingTarget;

    public bool isPostgame = false;

    public Move struggle;

    // Start is called before the first frame update
    void Start()
    {
        moveButtons = new List<MoveButton>();

        foreach (Transform child in moveGridTransform)
        {
            moveButtons.Add(child.GetComponent<MoveButton>());
        }

        Refresh();

        StartLevel();
    }

    void StartLevel()
    {
        level = levelList.levels[Storage.currentLevel];

        youIcon.SetActive(Storage.currentLevel == 0);

        currentPlayerIndex = 0;
        isPostgame = false;
        selectingMove = false;
        selectingTarget = false;

    platformAnimator.SetInteger("PlayerIndex", 0);
        platformAnimator.SetBool("LoweredPlatform", true);
        StartCoroutine(SetBattlersWhenLowered());

        battlerDisplaysAnimator.SetInteger("PlayerIndex", 0);

        if (level.levelStartDialogue.Length > 0)
        {
            Refresh();
            StartCoroutine(PlayCutscene(level.levelStartDialogue, start: true));
        }
        else
        {
            StartCoroutine(StartBattle());
        }

        foreach (Battler battler in battlers)
        {
            // battler.ResetMoveUses();
            battler.statusEffects = new List<StatusEffect>();
        }
    }

    void StartNextLevel()
    {
        Storage.currentLevel++;
        if (Storage.currentLevel >= levelList.levels.Count) SceneManager.LoadScene("End");
        else StartLevel();
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
            battlerDisplaysAnimator.runtimeAnimatorController = triplebattlerDisplayController;
        }
        else
        {
            platformAnimator.runtimeAnimatorController = platformController;
            battlerDisplaysAnimator.runtimeAnimatorController = battlerDisplayController;
        }

        platformAnimator.SetBool("LoweredPlatform", false);
        platformAnimator.SetInteger("PlayerIndex", 0);
        battlerDisplaysAnimator.SetInteger("PlayerIndex", 0);

        // Hide un needed battlers
        foreach (Transform child in battlerTransform) child.gameObject.SetActive(false);
        foreach (BattlerDisplay display in battlerDisplays) display.gameObject.SetActive(false);

        battlers = new Battler[level.battlers.Length];
        for (int i = 0; i < level.battlers.Length; i++)
        {
            BattlerStats battlerStats = level.battlers[i];
            Battler battler = battlerTransform.GetChild(i).GetComponent<Battler>();
            battler.gameObject.SetActive(true);
            battlers[i] = battler;
            battlerDisplays[i].gameObject.SetActive(true);
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

    IEnumerator StartBattle()
    {
        SoundManager.Instance.StopBGM();
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
        SoundManager.Instance.PlayBGM();
        Refresh();
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

        Move selectedMove = null;

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
        selectedMove = CurrentPlayer.moves[selectedMoveIndex];

        if (CurrentPlayer.moveUsesRemaining[moveIndex] <= 0)
        {
            selectedMove = struggle;
            selectedMoveIndex = -1;
        }
        if (possibleTargets.Count > 1 && (selectedMove.damage > 0 || selectedMove.opponentEffect.duration > 0))
        {
            selectingTarget = true;
            SetDim(true);
        } else if (possibleTargets.Count > 0)
        {
            selectedBattlerIndex = (currentPlayerIndex + 1) % battlers.Length;
            StartCoroutine(EvaluateTurn());
        }        
    }

    private Color[] originalColors;
    private bool selectDim = false;
    public void SetDim(bool dim)
    {
        if (dim == selectDim) return;
        selectDim = dim;
        Color targetColor = dim ? Color.Lerp(Color.white, dimColor, 0.5f) : Color.white;

        if (dim) originalColors = new Color[targetSelectDim.Length];

        for (int i = 0; i < targetSelectDim.Length; i++)
        {
            Graphic graphic = targetSelectDim[i];
            if (dim)
            {
                originalColors[i] = graphic.color;
                graphic.color = Color.Lerp(graphic.color, dimColor, 0.5f);
            } else
            {
                graphic.color = originalColors[i];
            }
        }

        foreach (SpriteRenderer renderer in targetSelectSpriteDim)
        {
            renderer.color = targetColor;
        }

        foreach (Battler battler in battlers)
        {
            if (dim && (battler == CurrentPlayer || battler.isDead))
            {
                battler.spriteRenderer.color = targetColor;
            } else
            {
                battler.spriteRenderer.color = Color.white;
            }
        }
    }

    private int selectedBattlerIndex;
    public void SubmitTarget(int battlerIndex)
    {
        if (!selectingTarget) return;

        Battler selectedTarget = battlers[battlerIndex];
        if (selectedTarget != CurrentPlayer && !selectedTarget.isDead)
        {
            selectedBattlerIndex = battlerIndex;
            selectingTarget = false;
            SetDim(false);
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

        Move move;
        if (moveIndex != -1)
        {
            attacker.moveUsesRemaining[moveIndex]--;
            move = attacker.moves[moveIndex];
        }
        else move = struggle;

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
        
        // healing
        if (move.damage < 0)
        {
            attacker.hp = Math.Min(attacker.hp-move.damage, attacker.maxHp);
            BattleMessage($"{attacker.coloredName} heals {Math.Abs(move.damage)} HP.");
            Refresh();
        }

        StartCoroutine(DamageAfterAnimation(attacker, target, move));
    }

    IEnumerator DamageAfterAnimation(Battler attacker, Battler target, Move move)
    {
        yield return new WaitForSeconds(1f);

        // attacker.mp -= move.manaCost;
        
        // only play hit animation and dispaly damage in battle log if this deals any damage
        if (move.damage > 0 && !target.HasStatus("Counter"))
        {
            int finalDmg = (move.damage - (target.HasStatus("Block") ? 3 : 0));
            target.hp -= finalDmg;

            if (target.hp <= 0)
            {
                
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

            BattleMessage($"{target.coloredName} took {finalDmg} damage!");
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

        if (move.displayName.Equals("Purify"))
        {
            foreach (StatusEffect se in attacker.statusEffects)
            {
                
            }

            for (int i = 0; i < attacker.statusEffects.Count; i++)
            {
                if (attacker.statusEffects[i] != attacker.GetStatusOfName("Pure"))
                {
                    attacker.statusEffects.RemoveAt(i);
                    i--;
                } 
            }
        }

        // Inflict status effect on self
        if (move.selfEffect.duration > 0)
        {
            if (!attacker.HasStatus("Pure") || move.selfEffect.type.statusName.Equals("Pure"))
            {
                yield return new WaitForSeconds(0.5f);

                string turnsStr = move.selfEffect.duration == 1 ? "turn" : "turns";
                switch (move.selfEffect.type.name)
                {
                    case "Poison": BattleMessage($"{attacker.coloredName} was poisoned for {move.selfEffect.duration} {turnsStr}!"); break;
                    case "Fire": BattleMessage($"{attacker.coloredName} was burned for {move.selfEffect.duration} {turnsStr}!"); break;
                }

                AddStatus(attacker, move.selfEffect.type, move.selfEffect.duration);
            }
            else
            {
                // status blocked by pure
                BattleMessage($"{attacker.coloredName} was purified of the status!");
            }

            Refresh();
        }

        // epic hard code moment
        if (move.displayName.Equals("Status Swap"))
        {
            List<StatusEffect> temp = new List<StatusEffect>(attacker.statusEffects);
            attacker.statusEffects = new List<StatusEffect>(target.statusEffects);
            target.statusEffects = new List<StatusEffect>(temp);
            BattleMessage($"Status effects have been swapped!");
        }

        Refresh();

        // Inflict status effect on opponent
        if (move.opponentEffect.duration > 0)
        {
            if(!target.HasStatus("Pure"))
            {
                yield return new WaitForSeconds(0.5f);

                string turnsStr = move.opponentEffect.duration == 1 ? "turn" : "turns";
                switch (move.opponentEffect.type.name)
                {
                    case "Poison": BattleMessage($"{target.coloredName} was poisoned for {move.opponentEffect.duration} {turnsStr}!"); break;
                    case "Fire": BattleMessage($"{target.coloredName} was burned for {move.opponentEffect.duration} {turnsStr}!"); break;
                }

                AddStatus(target, move.opponentEffect.type, move.opponentEffect.duration);
            }
            else
            {
                BattleMessage($"{target.coloredName} was purified of the status!");
            }
            Refresh();
        }
    }

    void CheckForDeaths()
    {
        foreach (Battler battler in battlers)
        {
            // battler has died
            if (battler.hp <= 0 && !battler.isDead)
            {
                BattleMessage($"{battler.coloredName} was slain!");
                battler.isDead = true;
                battler.spriteAnimator.Play("Base Layer.defeat", 0);
                
                // check if all player or target battlers have died
                CheckForPostgame();
            }
        }
    }

    void CheckForPostgame()
    {
        if (isPostgame) return;
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
        Refresh();
    }

    IEnumerator EvaluateTurn()
    {
        // temporary only for move list visual to show that one use was used when selecting the move,
        // actual decrement happens later in UseMove

        if (selectedMoveIndex > -1) CurrentPlayer.moveUsesRemaining[selectedMoveIndex]--;
        Refresh();

        // Wait for the buttons to fade out before using move so that log can show use correctly
        movesGridAnimator.SetBool("ShowMoves", false);
        yield return new WaitUntil(() => movesGridAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        ClearBattleLog();
        battleLogAnimator.SetBool("ShowLog", true);
        yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        if (selectedMoveIndex > -1) CurrentPlayer.moveUsesRemaining[selectedMoveIndex]++; // (undo for temp change at start of this method)

        foreach (Battler battler in battlers)
        {
            if (selectedMoveIndex == -1)
            {
                battler.selectedMove = struggle;
            }
            else
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
                    try{
                        battler.selectedMove = usableMoves[Random.Range(0, usableMoves.Count)];
                    }
                    catch
                    {
                        battler.selectedMove = struggle;
                    }
                    
                }
                }

        }

        // Sort act order by priority
        // if tie, player will go first, or if both enemies, lower index in arr first (i think thats how the sort works)
        List<Battler> actionOrder = new List<Battler>(battlers);
        actionOrder.Sort((b1, b2) =>
        {
            int priorityDifference = b1.selectedMove.priority - b2.selectedMove.priority;
            if (priorityDifference != 0) return priorityDifference;

            if (b1.battlerIndex == currentPlayerIndex && b2.battlerIndex != currentPlayerIndex) return -1;
            if (b2.battlerIndex == currentPlayerIndex && b1.battlerIndex != currentPlayerIndex) return 1;

            return 0;
        });

        foreach (Battler battler in actionOrder) yield return Act(battler, false);

        // Tick down status effects
        foreach (Battler battler in actionOrder) yield return TickStatusEffects(battler);

        CheckForPostgame();
        if (isPostgame) yield break;

        yield return new WaitForSeconds(1.25f);

        // === SWAP BEGINS HERE ===
        // Swap which battler is controlled by the character
        SoundManager.Instance.PlaySound(swapSFX, pitch:1.05f);

        // skip dead players
        currentPlayerIndex = (currentPlayerIndex - 1 + battlers.Length) % battlers.Length;

        while (CurrentPlayer.isDead)
        {
            yield return new WaitForSeconds(1f);
            currentPlayerIndex = (currentPlayerIndex - 1 + battlers.Length) % battlers.Length;
            // may need to change this ode once more than 1 enemy is added
            battlerDisplaysAnimator.SetInteger("PlayerIndex", currentPlayerIndex);
            platformRotationAnimator.SetInteger("PlayerIndex", currentPlayerIndex);
        }

        battlerDisplaysAnimator.SetInteger("PlayerIndex", currentPlayerIndex);
        platformRotationAnimator.SetInteger("PlayerIndex", currentPlayerIndex);
        Refresh();

        // Wait until a lil bit through the animation to re show the moves
        yield return new WaitForSeconds(0.25f);

        battleLogAnimator.SetBool("ShowLog", false);
        // yield return new WaitUntil(() => battleLogAnimator.IsInTransition(0));
        yield return new WaitForSeconds(0.5f);

        movesGridAnimator.SetBool("ShowMoves", true);
        selectingMove = true;
        // StruggleCheck();
        
    }

    // public void StruggleCheck()

    IEnumerator Act(Battler attacker, bool useStruggle)
    {
        if (attacker.isDead) yield break;

        List<int> validMoves = attacker.GetValidMoves();
        if (useStruggle) UseMove(attacker, battlers[selectedBattlerIndex], -1);
        else
        {
            if (attacker == CurrentPlayer)
            {
                UseMove(CurrentPlayer, battlers[selectedBattlerIndex], selectedMoveIndex);
                
            } else
            {
                // Enemy selects and uses a move
                // TODO: if enemy has no moves left, have them do nothing, struggle, etc. we'll figure that out later
                if (validMoves.Count == 0) UseMove(attacker, battlers[currentPlayerIndex], -1);
                UseMove(attacker, battlers[currentPlayerIndex], validMoves[Random.Range(0,validMoves.Count)]);
            }
        }


        yield return new WaitForSeconds(1.5f);
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
                    case "Poison": StatusDamage(battler, statusEffect, 3); break;
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
        CheckForDeaths();
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
        BattleMessage("You slayed yourself...");
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
