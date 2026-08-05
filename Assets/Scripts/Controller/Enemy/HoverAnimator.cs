using UnityEngine;

public class HoverAnimator : MonoBehaviour
{
    public Animator _animator;
    private int _animIDFloat;
    void Start()
    {
        _animIDFloat = Animator.StringToHash("Float");
        _animator.SetBool(_animIDFloat, true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
