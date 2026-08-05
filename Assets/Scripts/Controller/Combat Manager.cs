using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Splines.Interpolators;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [Header("Collider")]
    public Collider battleArea;
    //public playerInput playerInput;
   

    [Header("UI Linking")]
    public GameObject battleCanvas;
    public Slider playerSlider;
    public Slider enemySlider;
    public GameObject playerRedImage;
    public GameObject enemyRedImage;
    public TextMeshProUGUI battleEndText;


    [Header("Combat Stats")]
    public float playerMaxHealth = 100f;
    public float enemyMaxHealth = 100f;
    public float playerCurrentHealth = 100f;
    public float enemyCurrentHealth = 100f;
    public float playerDamage = 10f;
    public float enemyDamage = 10f;

    [Header("Script References")]
    public ScanningManager scanningManager;
    public CameraLockOn cameraLockOn;
    public PlayerInput playerInput;

    [Header("Bools")]
    private bool playerTurn;



    void Start()
    {
        playerTurn = true;
        battleArea = this.GetComponent<Collider>();
        playerSlider.maxValue = playerMaxHealth;
        enemySlider.maxValue = enemyMaxHealth;
        

        ResetCombatValues();
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            Debug.Log("Visible: " + Cursor.visible);
            Debug.Log("Lock State: " + Cursor.lockState);

            cameraLockOn.ForceUnlock();

            battleArea.enabled = false; //turn off collider
            battleCanvas.SetActive(true); // turn on Combat UI
            playerInput.enabled = false; //turn off movement
            //cameraLockOn.enabled = false; //turn off lock on

            //scanningManager.CancelScan();
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
           //playerSlider.value = Mathf.Lerp(playerSlider.value, playerCurrentHealth, .5f); animate slider

            DisplayDamage(PlayerRequest);

            checkHealthPoints();

        }
        else//Player Doing Damage
        {
            //UI update
            enemyCurrentHealth += playerDamage;
            enemySlider.value = enemyCurrentHealth; 

            DisplayDamage(PlayerRequest);

            checkHealthPoints();

        }
    }

    public void DisplayDamage(bool PlayerRequest)
    {
        if (!PlayerRequest)//Enemy Doing Damage
        {
            //red damage display
            StartCoroutine(FlashDamage(playerRedImage));
            
            //animations

        }
        else//Player Doing Damage
        {
            //red mat
            StartCoroutine(FlashDamage(enemyRedImage));

            //animations
        }
    }

    private IEnumerator FlashDamage(GameObject image)
    {
        image.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        image.SetActive(false);
    }

    public void checkHealthPoints()
    {
        if (playerCurrentHealth >= playerMaxHealth)
        {
            StartCoroutine(EndBattle(false));
        }
        
        if (enemyCurrentHealth >= enemyMaxHealth)
        {
            StartCoroutine(EndBattle(true));
        }
    }

    private IEnumerator EndBattle(bool playerWon)
    {
        if (playerWon)
        {
            battleEndText.text = "YOU WIN";
            Debug.Log("Enemy Lost");
        }
        else
        {
            battleEndText.text = "YOU LOSE";
            Debug.Log("Player Lost");
        }

        // Show the message for a second
        yield return new WaitForSeconds(1f);

        // Resume gameplay
        yield return StartCoroutine(ResumeGame());
    }

    
}
