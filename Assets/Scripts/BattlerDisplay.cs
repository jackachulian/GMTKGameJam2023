using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The window at the top that displays the HP & MP of a battler.
/// </summary>
public class BattlerDisplay : MonoBehaviour
{
    /// <summary>
    /// Index of player to initially show.
    /// Will be offset by the current player index of battle manager.
    /// </summary>
    public int playerIndex;

    private BattleManager battleManager;

    [SerializeField] private TMPro.TextMeshProUGUI hpLabel, mpLabel;

    [SerializeField] private Image portraitImage, hpImage, mpImage;

    private Battler displayedBattler;

    void OnValidate()
    {
        battleManager = FindObjectOfType<BattleManager>();
        Refresh();
    }

    public void Refresh()
    {
        if (!battleManager || battleManager.battlers == null ||battleManager.battlers.Length <= 0) return;
        // displayedBattler = battleManager.battlers[(battleManager.currentPlayerIndex + playerIndex) % battleManager.battlers.Length];
        displayedBattler = battleManager.battlers[playerIndex];

        portraitImage.sprite = displayedBattler.portraitSprite;

        hpLabel.text = $"HP {displayedBattler.hp}/{displayedBattler.maxHp}";
        mpLabel.text = $"MP {displayedBattler.mp}/{displayedBattler.maxMp}";

        hpImage.fillAmount = displayedBattler.hp * 1f / displayedBattler.maxHp;
        mpImage.fillAmount = displayedBattler.mp * 1f / displayedBattler.maxMp;
    }
}