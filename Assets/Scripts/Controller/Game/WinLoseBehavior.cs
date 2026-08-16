using NUnit.Framework;
using StarterAssets;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class WinLoseBehavior : MonoBehaviour
{
    public int currentScans = 0;
    public int requiredScans = 5;
    public bool wonBattle = false;

    [Header("UI Reference")]
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject pauseCanvas;
    private bool isPaused = false;
    public StarterAssetsInputs input;

    [Header("Audio")]
    public AudioSource playerSource;
    public AudioClip pageTurn;
    public AudioClip winAudio;
    public AudioClip loseAudio;
    public void OnPause()
    {
        Debug.Log("Pause button pressed");
        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pauseCanvas.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
            EnableCursor();
        else
            DisableCursor();
    }
    private void Update()
    {
        if (input.pause)
        {
            TogglePause();

            // Reset it so holding Escape doesn't repeatedly toggle
            input.pause = false;
        }
    }

    public void Resume()
    {
        pauseCanvas.SetActive(false);
        Time.timeScale = 1f;
        DisableCursor();

    }
    void EnableCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void DisableCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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

        playerSource.clip = winAudio;
        playerSource.Play();

        Time.timeScale = 0f;
        winPanel.SetActive(true);
        EnableCursor();
    }

    public IEnumerator Lose()
    {
        yield return new WaitForSeconds(1);

        playerSource.clip = loseAudio;
        playerSource.Play();

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

    public void AudioPageTurn()
    {
        playerSource.clip = pageTurn;
        playerSource.Play();
    }
}
