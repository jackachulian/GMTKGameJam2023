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
    // gear icon next to selected button
    [SerializeField] GameObject cursor;

    [SerializeField] AudioClip bgm;

    void Start()
    {
        SoundManager.Instance.SetBGM(bgm);
        SoundManager.Instance.PlayBGM();
    }
    public void SelectStart()
    {
        SoundManager.Instance.StopBGM();
        SceneManager.LoadScene("Battle");
    }

    public void SelectAbout()
    {
        ToggleWindows();
        // EventSystem.current.SetSelectedGameObject(aboutCloseButton);
    }

    public void CloseAbout()
    {
        ToggleWindows();
        // EventSystem.current.SetSelectedGameObject(aboutButton);
    }

    public void ToggleWindows()
    {
        mainWindow.SetActive(!mainWindow.activeSelf);
        aboutWindow.SetActive(!aboutWindow.activeSelf);
    }

    public void UpdateCursor(GameObject hovered)
    {
        // move cursor to button y value
        cursor.SetActive(true);
        cursor.transform.position = new Vector2(cursor.transform.position.x, hovered.transform.position.y);
    }
}
