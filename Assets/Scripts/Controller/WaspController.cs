using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public class WaspController : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Move speed of the character in m/s")]
    public float MoveSpeed = 2.0f;

    [Tooltip("Sprint speed of the character in m/s")]
    public float SprintSpeed = 5.335f;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("How fast the character flies up")]
    public float FlyUpSpeed = 3f;

    [Tooltip("How fast the character flies down")]
    public float FlyDownSpeed = 3f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [Tooltip("What layers the character uses as ground")]
    public LayerMask GroundLayers;

    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;

    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;

    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
    public float CameraAngleOverride = 0.0f;

    [Tooltip("For locking the camera position on all axis")]
    public bool LockCameraPosition = false;

    // cinemachine
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    // player
    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    private PlayerInput _playerInput;
   // private Animator _animator;
    private CharacterController _controller;
    private GameObject _mainCamera;
    private InputActionAsset _input;

    [Header("Character Input Values")]
    public Vector2 m_move;
    public Vector2 m_look;
    public InputAction m_FlyUp;
    //public InputAction m_sprint;
    public InputAction m_FlyDown;

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;


    private void Awake()
    {
        // get a reference to our main camera
        if (_mainCamera == null)
        {
            _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        }
    }

    void Start()
    {
        _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

        //_hasAnimator = TryGetComponent(out _animator);
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
    }

    void Update()
    {
        //FlyUp();
       // FlyDown();
        //GroundedCheck();
        //Move();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
            transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
            QueryTriggerInteraction.Ignore);

        // update animator if using character
        /*if (_hasAnimator)
        {
            _animator.SetBool(_animIDGrounded, Grounded);
        }*/
    }

    private void CameraRotation()
    {
        /* _input.
         // if there is an input and camera position is not fixed
         if (_input.Look.sqrMagnitude >= _threshold && !LockCameraPosition)
         {
             //Don't multiply mouse input by Time.deltaTime;
             float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

             _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
             _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
         }

         // clamp our rotations so our values are limited 360 degrees
         _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
         _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

         // Cinemachine will follow this target
         CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
             _cinemachineTargetYaw, 0.0f);
     }

     private void Move()
     {
         // set target speed based on move speed, sprint speed and if sprint is pressed
         float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

         // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

         // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
         // if there is no input, set the target speed to 0
         if (_input.move == Vector2.zero) targetSpeed = 0.0f;

         // a reference to the players current horizontal velocity
         float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

         float speedOffset = 0.1f;
         float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

         // accelerate or decelerate to target speed
         if (currentHorizontalSpeed < targetSpeed - speedOffset ||
             currentHorizontalSpeed > targetSpeed + speedOffset)
         {
             // creates curved result rather than a linear one giving a more organic speed change
             // note T in Lerp is clamped, so we don't need to clamp our speed
             _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                 Time.deltaTime * SpeedChangeRate);

             // round speed to 3 decimal places
             _speed = Mathf.Round(_speed * 1000f) / 1000f;
         }
         else
         {
             _speed = targetSpeed;
         }

         _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
         if (_animationBlend < 0.01f) _animationBlend = 0f;

         // normalise input direction
         Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

         // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
         // if there is a move input rotate player when the player is moving
         if (_input.move != Vector2.zero)
         {
             _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                               _mainCamera.transform.eulerAngles.y;
             float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                 RotationSmoothTime);

             // rotate to face input direction relative to camera position
             transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
         }


         Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

         // move the player
         _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                          new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

         // update animator if using character
         if (_hasAnimator)
         {
             _animator.SetFloat(_animIDSpeed, _animationBlend);
             _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
         }
        */
    }

}
