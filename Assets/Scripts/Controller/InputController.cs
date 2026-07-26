using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public InputActionAsset InputActions;

    private InputAction m_moveAction;
    private InputAction m_lookAction;
    private InputAction m_flyUpAction;
    private InputAction m_flyDownAction;
    private InputAction m_pauseActionPlayer;
    private InputAction m_pauseActionUI;


    private Vector2 m_moveAmount;
    private Vector2 m_lookAmount;
    private Animator m_animator;
    private Rigidbody m_rigidbody;

    public float MoveSpeed = 5f;
    public float RotateSpeed = 5f;
    public float UpSpeed = 1.0f;
    public float DownSpeed = 1.0f;

    public GameObject PauseDisplay;

    private void Awake()
    {
        m_moveAction = InputSystem.actions.FindAction("Move");
        m_lookAction = InputSystem.actions.FindAction("Look");
        m_flyUpAction = InputSystem.actions.FindAction("Jump");
        m_flyDownAction= InputSystem.actions.FindAction("Crouch");

        m_pauseActionPlayer = InputSystem.actions.FindAction("Player/Pause");
        m_pauseActionUI = InputSystem.actions.FindAction("UI/Pause");

        m_animator = GetComponent<Animator>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        m_moveAmount = m_moveAction.ReadValue<Vector2>();
        m_lookAmount = m_moveAction.ReadValue<Vector2>();

        if (m_flyUpAction.IsPressed()) 
        {
            FlyUp();
        }

        DisplayPause();
    }

    public void FlyUp()
    {
        m_rigidbody.AddForceAtPosition(new Vector3(0,5f,0), Vector3.up, ForceMode.Impulse);
        m_animator.SetTrigger("FlyUp");
    }

    public void FlyDown()
    {
        m_rigidbody.AddForceAtPosition(new Vector3(0, 5f, 0), Vector3.down, ForceMode.Impulse);
        m_animator.SetTrigger("FlyDown");
    }

    private void FixedUpdate()
    {
        Movement();
        Rotating();
    }

    private void Movement()
    {
        m_animator.SetFloat("Speed", m_moveAmount.y);
        m_rigidbody.MovePosition(m_rigidbody.position + transform.forward * m_moveAmount.y * MoveSpeed *  Time.deltaTime);
    }

    private void Rotating()
    {
        if (m_moveAmount.y > 0) 
        {
            float rotationAmount = m_lookAmount.x * RotateSpeed *Time.deltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0, rotationAmount, 0);
            m_rigidbody.MoveRotation(m_rigidbody.rotation * deltaRotation);
        }
    }

    private void DisplayPause()
    {
        if (m_pauseActionPlayer.WasPressedThisFrame())
        {
            PauseDisplay.SetActive(true);
            InputActions.FindActionMap("Player").Disable();
            InputActions.FindActionMap("UI").Enable();
        }
        else if (m_pauseActionUI.WasPressedThisFrame())
        {
            PauseDisplay.SetActive(false);
            InputActions.FindActionMap("UI").Disable();
            InputActions.FindActionMap("Player").Enable();
        }
    }
}
