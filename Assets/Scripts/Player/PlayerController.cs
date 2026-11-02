using System;
using System.Collections;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour {
        [Header("Refs")]
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] Transform CameraTransform;
        [SerializeField] CapsuleCollider playerCollider;
        
        [Header("Settings")]
        [SerializeField] bool drawGizmos = true;
        
        PlayerInputManager playerInputManager;

        float xRotation;
        Vector2 currentMouseDelta;
        Vector3 moveDirection;
        
        bool jumpActionPressed;
        bool jumpActionReleased;
        bool isJumping;
        float jumpHoldTimer;
        
        float maxJumpHoldTime = 0.3f;
        float jumpHoldForce = 40f;
        float jumpInitialForce = 300f;
        
        void Start() {
            playerInputManager = PlayerInputManager.TryGetInstance();
        }

        void Update() {
            UpdatePlayerInput();
        }

        void FixedUpdate() {
            Move();
            Jump();
            HandleGrounded();
        }
        void LateUpdate() {
            Look();
        }

        void Look() {
            var mouseX = currentMouseDelta.x * 0.5f;
            var mouseY = currentMouseDelta.y * 0.5f;

            var rot = CameraTransform.transform.localRotation.eulerAngles;
            var yRotation = rot.y + mouseX;
            
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            CameraTransform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        void UpdatePlayerInput() {
            currentMouseDelta = playerInputManager.GetLookDelta();
            moveDirection = GetMoveDirection();
            jumpActionPressed = playerInputManager.GetJumpActionPressedThisFrame();
            jumpActionReleased = playerInputManager.GetJumpActionReleasedThisFrame();
            HandleJumpActionState();
        }

        void HandleJumpActionState() {
            if (jumpActionPressed && IsGrounded() && !isJumping) {
                isJumping = true;
                jumpHoldTimer = 0f;
                
                playerRigidbody.AddForce(Vector3.up * jumpInitialForce, ForceMode.Impulse);
            }
        }
        
        Vector3 GetMoveDirection() {
            var forward = CameraTransform.forward;
            var right = CameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            var input = playerInputManager.GetMoveVector();
            return (right * input.x + forward * input.z).normalized;
        }
        
        void Move() {
            playerRigidbody.linearVelocity = new Vector3(GetCurrentHorizontalVelocity().x, playerRigidbody.linearVelocity.y, GetCurrentHorizontalVelocity().z);
            TryStep(moveDirection);

            //Horizontal only so TryStep can deal with small bumps in geometry
            Vector3 GetCurrentHorizontalVelocity() {
                float moveSpeed = 10f;
                float acceleration = 25f;
                
                var currentVelocity = playerRigidbody.linearVelocity;
                var horizontalVelocity = new Vector3( currentVelocity.x, 0f, currentVelocity.z );
                var targetVelocity = moveDirection * moveSpeed;
            
                return Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            
            void TryStep(Vector3 direction) {
                if (direction == Vector3.zero)
                    return;

                //So we dont start checking underground
                const float LOWER_ORIGIN_MINIMUM_HEIGHT = 0.05f;
                
                var stepHeight = 0.3f;
                var stepCheckDistance  = 0.4f;
                
                var lowerOrigin = GetPlayerFeetPosition() + Vector3.up * LOWER_ORIGIN_MINIMUM_HEIGHT;
                var upperOrigin = GetPlayerFeetPosition() + Vector3.up * stepHeight;

                var lowerHit = Physics.Raycast(lowerOrigin, direction, stepCheckDistance);
                var upperHit = Physics.Raycast(upperOrigin, direction, stepCheckDistance);
                if (lowerHit && !upperHit)
                    playerRigidbody.position += Vector3.up * (stepHeight * 5f * Time.fixedDeltaTime);
            }
        }
        
        void Jump() {
            if (!isJumping || jumpActionReleased)
                return;

            if (jumpHoldTimer >= maxJumpHoldTime)
                return;

            playerRigidbody.AddForce(Vector3.up * jumpHoldForce, ForceMode.Acceleration);
            jumpHoldTimer += Time.fixedDeltaTime;
        }
        
        bool IsGrounded() {
            if (playerRigidbody.linearVelocity.y > 0.1f)
                return false;

            var sphereCastRadius = playerCollider.radius / 2f;
            const float groundCheckMaxDistance = 0.35f;
            return Physics.SphereCast(GetPlayerFeetPosition(), sphereCastRadius,
                Vector3.down, out _, groundCheckMaxDistance);
        }
        
        void HandleGrounded() {
            if (playerRigidbody.linearVelocity.y <= 0f && IsGrounded()) 
                isJumping = false;
        }

        Vector3 GetPlayerFeetPosition() {
            return playerRigidbody.position + Vector3.up * (playerCollider.radius);
        }
        
        void OnDrawGizmosSelected() {
            if (!drawGizmos)
                return;
            
            Gizmos.color = Color.blue;
            
            const float lineLength = 5f;
            const float lineHeight = 1.25f;

            Vector3 lineStart = transform.position + Vector3.up * lineHeight;
            Vector3 lineEnd = lineStart + transform.forward * lineLength;
            Gizmos.DrawLine(lineStart, lineEnd);
        }
    }
}
