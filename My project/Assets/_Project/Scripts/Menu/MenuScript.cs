using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{ 

    public void StartGame()
    {
        SceneManager.LoadScene("FirstEra");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

}