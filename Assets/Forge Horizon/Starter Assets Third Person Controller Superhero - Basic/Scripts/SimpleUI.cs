using UnityEngine;
using UnityEngine.SceneManagement;

namespace ForgeHorizon
{
    public class SimpleUI : MonoBehaviour
    {
        public GameObject menuScreen;
        public GameObject pauseScreen;
        public GameObject controlScreen;
        public GameObject playerFollowCamera;
        public Transform player;

        private bool isPaused = false;

        private void Start()
        {
            playerFollowCamera.SetActive(false);    
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused)
                {
                    PlayGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }        

        public void PlayGame()
        {
            menuScreen.SetActive(false);
            pauseScreen.SetActive(false);
            controlScreen.SetActive(false);
            playerFollowCamera.SetActive(true);
            player.gameObject.SetActive(true);

            isPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
        }

        public void PauseGame()
        {
            menuScreen.SetActive(false);
            pauseScreen.SetActive(true);
            controlScreen.SetActive(false);
            player.gameObject.SetActive(false);
            playerFollowCamera.SetActive(false);

            isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
        }

        public void GameControl()
        {
            menuScreen.SetActive(false);
            pauseScreen.SetActive(false);
            controlScreen.SetActive(true);
        }

        public void BackGame()
        {
            menuScreen.SetActive(true);
            pauseScreen.SetActive(false);
            controlScreen.SetActive(false);
        }

        public void ResetGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
