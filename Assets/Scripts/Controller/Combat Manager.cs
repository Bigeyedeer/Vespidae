using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    public Collider battleArea;
    public CharacterController characterController;
    public CameraLockOn cameraLockOn;

    public GameObject battleCanvas;
    public Slider playerSlider;
    public Slider enemySlider;
    public float playerMaxHealth = 100f;
    public float enemyMaxHealth = 100f;
    public float playerCurrentHealth = 100f;
    public float enemyCurrentHealth = 100f;
    public float playerDamage = 10f;
    public float enemyDamage = 10f;

    void Start()
    {
        playerSlider.maxValue = playerMaxHealth;
        enemySlider.maxValue = enemyMaxHealth;

        ResetCombatValues();
    }

    public void OnColliderEnter(Collider other)
    {
        if (other == battleArea)
        {
            battleArea.enabled = false; //turn off collider
            battleCanvas.SetActive(true); // turn on Combat UI
            characterController.enabled = false; //turn off movement
            cameraLockOn.enabled = false; //turn off lock on
        }
    }

    public IEnumerator ResumeGame()
    {
        ResetCombatValues();
        battleArea.enabled = false;
        battleCanvas.SetActive(false);
        characterController.enabled = true;
        cameraLockOn.enabled = true;

        yield return new WaitForSeconds(3f);
        battleArea.enabled = true;
    }

    public void ResetCombatValues()
    {
        
        playerSlider.value = playerMaxHealth;
        enemySlider.value = enemyMaxHealth;

        playerCurrentHealth = playerMaxHealth;
        enemyCurrentHealth = enemyMaxHealth;
    }

    public void DoDamage(bool PlayerRequest)
    {
        if (!PlayerRequest)//Enemy Doing Damage
        {
            playerCurrentHealth =- enemyDamage;
            playerSlider.value = playerCurrentHealth;
            checkHealthPoints();
            //play animation
            //red material
        }
        else//Player Doing Damage
        {
            enemyCurrentHealth =- playerDamage;
            enemySlider.value = enemyCurrentHealth; 
            checkHealthPoints();
            //play animation
            //red material
        }
    }

    public void checkHealthPoints()
    {
        if (playerCurrentHealth <= 0 || enemyCurrentHealth <= 0)
        {
            ResumeGame();
        }
    }
}
