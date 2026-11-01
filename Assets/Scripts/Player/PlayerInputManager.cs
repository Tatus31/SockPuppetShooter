using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Player {
    public class PlayerInputManager : BetterSingleton<PlayerInputManager> {
        const string MOVE_ACTION_STRING = "Move";
        const string JUMP_ACTION_STRING = "Jump";
        const string LOOK_ACTION_STRING = "Look";
        const string SPRINT_ACTION_STRING = "Sprint";
        const string INTERACT_ACTION_STRING = "Interact";
        const string CROUCH_ACTION_STRING = "Crouch";
        const string ATTACK_ACTION_STRING = "Attack";
        
        InputAction moveAction;
        InputAction jumpAction;
        InputAction lookAction;
        InputAction SprintAction;
        InputAction InteractAction;
        InputAction CrouchAction;
        InputAction AttackAction;
        
        public Vector2 GetLookDelta() => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        public Vector3 GetMoveVector() {
            var inVector2 = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            return new Vector3(inVector2.x, 0, inVector2.y);
        }
        
        public bool GetJumpActionPressedThisFrame() => jumpAction?.WasPressedThisFrame() ?? false;
        
        public bool GetInteractActionPressedThisFrame() => InteractAction?.WasPressedThisFrame() ?? false;
        
        public bool GetSprintActionPressedThisFrame() => SprintAction?.WasPressedThisFrame() ?? false;
        
        public bool GetCrouchActionPressedThisFrame() => CrouchAction?.WasPressedThisFrame() ?? false;
        
        public bool GetAttackActionPressedThisFrame() => AttackAction?.WasPressedThisFrame() ?? false;
        
        void Start() {
            InitInputActions();
        }
        
        void InitInputActions() {
            moveAction = InputSystem.actions.FindAction(MOVE_ACTION_STRING);
            jumpAction = InputSystem.actions.FindAction(JUMP_ACTION_STRING);
            lookAction = InputSystem.actions.FindAction(LOOK_ACTION_STRING);
            SprintAction = InputSystem.actions.FindAction(SPRINT_ACTION_STRING);
            InteractAction = InputSystem.actions.FindAction(INTERACT_ACTION_STRING);
            CrouchAction = InputSystem.actions.FindAction(CROUCH_ACTION_STRING);
            AttackAction = InputSystem.actions.FindAction(ATTACK_ACTION_STRING);
        }

    }
}
