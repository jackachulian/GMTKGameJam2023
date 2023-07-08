using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PostgameManager : MonoBehaviour
{
    public Animator loseAnim, winAnim;

    public GameObject loseContainer, winContainer;

    public void Win()
    {
        // winAnim.SetTrigger("Win");
    }

    public void Lose()
    {
        loseAnim.SetTrigger("Lost");
    }

}
