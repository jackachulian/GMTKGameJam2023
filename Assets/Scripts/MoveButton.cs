using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class MoveButton : MonoBehaviour, IPointerClickHandler
{
    private BattleManager battleManager;

    public int moveIndex;

    /// <summary>
    /// Move that is displayed as of the last refresh, which is called when swapping battlers.
    /// </summary>
    private Move displayedMove;


    [SerializeField] private TMPro.TextMeshProUGUI usesLabel, nameLabel;

    // Use this for initialization
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnValidate()
    {
        battleManager = FindObjectOfType<BattleManager>();
    }

    // Re-display info about the player's current battler's move at this MoveButton's index.
    public void Refresh()
    {
        displayedMove = battleManager.CurrentPlayer.moves[moveIndex];
        usesLabel.text = battleManager.CurrentPlayer.moveUsesRemaining[moveIndex]+"";
        nameLabel.text = displayedMove.name;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        battleManager.SubmitMove(moveIndex);
    }
}