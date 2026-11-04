using System;
using System.Collections;
using DG.Tweening;
using Player.Data;
using Unity.U2D.Physics;
using UnityEngine;

namespace Player {
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour {
        //So we dont start checking underground
        const float LOWER_ORIGIN_MINIMUM_HEIGHT = 0.15f;
        
        [Header("Refs and Data")]
        [SerializeField] PlayerData playerData;
        [Space(10)]
        [SerializeField] Transform cameraTransform;
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] CapsuleCollider playerCollider;
        [SerializeField] LayerMask ignoredLayer;
        
        [Header("Settings")]
        [SerializeField] bool drawGizmos = true;
        
        PlayerInputManager playerInputManager;

        RaycastHit groundHit;

        float xRotation;
        float jumpElapsedTime;
        float initialSlideForce;
        float slideElapsedTime;
        
        Vector2 currentMouseDelta;
        Vector3 moveDirection;
        Vector3 defaultCameraPosition;
        
        bool jumpActionPressed;
        bool jumpActionReleased;
        bool canJump;
        bool isJumping;
        bool isJumpQueued;
        bool isSliding;
        bool isSlideActionHeld;

        readonly Collider[] groundedCollisionResults = new Collider[16];

        float GetSphereCastRadius => playerCollider ? playerCollider.radius / 2f : 1f;

        void OnValidate() {
            initialSlideForce = playerData.slideForce;
        }

        void Start() {
            defaultCameraPosition = cameraTransform.localPosition;
            playerInputManager = PlayerInputManager.TryGetInstance();
        }

        void Update() {
            UpdatePlayerInput();
        }

        void FixedUpdate() {
            Move();
            Jump();
            ApplyGravity();
            ApplyFriction();
            ApplyJumpCutOffGravity();
            HandleSliding();
        }
        void LateUpdate() {
            Look();
        }

