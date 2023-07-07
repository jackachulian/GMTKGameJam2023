using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MainMenu : MonoBehaviour
{
    [SerializeField] GameObject mainWindow;
    [SerializeField] GameObject aboutWindow;
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject aboutButton;
    [SerializeField] GameObject aboutCloseButton;

    public void SelectStart()
    {
        SceneManager.LoadScene("Battle");
    }

    public void SelectAbout()
    {
        ToggleWindows();
        EventSystem.current.SetSelectedGameObject(aboutCloseButton);
    }

    public void CloseAbout()
    {
        ToggleWindows();
        EventSystem.current.SetSelectedGameObject(aboutButton);
    }

    public void ToggleWindows()
    {
        mainWindow.SetActive(!mainWindow.activeSelf);
        aboutWindow.SetActive(!aboutWindow.activeSelf);
    }
}
