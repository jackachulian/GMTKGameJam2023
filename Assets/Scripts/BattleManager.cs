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
    public bool SubmitMove(Move move) {
        if (move.manaCost > CurrentPlayer.mp) return false;

        UseMove(CurrentPlayer, CurrentEnemy, move);

        UseMove(CurrentEnemy, CurrentPlayer, CurrentEnemy.moves[Random.Range(0, 4)]);

        // === SWAP BEGINS HERE ===
        Debug.Log("Swapping players!");
        // Swap which battler is controlled by the character
        currentPlayerIndex = (currentPlayerIndex + 1) % battlers.Length;

        Refresh();

        return true;
    }

    void Refresh()
    {
        foreach (var moveButton in moveButtons) moveButton.Refresh();

        foreach (var battlerDisplay in battlerDisplays)
        {
            if (battlerDisplay) battlerDisplay.Refresh();
        }
    }

    void UseMove(Battler attacker, Battler target, Move move) {
        Debug.Log($"{attacker.name} uses {move.name} on {target.name}");
        attacker.mp -= move.manaCost;
        target.hp -= move.damage;
    }
}
