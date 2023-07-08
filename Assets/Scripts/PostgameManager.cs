using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PostgameManager : MonoBehaviour
{
    public Animator loseAnim, winAnim;

    [SerializeField] private AudioClip winSFX, loseSFX;

    public BattleManager battleManager;

    public void Win()
    {
        winAnim.SetTrigger("Won");
        SoundManager.Instance.PlaySound(winSFX);
    }

    public void Lose()
    {
        loseAnim.SetTrigger("Lost");
        SoundManager.Instance.PlaySound(loseSFX);
    }

    public void SelectRetry()
    {
        SceneManager.LoadScene("Battle");
    }
    
}
