using System;
using UnityEngine;

[Serializable]
public class TutorialStep
{
    [Header("Step Information")]
    public string title;

    [TextArea(3, 8)]
    public string description;

    [Header("Behaviour")]
    public bool requiresManualContinue = true;
    public bool showContinueButton = true;

    [Header("Directional Arrow")]
    public bool showArrow = false;

    [Tooltip("Optional world object the arrow should follow.")]
    public Transform arrowWorldTarget;

    [Tooltip("Screen-space offset from the world target.")]
    public Vector2 arrowOffset;

    [Range(-360f, 360f)]
    public float arrowRotation = 0f;
}