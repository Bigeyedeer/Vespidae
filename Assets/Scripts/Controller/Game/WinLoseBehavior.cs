using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinLoseBehavior : MonoBehaviour
{
    public List<ScannableObject> requiredScanList;
    public int currentScans = 0;
    public int requiredScans;
    public bool wonBattle = false;

    [Header("UI Reference")]
    public GameObject winPanel;
    public GameObject losePanel;

    void Start()
    {
        requiredScans = requiredScanList.Count;
    }

    void Update()
    {
        
    }

    public void CheckCondition()
    {
        if (currentScans == requiredScans || wonBattle)
        {
            Win();
        }

        if (!wonBattle)
        {
            Lose();
        }
    }

    public void Win()
    {
        Time.timeScale = 0f;
        winPanel.SetActive(true);
    }

    public void Lose()
    {
        Time.timeScale = 0f;
        losePanel.SetActive(true);
    }

    public void Restart()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
