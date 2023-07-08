using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PostgameManager : MonoBehaviour
{
    public Animator loseAnim, winAnim;

    public GameObject loseContainer, winContainer;

    public BattleManager battleManager;

    public void Win()
    {
        winAnim.SetTrigger("Won");
    }

    public void Lose()
    {
        loseAnim.SetTrigger("Lost");
    }

    public void SelectRetry()
    {
        SceneManager.LoadScene("Battle");
    }
    
}
