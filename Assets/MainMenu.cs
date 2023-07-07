using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void SelectStart()
    {
        SceneManager.LoadScene("Battle");
    }

    public void SelectAbout()
    {
        // TODO implement
    }
}
