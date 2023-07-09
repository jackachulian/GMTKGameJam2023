using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndScreen : MonoBehaviour
{
    [SerializeField] AudioClip winBGM;
    public void SelectReturn()
    {
        SceneManager.LoadScene("MainMenu");
    }

    void Start()
    {
        SoundManager.Instance.SetBGM(winBGM);
        SoundManager.Instance.PlayBGM();
    }
}
