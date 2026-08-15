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
    public Button stingButton;
    public Button tackleButton;

    public GameObject winPanel;
    public GameObject losePanel;


    [Header("Combat Stats")]
    public float playerMaxHealth = 100f;
    public float enemyMaxHealth = 100f;
    public float playerCurrentHealth = 100f;
    public float enemyCurrentHealth = 100f;
    public float playerDamage = 10f;
    public float enemyDamage = 10f;

    [Header("Visuals")]
    public float healthLerpSpeed = 1f;
    private float displayedPlayerHealth;
    private float displayedEnemyHealth;

    [Header("Script References")]
    public ScanningManager scanningManager;
    public CameraLockOn cameraLockOn;
    public PlayerInput playerInput;

    [Header("Bools")]
    private bool playerTurn;

    [Header("Animation IDs")]
    public Animator playerAnimator;
    public Animator enemyAnimator;
    private int _animIDFighting;
    private int _animIDSting;
    private int _animIDTackle;
    public float animationDuration = 1f;



    void Start()
    {
        playerTurn = true;
        battleArea = GetComponent<Collider>();
        playerSlider.maxValue = playerMaxHealth;
        enemySlider.maxValue = enemyMaxHealth;
        

        ResetCombatValues();

        _animIDFighting = Animator.StringToHash("Fighting");
        _animIDSting = Animator.StringToHash("Sting");
        _animIDTackle = Animator.StringToHash("Tackle");
    }

    private void Update()
    {
        LerpSliderValue();
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

            playerAnimator.SetBool(_animIDFighting, true);
            enemyAnimator.SetBool(_animIDFighting, true);

            battleArea.enabled = false; //turn off collider
            battleCanvas.SetActive(true); // turn on Combat UI
            playerInput.enabled = false; //turn off movement
            stingButton.interactable = true;
            playerTurn = true;
            StopAllCoroutines();
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

        playerAnimator.SetBool(_animIDFighting, false);
        enemyAnimator.SetBool(_animIDFighting, false);

        ResetCombatValues();
        battleArea.enabled = false;
        battleCanvas.SetActive(false);
        playerInput.enabled = true;
        cameraLockOn.enabled = true;

        yield return new WaitForSeconds(5f);
        battleArea.enabled = true;

        
    }

    public void ResetCombatValues()
    {
        
        playerSlider.value = 0;
        enemySlider.value = 0;

        playerCurrentHealth = 0;
        enemyCurrentHealth = 0;

        displayedPlayerHealth = playerCurrentHealth;
        displayedEnemyHealth = enemyCurrentHealth;

        battleEndText.text = "BATTLE START";
        winPanel.SetActive(false);
        losePanel.SetActive(false);
    }
    public IEnumerator DisableButton()
    {
        stingButton.interactable = false;
        tackleButton.interactable = false;
        yield return new WaitWhile(() => playerTurn);

        stingButton.interactable = true;
        tackleButton.interactable = true;
    }

    public void PlayerAttack()
    {
        if (!playerTurn)
            return;

        //StartCoroutine(PlayerAttackSequence());
    }

    public void PlayerSting()
    {
        if (!playerTurn)
            return;

        StartCoroutine(PlayerAttackSequence(_animIDSting));
    }

    public void PlayerTackle()
    {
        if (!playerTurn)
            return;

        StartCoroutine(PlayerAttackSequence(_animIDTackle));
    }

    private IEnumerator PlayerAttackSequence(int attackID)
    {
        playerTurn = false;
        stingButton.interactable = false;

        // Play player's attack animation
        yield return StartCoroutine(AnimatedAttack(true, attackID));

        // Damage enemy AFTER animation
        enemyCurrentHealth += playerDamage;

        DisplayDamage(true);

        checkHealthPoints();

        if (enemyCurrentHealth >= enemyMaxHealth)
            yield break;

        // Small pause before enemy reacts
        yield return new WaitForSeconds(0.5f);

        StartCoroutine(EnemyAttackSequence());
    }

    private IEnumerator EnemyAttackSequence()
    {
        int randomAttack = Random.Range(0, 2);
        int attackID;

        if (randomAttack == 0)
            attackID = _animIDSting;
        else
            attackID = _animIDTackle;
        // Play enemy attack animation
        yield return StartCoroutine(AnimatedAttack(false, attackID));

        // Damage player
        playerCurrentHealth += enemyDamage;

        DisplayDamage(false);

        checkHealthPoints();

        if (playerCurrentHealth >= playerMaxHealth)
            yield break;

        yield return new WaitForSeconds(0.5f);

        playerTurn = true;
        stingButton.interactable = true;
        tackleButton.interactable = true;
    }

    /*public void DoDamage(bool PlayerRequest)
    {
        if (!PlayerRequest)//Enemy Doing Damage
        {

            //UI update
            playerCurrentHealth += enemyDamage;
            //playerSlider.value = playerCurrentHealth;
           //playerSlider.value = Mathf.Lerp(playerSlider.value, playerCurrentHealth, .5); animate slider

            DisplayDamage(PlayerRequest);

            checkHealthPoints();

            playerTurn = true;

        }
        else//Player Doing Damage
        {
            //UI update
            enemyCurrentHealth += playerDamage;
            //enemySlider.value = enemyCurrentHealth; handled in LerpSliderValue()

            DisplayDamage(PlayerRequest);

            checkHealthPoints();

            StartCoroutine(DisableButton());

        }
    } not using DoDamage Anymore)*/ 

    public void DisplayDamage(bool PlayerRequest)
    {
        if (!PlayerRequest)//Enemy Doing Damage
        {
            //red damage display
            StartCoroutine(FlashDamage(playerRedImage));
        }
        else//Player Doing Damage
        {
            //red damage display
            StartCoroutine(FlashDamage(enemyRedImage));
        }
    }

    private IEnumerator FlashDamage(GameObject image)
    {
        image.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        image.SetActive(false);
    }

   /* private IEnumerator AnimatedAttack(bool playerAttack)
    {
        int anim = UnityEngine.Random.Range(0, 2) == 0
            ? _animIDSting
            : _animIDTackle;

        if (playerAttack)
        {
            playerAnimator.SetTrigger(anim);
            yield return new WaitForSeconds(animationDuration);
            //playerAnimator.SetTrigger(anim);
        }

        else
        {
            enemyAnimator.SetTrigger(anim);
            yield return new WaitForSeconds(animationDuration);
            //playerAnimator.SetTrigger(anim);
        }
            

        
    }*/

    private IEnumerator AnimatedAttack(bool playerAttack, int attackID)
    {
        Animator animator = playerAttack ? playerAnimator : enemyAnimator;

        animator.SetTrigger(attackID);

        // Wait until the animator leaves the attack state
        yield return null;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            yield return null;
        }

        /*while (animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            yield return null;
        }*/
        yield return new WaitForSeconds(.5f);


    }

    public void LerpSliderValue()
    {
        displayedPlayerHealth = Mathf.Lerp(
            displayedPlayerHealth,
            playerCurrentHealth,
            Time.deltaTime * healthLerpSpeed);

        displayedEnemyHealth = Mathf.Lerp(
            displayedEnemyHealth,
            enemyCurrentHealth,
            Time.deltaTime * healthLerpSpeed);

        playerSlider.value = displayedPlayerHealth;
        enemySlider.value = displayedEnemyHealth;
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
            //battleEndText.text = "YOU WIN";
            winPanel.SetActive(true);

            Debug.Log("Enemy Lost");
        }
        else
        {
            //battleEndText.text = "YOU LOSE";
            losePanel.SetActive(true);

            Debug.Log("Player Lost");
        }

        // Show the message for a second
        yield return new WaitForSeconds(4f);

        // Resume gameplay
        yield return StartCoroutine(ResumeGame());
    }

    
}
