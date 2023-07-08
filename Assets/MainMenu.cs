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

    public void UpdateCursor()
    {
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        // move cursor to button y value
        cursor.transform.position = new Vector2(cursor.transform.position.x, selected.transform.position.y);
        Debug.Log("cursor function call");
    }
}
