#region

using UnityEngine;
using UnityEngine.InputSystem;

#endregion

namespace InputSystem
{
    public class TunnelsInputs : MonoBehaviour
    {
        //TODO: Figure out ow to use inputs from different objects
        // THis might be of use:
        // https://github.com/UnityTechnologies/open-project-1/tree/devlogs/3-input

        [Header("Character Input Values")] 
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;
        public bool flashlight;
        public bool interact;

        [Header("Movement Settings")] 
        public bool analogMovement;
        
        [Header("Mouse Cursor Settings")] 
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook) LookInput(value.Get<Vector2>());
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }

        public void OnFlashlight(InputValue value)
        {
            FlashlightInput(value.isPressed);
        }

        public void OnInteract(InputValue value)
        {
            InteractInput(value.isPressed);
        }

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

        public void FlashlightInput(bool newFlashlightState)
        {
            if (!newFlashlightState) return;
            flashlight ^= true;
        }

        public void InteractInput(bool newInteractState)
        {
            interact = newInteractState;
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}