using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputs : MonoBehaviour
    {
        [Header("Character Input Values")]
        public Vector2 move;
        public Vector2 look;

        public bool jump;
        public bool sprint;
        public bool crouch;
        public bool pause;

        [Header("Flying Input Values")]
        public bool fly;
        public bool ascend;
        public bool descend;

        [Header("Camera Lock On Input")]
        public bool lockOn;

        [Header("Movement Settings")]
        public bool analogMovement;
        

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
                LookInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnCrouch(InputValue value)
        {
            CrouchInput(value.isPressed);
        }

        // NEW

        public void OnFly(InputValue value)
        {
            FlyInput(value.isPressed);
        }

        public void OnAscend(InputValue value)
        {
            AscendInput(value.isPressed);
        }

        public void OnDescend(InputValue value)
        {
            DescendInput(value.isPressed);
        }

        public void OnLockOn(InputValue value)
        {
            LockOnInput(value.isPressed);
        }
        public void OnPause(InputValue value)
        {
           PauseInput(value.isPressed);
        }
       

#endif

        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        public void CrouchInput(bool newCrouchState)
        {
            crouch = newCrouchState;
        }

        // NEW

        public void FlyInput(bool newFlyState)
        {
            fly = newFlyState;
        }

        public void AscendInput(bool newAscendState)
        {
            ascend = newAscendState;
        }

        public void DescendInput(bool newDescendState)
        {
            descend = newDescendState;
        }

        private void LockOnInput(bool newLockOnState)
        {
            lockOn = newLockOnState;
        }

        private void PauseInput(bool newLockOnState)
        {
            pause = newLockOnState;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}