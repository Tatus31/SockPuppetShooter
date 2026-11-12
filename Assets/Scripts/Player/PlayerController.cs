using System;
using DG.Tweening;
using Managers;
using Player.Data;
using TMPro;
using UnityEngine;
using Utils;

namespace Player {
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour {
        //So we dont start checking underground
        const float LOWER_ORIGIN_MINIMUM_HEIGHT = 0.45f;
        const float ALLOWED_WALL_CONTACT_THRESHOLD = 0.2f;
        
        [Header("Refs and Data")]
        [SerializeField] PlayerData playerData;
        [Space(10)]
        [SerializeField] Transform cameraTransform;
        [SerializeField] Transform defaultCameraPositionTransform;
        [SerializeField] GameObject slidingEffectGameObject;
        [SerializeField] Rigidbody playerRigidbody;
        [SerializeField] CapsuleCollider playerCollider;
        
        [Header("Gizmo Settings")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] TextMeshProUGUI speedText;
        
        PlayerInputManager playerInputManager;
        DebugLogManager debugLogManager;
        LayerManager layerManager;

        RaycastHit groundHit;

        float xRotation;
        float jumpElapsedTime;
        float initialSlideForce;
        float slideElapsedTime;
        float wallImpactSpeed;
        float wallKickTimer;

        Vector2 currentMouseDelta;
        Vector3 moveDirection;
        Vector3 wallNormal;

        
        bool jumpActionPressed;
        bool jumpActionReleased;
        bool canJump;
        bool isJumpQueued; 
        bool isSliding;
        bool isSlideActionHeld;
        [SerializeField] bool isTouchingWall;
        
        readonly Collider[] groundedCollisionResults = new Collider[16];
        readonly Collider[] forwardCollisionResults = new Collider[16];
        
        float GetSphereCastRadius => playerCollider ? playerCollider.radius / 1.5f : 1f;

        #region Mono
        void OnValidate() {
            initialSlideForce = playerData.slideForce;
        }

        void Start() {
            var gameManager = GameManager.TryGetInstance();
            playerInputManager = gameManager.playerInputManager;
            debugLogManager = gameManager.debugLogManager;
            layerManager = gameManager.layerManager;
        }

        void Update() {
            UpdatePlayerInput();
            UpdateSpeedText();
        }

        void FixedUpdate() {
            HandleWallKickTimer();
            AlignVelocityToSlope();
            Move();
            Jump();
            ApplyJumpGravity();
            ApplyFriction();
            ApplyJumpCutOffGravity();
            HandleSliding();
        }
        void LateUpdate() {
            Look();
        }

        void OnCollisionEnter(Collision other) {
            HandleWallContact(other, true);
        }

        void OnCollisionStay(Collision other) {
            HandleWallContact(other, false);
        }

        void OnCollisionExit(Collision other) {
            isTouchingWall = false;
            wallImpactSpeed = 0f;
        }

        #endregion

        #region Look
        void Look() {
            var mouseX = currentMouseDelta.x * playerData.mouseSensitivityX;
            var mouseY = currentMouseDelta.y * playerData.mouseSensitivityY;

            var rot = cameraTransform.transform.localRotation.eulerAngles;
            var yRotation = rot.y + mouseX;
            
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            cameraTransform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        #endregion

        #region Input
        void UpdatePlayerInput() {
            currentMouseDelta = playerInputManager.GetLookDelta();
            moveDirection = IsOnSlope() ? GetSlopeMoveDirection() : GetMoveDirection();
            jumpActionPressed = playerInputManager.GetJumpActionPressedThisFrame();
            isSlideActionHeld = playerInputManager.IsCrouchActionHeld();

            ToggleDebugConsole();
            QueueJumpInput();
            
            jumpActionReleased = playerInputManager.IsJumpActionHeld();
            canJump = IsGrounded();
            return;

            void QueueJumpInput() {
                if (jumpActionPressed)
                    isJumpQueued = true;
            }
            
            void ToggleDebugConsole() { 
                if (playerInputManager.GetInteractActionPressedThisFrame())
                    debugLogManager.ToggleConsole();
            }
        }
        #endregion
        
        #region Move
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

        void Move() {
            if (wallKickTimer > 0f)
                return;
            
            if (!isSliding) {
                var horizontalVelocity = GetMovementHorizontalAcceleration();
                playerRigidbody.AddForce(horizontalVelocity, ForceMode.Acceleration);
            }
            TryStep(moveDirection);
            return;

            //Horizontal only so TryStep can deal with small bumps in geometry
            Vector3 GetMovementHorizontalAcceleration() {
                var currentVelocity = IsOnSlope() ? GetSlopeVelocity() : GetHorizontalVelocity();
                if (moveDirection.sqrMagnitude < 0.01f)
                    return Vector3.zero;

                var targetVelocity = canJump ? moveDirection * playerData.moveSpeed : moveDirection * playerData.inAirMoveSpeed;
                var finalAcceleration = canJump ? playerData.acceleration : playerData.inAirAcceleration;
                return Vector3.ClampMagnitude((targetVelocity - currentVelocity) / Time.fixedDeltaTime, finalAcceleration);
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
        #endregion

        #region Friction
        void ApplyFriction() {
            if (wallKickTimer > 0f)
                return;
            
            var isOnSlope = IsOnSlope();
            var slopeVelocity = isOnSlope ? GetSlopeVelocity() : GetHorizontalVelocity();
            var slopeSpeed = slopeVelocity.magnitude;
            if (slopeSpeed < 0.01f)
                return;

            if (!isSliding && GetMoveDirection().sqrMagnitude > 0.01f)
                return;

            var finalFriction = isSliding
                ? isOnSlope ? playerData.frictionOnSlope : playerData.slidingFriction
                : playerData.groundedFriction;

            var friction = Mathf.Min(slopeSpeed / Time.fixedDeltaTime, finalFriction);
            playerRigidbody.AddForce(-slopeVelocity.normalized * friction, ForceMode.Acceleration);
        }
        #endregion

        #region Slide
        void HandleSliding() {
            MoveCameraDuringSlide();
            
            if (!isSliding) {
                var horizontalSpeed = GetHorizontalVelocity().magnitude;
                var canStartSliding = isSlideActionHeld && IsGrounded() && horizontalSpeed > playerData.allowSlideVelocity;
                if (canStartSliding)
                    StartSliding();
                
                return;
            }

            var shouldStopSliding = !isSlideActionHeld || !IsGrounded() || !canJump;
            if (shouldStopSliding) {
                StopSliding();
                return;
            }

            Slide();
        }
        
        void StartSliding() {
            isSliding = true;
            ToggleSlideEffect();
            slideElapsedTime = 0f;
            initialSlideForce = playerData.slideForce;
        }
        
        void Slide() {
            var horizontalVelocity = GetHorizontalVelocity();
            var slideDirection = horizontalVelocity.normalized;
            if (IsOnSlope())
                slideDirection = Vector3.ProjectOnPlane(slideDirection, groundHit.normal).normalized;

            slideElapsedTime += Time.fixedDeltaTime;
            var normalizedTime = Mathf.Clamp01(slideElapsedTime / playerData.slideTime);
            var slideForceMultiplier = playerData.slidingForceCurve.Evaluate(normalizedTime);
            var finalSlideForce = initialSlideForce * slideForceMultiplier;
            playerRigidbody.AddForce(slideDirection * finalSlideForce, ForceMode.Acceleration);
        }
        
        void StopSliding() {
            if (!isSliding)
                return;

            ToggleSlideEffect();
            isSliding = false;
        }

        void MoveCameraDuringSlide() {
            var targetY = isSliding 
                ? defaultCameraPositionTransform.position.y - playerData.slideCameraPositionOffset 
                : defaultCameraPositionTransform.position.y;
            
            MoveCameraTransformToTargetPositionY(targetY);
        }
        
        void MoveCameraTransformToTargetPositionY(float to) => cameraTransform.
            DOMoveY(to, playerData.slidingCameraTweenDuration).
            SetEase(playerData.slidingCameraTweenEase);
        
        void ToggleSlideEffect() => slidingEffectGameObject.SetActive(!slidingEffectGameObject.activeSelf);
        
        #endregion

        #region Jump
        void Jump() {
            if (!isJumpQueued)
                return;

            isJumpQueued = false;
            if (TryWallKick())
                return;
            
            if (!canJump)
                return;
            
            jumpElapsedTime = 0f;
            playerRigidbody.AddForce(Vector3.up * playerData.jumpMultiplier, ForceMode.VelocityChange);
        }
        
        void ApplyJumpGravity() {
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
        #endregion

        #region WallKick

        void HandleWallContact(Collision collision, bool useImpactVelocity) {
            foreach (var contact in collision.contacts) {
                if (Mathf.Abs(contact.normal.y) > ALLOWED_WALL_CONTACT_THRESHOLD)
                    continue;
                
                var normal= contact.normal;
                if (Vector3.Dot(normal, transform.position - contact.point) < 0f) 
                    normal = -normal;
                
                wallNormal = Vector3.ProjectOnPlane(normal, Vector3.up).normalized;
                isTouchingWall = true;
                if (!useImpactVelocity)
                    return;
                
                var horizontalImpactVelocity = Vector3.ProjectOnPlane(collision.relativeVelocity, Vector3.up);
                wallImpactSpeed = Mathf.Abs(Vector3.Dot(horizontalImpactVelocity, wallNormal));

                return;
            }
        }

        bool TryWallKick() {
            if (!isTouchingWall)
                return false;

            if (wallImpactSpeed < playerData.wallKickMinimumSpeed)
                return false;
            
            var velocity = playerRigidbody.linearVelocity;
            var wallTangent = Vector3.Cross(Vector3.up, wallNormal).normalized;
            var tangentVelocity = Vector3.Project(velocity, wallTangent);
            var kickVelocity = tangentVelocity + wallNormal * playerData.wallKickHorizontalForce + Vector3.up * playerData.wallKickVerticalForce;
            playerRigidbody.linearVelocity = kickVelocity;
            
            wallKickTimer = playerData.wallKickCooldown;
            isTouchingWall = false;
            wallImpactSpeed = 0f;
            jumpElapsedTime = 0f;
            
            return true;
        }

        #endregion

        #region Getters
        Vector3 GetHorizontalVelocity() {
            var velocity = playerRigidbody.linearVelocity;
            velocity.y = 0f;
            return velocity;
        }
        
        int GetGroundedSphereCast() {
            var sphereCastRadius = GetSphereCastRadius;
            return Physics.OverlapSphereNonAlloc(GetPlayerFeetPosition(), sphereCastRadius, groundedCollisionResults, layerManager.GetGroundedCastLayerMask);
        }
        
        int GetForwardSphereCast() {
            var sphereCastRadius = GetSphereCastRadius;
            return Physics.OverlapSphereNonAlloc(GetPlayerCenterPositionWithOffset(GetCameraForwardDirection(), 2), sphereCastRadius, forwardCollisionResults, layerManager.GetGroundedCastLayerMask);
        }

        Vector3 GetCameraForwardDirection() => cameraTransform ? cameraTransform.forward :  Vector3.zero;
        
        bool IsFacingWall() => GetForwardSphereCast() >= 1;
        
        bool IsGrounded() => GetGroundedSphereCast() >= 1;
        
        bool IsOnSlope() {
            if (!Physics.Raycast(transform.position, Vector3.down, out groundHit, GetSphereCastRadius))
                return false;
            
            var angle = Vector3.Angle(Vector3.up, groundHit.normal);
            return angle < playerData.maxSlopeAngle && angle != 0;
        }
        
        Vector3 GetSlopeMoveDirection() => Vector3.ProjectOnPlane(GetMoveDirection(), groundHit.normal).normalized;
        
        Vector3 GetSlopeVelocity() => Vector3.ProjectOnPlane(playerRigidbody.linearVelocity, groundHit.normal);
        
        void AlignVelocityToSlope() {
            if (!IsGrounded() || !IsOnSlope())
                return;

            var velocity = playerRigidbody.linearVelocity;
            playerRigidbody.linearVelocity = Vector3.ProjectOnPlane(velocity, groundHit.normal);
        }
        
        void HandleWallKickTimer() => wallKickTimer = Mathf.Max(0f, wallKickTimer - Time.fixedDeltaTime);
        
        Vector3 GetPlayerFeetPosition() =>
            playerRigidbody.position + Vector3.down * (playerCollider.radius / 2) + Vector3.up * LOWER_ORIGIN_MINIMUM_HEIGHT;
        
        Vector3 GetPlayerCenterPosition() =>
            playerRigidbody.position + Vector3.up * (playerCollider.height / 2);
        
        Vector3 GetPlayerCenterPositionWithOffset(Vector3 offset, float multiplier = 1) =>
            playerRigidbody.position + Vector3.up * (playerCollider.height / 2) + offset * multiplier;
        
        #endregion

        #region Gizmos
        void OnDrawGizmos() {
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
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetPlayerCenterPositionWithOffset(GetCameraForwardDirection(), 2), GetSphereCastRadius);
        }
        
        void UpdateSpeedText() {
            if (!speedText || !playerRigidbody)
                return;

            speedText.text = $"Speed: {GetHorizontalVelocity().magnitude:F2}";
        }
        
        #endregion
    }
}
