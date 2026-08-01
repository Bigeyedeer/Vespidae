using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public Collider battleArea;
    //public playerInput playerInput;
    public CameraLockOn cameraLockOn;
    public PlayerInput playerInput;

    public GameObject battleCanvas;
    public Slider playerSlider;
    public Slider enemySlider;
    public GameObject playerRedImage;
    public GameObject enemyRedImage;
    public TextMeshProUGUI battleEndText;

    public float playerMaxHealth = 100f;
    public float enemyMaxHealth = 100f;
    public float playerCurrentHealth = 100f;
    public float enemyCurrentHealth = 100f;
    public float playerDamage = 10f;
    public float enemyDamage = 10f;

    public ScanningManager scanningManager;
    

    void Start()
    {
        playerSlider.maxValue = playerMaxHealth;
        enemySlider.maxValue = enemyMaxHealth;
        

        ResetCombatValues();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Target"))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Visible: " + Cursor.visible);
            Debug.Log("Lock State: " + Cursor.lockState);

            battleArea.enabled = false; //turn off collider
            battleCanvas.SetActive(true); // turn on Combat UI
            playerInput.enabled = false; //turn off movement
            cameraLockOn.enabled = false; //turn off lock on
            scanningManager.CancelScan();
            //cameraLockOn.isLockedOn = false;

            
        }
    }

    public void StartResume()
    {
        StartCoroutine(ResumeGame());
    }

    public IEnumerator ResumeGame()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        ResetCombatValues();
        battleArea.enabled = false;
        battleCanvas.SetActive(false);
        playerInput.enabled = true;
        cameraLockOn.enabled = true;

        yield return new WaitForSeconds(3f);
        battleArea.enabled = true;
        
    }

    public void ResetCombatValues()
    {
        
        playerSlider.value = 0;
        enemySlider.value = 0;

        playerCurrentHealth = 0;
        enemyCurrentHealth = 0;

        battleEndText.text = "BATTLE START";
    }

    public void DoDamage(bool PlayerRequest)
    {
        if (!PlayerRequest)//Enemy Doing Damage
        {
            //UI update
            playerCurrentHealth += enemyDamage;
            playerSlider.value = playerCurrentHealth;

            checkHealthPoints();

            //rat mat
            playerRedImage.SetActive(true);
            playerRedImage.SetActive(false);

            //animations

        }
        else//Player Doing Damage
        {
            //UI update
            enemyCurrentHealth += playerDamage;
            enemySlider.value = enemyCurrentHealth; 

            checkHealthPoints();
            
            //red mat
            enemyRedImage.SetActive(true);
            enemyRedImage.SetActive(false);

            //animations
            Debug.Log("Player Did Damage");
        }
    }

    public void checkHealthPoints()
    {
        if (playerCurrentHealth >= playerMaxHealth)
        {
            battleEndText.text = "YOU LOSE";
            StartCoroutine(waitforSeconds(1f));
            StartResume();
            Debug.Log("Player Lost");
        }
        
        if (enemyCurrentHealth >= enemyMaxHealth)
        {
            battleEndText.text = "YOU WIN";
            StartCoroutine(waitforSeconds(1f));
            StartResume();
            Debug.Log("Enemy Lost");

        }
    }

    public IEnumerator waitforSeconds(float secs)
    {
        yield return new WaitForSeconds(secs);
    }
}
