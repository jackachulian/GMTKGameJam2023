using UnityEngine;

public class MovesGrid : MonoBehaviour
{
    public BattleManager battleManager;

    public void Refresh()
    {
        battleManager.Refresh();
    }
}