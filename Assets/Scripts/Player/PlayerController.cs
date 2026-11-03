using System;
using System.Collections;
using Player.Data;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour {
        //So we dont start checking underground
        const float LOWER_ORIGIN_MINIMUM_HEIGHT = 0.05f;
        
        [Header("Refs and Data")]
        [SerializeField] PlayerData playerData;
        [Space(10)]
        [SerializeField] Transform CameraTransform;
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] CapsuleCollider playerCollider;
        
        [Header("Settings")]
        [SerializeField] bool drawGizmos = true;
        
        PlayerInputManager playerInputManager;

        float xRotation;
        Vector2 currentMouseDelta;
        Vector3 moveDirection;
        
        bool jumpActionPressed;
        bool jumpActionReleased;
        bool canJump;
        
        Collider[] groundedCollisionResults;

        float GetSphereCastRadius => playerCollider ? playerCollider.radius / 2f : 0;

        void Start() {
            playerInputManager = PlayerInputManager.TryGetInstance();
        }

        void Update() {
            UpdatePlayerInput();
        }

        void FixedUpdate() {
            Move();
            Jump();
            IsGrounded();
        }
        void LateUpdate() {
            Look();
        }

        void Look() {
            var mouseX = currentMouseDelta.x * playerData.mouseSensitivityX;
            var mouseY = currentMouseDelta.y * playerData.mouseSensitivityY;

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
            canJump = IsGrounded();
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
                var moveSpeed = playerData.moveSpeed;
                var acceleration = playerData.acceleration;
                
                var currentVelocity = playerRigidbody.linearVelocity;
                var horizontalVelocity = new Vector3( currentVelocity.x, 0f, currentVelocity.z );
                var targetVelocity = moveDirection * moveSpeed;
            
                return Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            }
            
            void TryStep(Vector3 direction) {
                if (direction == Vector3.zero)
                    return;
                
                var stepHeight = playerData.stepHeight;
                var stepCheckDistance  = playerData.stepCheckDistance;
                
                var lowerOrigin = GetPlayerFeetPosition() + Vector3.up * LOWER_ORIGIN_MINIMUM_HEIGHT;
                var upperOrigin = GetPlayerFeetPosition() + Vector3.up * stepHeight;

                var lowerHit = Physics.Raycast(lowerOrigin, direction, stepCheckDistance);
                var upperHit = Physics.Raycast(upperOrigin, direction, stepCheckDistance);
                if (lowerHit && !upperHit)
                    playerRigidbody.position += Vector3.up * (stepHeight * playerData.stepClimbSpeed * Time.fixedDeltaTime);
            }
        }
        
        void Jump() {
            if(!canJump)
                return;
            
            if (jumpActionReleased)
                playerRigidbody.AddForce(transform.up * CalculateJumpPower(), ForceMode.Impulse);
        }
        
        float CalculateJumpPower() {
            var power = playerData.jumpPower;
            if (IsGrounded()) 
                power -= playerRigidbody.linearVelocity.y;
            
            return power;
        }
        
        int GetGroundedSphereCast() {
            if (playerRigidbody.linearVelocity.y > 0.1f)
                return 0;

            var sphereCastRadius = GetSphereCastRadius;
            return Physics.OverlapSphereNonAlloc(GetPlayerFeetPosition(), sphereCastRadius, groundedCollisionResults);
        }
        
        bool IsGrounded() => GetGroundedSphereCast() >= 1 && playerRigidbody.linearVelocity.y <= 0f ? true : false;
        
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
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetPlayerFeetPosition(), GetSphereCastRadius);
        }
    }
}
