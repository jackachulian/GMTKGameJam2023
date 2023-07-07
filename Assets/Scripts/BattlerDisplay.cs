using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The window at the top that displays the HP & MP of a battler.
/// </summary>
public class BattlerDisplay : MonoBehaviour
{
    /// <summary>
    /// True to show player side (left), false to show enemy side (right)
    /// </summary>
    public bool showPlayer;

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
        displayedBattler = showPlayer ? battleManager.CurrentPlayer : battleManager.CurrentEnemy;

        portraitImage.sprite = displayedBattler.portraitSprite;

        hpLabel.text = $"HP {displayedBattler.hp}/{displayedBattler.maxHp}";
        mpLabel.text = $"MP {displayedBattler.mp}/{displayedBattler.maxMp}";

        hpImage.fillAmount = displayedBattler.hp * 1f / displayedBattler.maxHp;
        mpImage.fillAmount = displayedBattler.mp * 1f / displayedBattler.maxMp;
    }
}