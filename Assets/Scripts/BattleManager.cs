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
    public Transform moveButtonsTransform;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

        // Swap which battler is controlled by the character
        currentPlayerIndex = (currentPlayerIndex + 1) % battlers.Length;



        return true;
    }

    void UseMove(Battler attacker, Battler target, Move move) {
        attacker.mp -= move.manaCost;
        target.hp -= move.damage;
    }
}
