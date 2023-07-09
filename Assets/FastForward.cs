using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FastForward : MonoBehaviour
{
    [SerializeField] Image image;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Space))
        {
            Time.timeScale = 3f;
            image.enabled = true;
        }
        else
        {
            Time.timeScale = 1f;
            image.enabled = false;
        }
    }
}
