using UnityEngine;
using UnityEngine.InputSystem;

public class MapViewToggle : MonoBehaviour
{
    [Header("Objects To Toggle")]
    [SerializeField] private GameObject hexTiles;
    [SerializeField] private GameObject wasps;

    private bool mapOnlyMode = false;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            mapOnlyMode = !mapOnlyMode;

            if (hexTiles != null)
                hexTiles.SetActive(!mapOnlyMode);

            if (wasps != null)
                wasps.SetActive(!mapOnlyMode);
        }
    }
}