        void Look() {
            var mouseX = currentMouseDelta.x * playerData.mouseSensitivityX;
            var mouseY = currentMouseDelta.y * playerData.mouseSensitivityY;

            var rot = cameraTransform.transform.localRotation.eulerAngles;
            var yRotation = rot.y + mouseX;
            
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        void UpdatePlayerInput() {
            currentMouseDelta = playerInputManager.GetLookDelta();
            moveDirection = IsOnSlope() ? GetSlopeMoveDirection() : GetMoveDirection();
            jumpActionPressed = playerInputManager.GetJumpActionPressedThisFrame();
            isSlideActionHeld = playerInputManager.IsCrouchActionHeld();
            if (jumpActionPressed)
                isJumpQueued = true;
            
            jumpActionReleased = playerInputManager.IsJumpActionHeld();
            canJump = IsGrounded();
        }
        
        Vector3 GetMoveDirection() {
            var forward = cameraTransform.forward;
            var right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            var input = playerInputManager.GetMoveVector();
            return (right * input.x + forward * input.z).normalized;
        }

        void MoveCameraTransformToTargetPositionY(float to) {
            cameraTransform.DOKill();
            cameraTransform.DOMoveY(to, playerData.slidingCameraTweenDuration).SetEase(playerData.slidingCameraTweenEase);
        }
        
        void HandleSliding() {
            if (!isSliding) {
                var horizontalSpeed = GetHorizontalVelocity().magnitude;
                var canStartSliding = isSlideActionHeld && IsGrounded() && horizontalSpeed > playerData.allowSlideVelocity;
                if (canStartSliding)
                    StartSliding();
                
                return;
            }

            var shouldStopSliding = !isSlideActionHeld || !IsGrounded() || isJumping;
            if (shouldStopSliding) {
                StopSliding();
                return;
            }

            Sliding();
        }
        
        void StartSliding() {
            isSliding = true;
            slideElapsedTime = 0f;
            initialSlideForce = playerData.slideForce;
            MoveCameraTransformToTargetPositionY(defaultCameraPosition.y - playerData.slideCameraPositionOffset);
        }
        
        void Sliding() {
            var horizontalVelocity = GetHorizontalVelocity();
            var slideDirection = horizontalVelocity.normalized;
            if (IsOnSlope())
                slideDirection = Vector3.ProjectOnPlane(slideDirection, groundHit.normal).normalized;
            
            slideElapsedTime += Time.fixedDeltaTime;
            var normalizedTime = Mathf.Clamp01(slideElapsedTime / playerData.slideTime);
            var finalSlideForce = Mathf.Lerp(initialSlideForce, 0f, normalizedTime);
            playerRigidbody.AddForce(slideDirection * finalSlideForce, ForceMode.Acceleration);
        }
        
        void StopSliding() {
            if (!isSliding)
                return;

            isSliding = false;
            MoveCameraTransformToTargetPositionY(defaultCameraPosition.y);
        }
        
        Vector3 GetHorizontalVelocity() {
            var velocity = playerRigidbody.linearVelocity;
            velocity.y = 0f;
            return velocity;
        }
        
        void Move() {
            if (!isSliding) {
                var horizontalVelocity = GetMovementHorizontalAcceleration();
                playerRigidbody.AddForce(horizontalVelocity, ForceMode.Acceleration);
            }
            TryStep(moveDirection);
            return;

            //Horizontal only so TryStep can deal with small bumps in geometry
            Vector3 GetMovementHorizontalAcceleration() {
                var currentVelocity = GetHorizontalVelocity();
                if (moveDirection.sqrMagnitude < 0.01f)
                    return Vector3.zero;

                var targetVelocity = moveDirection * playerData.moveSpeed;
                return Vector3.ClampMagnitude((targetVelocity - currentVelocity) / Time.fixedDeltaTime, playerData.acceleration);
            }
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
        
        void Jump() {
            if (!isJumpQueued)
                return;

            isJumpQueued = false;
            if (!canJump)
                return;

            isJumping = true;
            jumpElapsedTime = 0f;
            playerRigidbody.AddForce(Vector3.up * playerData.jumpMultiplier, ForceMode.VelocityChange);
            if (playerRigidbody.linearVelocity.y <= 0f)
                isJumping = false;
        }
        
        void ApplyGravity() {
            jumpElapsedTime += Time.fixedDeltaTime;
            var normalizedTime = Mathf.Clamp01( jumpElapsedTime / playerData.jumpDuration);
            var gravityMultiplier = playerData.jumpCurve.Evaluate(normalizedTime);
            var finalGravityControl = playerRigidbody.linearVelocity.y < 0
                ? playerData.fallingGravityControlMultiplier
                : playerData.gravityControlMultiplier;
            
            playerRigidbody.AddForce(Physics.gravity * (gravityMultiplier * finalGravityControl), ForceMode.Acceleration);
        }

        void ApplyJumpCutOffGravity() {
            var finalJumpCutOffForce = playerRigidbody.linearVelocity.y > playerData.minimalJumpVelocity && !jumpActionReleased ? playerData.jumpCutOfForce : 0;
            playerRigidbody.AddForce(Vector3.down * finalJumpCutOffForce, ForceMode.Acceleration);
        }  
        
        void ApplyFriction() {
            if (isJumping || isSliding || GetMoveDirection().sqrMagnitude > 0.01f)
                return;

            var horizontalVelocity = GetHorizontalVelocity();
            var speed = horizontalVelocity.magnitude;
            if (speed < 0.01f)
                return;

            var friction = Mathf.Min(speed / Time.fixedDeltaTime, playerData.groundedFriction);
            playerRigidbody.AddForce(-horizontalVelocity.normalized * friction, ForceMode.Acceleration);
        }
        
        int GetGroundedSphereCast() {
            if (playerRigidbody.linearVelocity.y > 0.1f)
                return 0;

            var sphereCastRadius = GetSphereCastRadius;
            return Physics.OverlapSphereNonAlloc(GetPlayerFeetPosition(), sphereCastRadius, groundedCollisionResults, GetGroundedCastLayerMask);
        }
        
        int GetGroundedCastLayerMask => ~ignoredLayer;
        
        bool IsGrounded() => GetGroundedSphereCast() >= 1 && playerRigidbody.linearVelocity.y <= 0f;
        
        bool IsOnSlope() {
            if (!Physics.Raycast(transform.position, Vector3.down, out groundHit, GetSphereCastRadius))
                return false;
            
            var angle = Vector3.Angle(Vector3.up, groundHit.normal);
            return angle < playerData.maxSlopeAngle && angle != 0;
        }
        
        Vector3 GetSlopeMoveDirection() => Vector3.ProjectOnPlane(GetMoveDirection(), groundHit.normal).normalized;
        
        Vector3 GetPlayerFeetPosition() =>
            playerRigidbody.position + Vector3.down * (playerCollider.radius / 2) + Vector3.up * LOWER_ORIGIN_MINIMUM_HEIGHT;
        
        Vector3 GetPlayerCenterPosition() =>
            playerRigidbody.position + Vector3.up * (playerCollider.height / 2);
    
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
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetPlayerCenterPosition(), GetSphereCastRadius);
        }
    }
}
