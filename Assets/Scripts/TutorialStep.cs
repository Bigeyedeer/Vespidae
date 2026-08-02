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
}