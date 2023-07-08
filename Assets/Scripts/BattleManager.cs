using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    /// <summary>
    /// Holds the two battlers
    /// </summary>
    public Battler[] battlers;

    /// <summary>
    /// Index in battlers[] of the battler the player is currently playing as, other battler will be computer controlled
    /// </summary>
    public int currentPlayerIndex;

    public Battler CurrentPlayer { get {return battlers[currentPlayerIndex];}}
    public Battler CurrentEnemy { get { return battlers[(currentPlayerIndex+1)%battlers.Length]; } }


    /// <summary>
    /// Transform that holds all the move button children to be updated when swapping battlers
    /// </summary>
    public Transform moveGrid; 

    private List<MoveButton> moveButtons;

    [SerializeField] private BattlerDisplay[] battlerDisplays;


    [SerializeField] private Animator platformRotationAnimator, battlerDisplaysAnimator;

    // Start is called before the first frame update
    void Start()
    {
        Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        moveButtons = new List<MoveButton>();

        foreach (Transform child in moveGrid)
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
    public bool SubmitMove(int moveIndex) {
        // if (move.manaCost > CurrentPlayer.mp) return false;

        if (CurrentPlayer.moveUsesRemaining[moveIndex] <= 0) return false;

        UseMove(CurrentPlayer, CurrentEnemy, moveIndex);

        // TODO: if enemy has no moves left, have them do nothing, struggle, etc. we'll figure that out later

        UseMove(CurrentEnemy, CurrentPlayer, Random.Range(0, 4));

        // === SWAP BEGINS HERE ===
        // Swap which battler is controlled by the character
        currentPlayerIndex = (currentPlayerIndex + 1) % battlers.Length;

        // may need to change this ode once more than 1 enemy is added
        platformRotationAnimator.SetBool("Player1OnRight", currentPlayerIndex == 1);
        battlerDisplaysAnimator.SetBool("Player1OnRight", currentPlayerIndex == 1);

        Refresh();

        return true;
    }

    void Refresh()
    {
        foreach (var moveButton in moveButtons)
        {
            if (moveButton) moveButton.Refresh();
        }

        foreach (var battlerDisplay in battlerDisplays)
        {
            if (battlerDisplay) battlerDisplay.Refresh();
        }
    }

    void UseMove(Battler attacker, Battler target, int moveIndex) {
        attacker.moveUsesRemaining[moveIndex]--;
        Move move = attacker.moves[moveIndex];
        Debug.Log($"{attacker.name} uses {move.name} on {target.name}");
        attacker.mp -= move.manaCost;
        target.hp -= move.damage;
    }
}
