using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinLoseBehavior : MonoBehaviour
{
    public int currentScans = 0;
    public int requiredScans = 5;
    public bool wonBattle = false;

    [Header("UI Reference")]
    public GameObject winPanel;
    public GameObject losePanel;

    void Start()
    {
    }

    void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
    }

    public void IncreaseScanCount()
    {
        currentScans++;

        if (currentScans >= requiredScans)
        {
            StartCoroutine(Win());
        }
    }

    public IEnumerator Win()
    {
        yield return new WaitForSeconds(3);


        Time.timeScale = 0f;
        winPanel.SetActive(true);
        EnableCursor();
    }

    public IEnumerator Lose()
    {
        yield return new WaitForSeconds(1);

        Time.timeScale = 0f;
        losePanel.SetActive(true);
        EnableCursor();
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }
}
