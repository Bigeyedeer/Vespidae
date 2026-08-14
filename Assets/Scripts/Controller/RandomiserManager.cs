using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RandomiserManager : MonoBehaviour
{
    [Header("Wasps")]
    public List<GameObject> waspEnemiesList;
    public List<GameObject> spawnPointList;

    [Header("Other")]
    public List<GameObject> greeneryList;
    public Transform centerPos;
    

    public float minValue = 0f;
    public float maxValue = 360f;

    private void Start()
    {
        WaspRandomise();
        randomiseGreenery();
    }
    void WaspRandomise()
    {
        // Turn everything off first
        foreach (GameObject obj in waspEnemiesList)
        {
            obj.SetActive(false);
        }

        //Generate a number for each list
        int randomIntWasp = Random.Range(0, waspEnemiesList.Count);
        int randomIntSpawn = Random.Range(0, spawnPointList.Count);

        //Generate a Y rotation value
        float randomRotation = Random.Range(0, 360);
        Vector3 rotation = waspEnemiesList[randomIntWasp].transform.eulerAngles;
        rotation.y = randomRotation;

        //Set Wasp spawn to random location
        waspEnemiesList[randomIntWasp].transform.position = spawnPointList[randomIntSpawn].transform.position;

        //Change Y Rotation
        waspEnemiesList[randomIntWasp].transform.eulerAngles = rotation;

        //Enable Wasp
        waspEnemiesList[randomIntWasp].SetActive(true);
    }

    void randomiseGreenery()
    {
        /*foreach (GameObject obj in greeneryList)
        {
            obj.transform.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90, 0f); //rotate parent

            foreach (Transform child in obj.transform)
            {
                child.localRotation = Quaternion.Euler(0f, Random.Range(0, 4) * 90, 0f); //rotate children
            }
        } my attempt, doesnt work because the gameobjects centers aren't at 0,0,0*/

        foreach (GameObject obj in greeneryList)
        {
            int randomRotation = Random.Range(0, 4) * 90;

            obj.transform.RotateAround(
                centerPos.position,   // center of your square
                Vector3.up,     // rotate around Y axis
                randomRotation
            );

            // Optional: rotate individual rocks/grass
            foreach (Transform child in obj.transform)
            {
                int childRotation = Random.Range(0, 4) * 90;
                Vector3 originalRotation = child.localEulerAngles;

                child.localRotation = Quaternion.Euler(
                    originalRotation.x,
                    childRotation,
                    originalRotation.z
                );
            }
        }
    }

    void Update()
    {
        
    }
}
