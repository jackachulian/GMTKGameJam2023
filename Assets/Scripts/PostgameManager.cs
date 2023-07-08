using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PostgameManager : MonoBehaviour
{
    public Animator loseAnim, winAnim;

    public GameObject loseContainer, winContainer;

    public BattleManager battleManager;

    public IEnumerator Win()
    {
        winAnim.SetTrigger("Won");

        yield return new WaitForSeconds(2.0f);

        // go to next level or end game screen, depending on level
        if (Storage.currentLevel >= battleManager.levelList.levels.Count)
        {
            Storage.currentLevel++;
            SceneManager.LoadScene("Battle");
        }
        else
        {
            Debug.Log("Game won");
        }
        
    }

    public IEnumerator Lose()
    {
        loseAnim.SetTrigger("Lost");

        yield return new WaitForSeconds(0.0f);
    }

    public void SelectRetry()
    {
        SceneManager.LoadScene("Battle");
    }
    
}
