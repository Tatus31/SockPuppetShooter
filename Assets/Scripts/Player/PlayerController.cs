using System;
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
        
        void Start() {
            playerInputManager = PlayerInputManager.TryGetInstance();
        }

        void FixedUpdate() {
            Move();
        }
        void LateUpdate() {
            Look();
        }

        void Look() {
            Vector2 currentMouseDelta = playerInputManager.GetLookDelta();
            
            var mouseX = currentMouseDelta.x * 0.5f;
            var mouseY = currentMouseDelta.y * 0.5f;

            var rot = CameraTransform.transform.localRotation.eulerAngles;
            var yRotation = rot.y + mouseX;
            
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            CameraTransform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        }

        Vector3 GetMoveDirection()
        {
            var forward = CameraTransform.forward;
            var right = CameraTransform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            var input = playerInputManager.GetMoveVector();
            return (right * input.x + forward * input.z).normalized;
        }
        
        void Move()
        {
            float moveSpeed = 5f;
            float acceleration = 10f;
            
            var moveDirection = GetMoveDirection();
            var currentVelocity = playerRigidbody.linearVelocity;
            var horizontalVelocity = new Vector3( currentVelocity.x, 0f, currentVelocity.z );
            var targetVelocity = moveDirection * moveSpeed;
            
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
            playerRigidbody.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
            TryStep(moveDirection);
        }

        void TryStep(Vector3 direction)
        {
            if (direction == Vector3.zero)
                return;

            var stepHeight = 0.3f;
            var stepCheckDistance  = 0.4f;
            
            var feetPosition = playerRigidbody.position + Vector3.up * (playerCollider.radius);
            var lowerOrigin = feetPosition + Vector3.up * 0.05f;
            var upperOrigin = feetPosition + Vector3.up * stepHeight;

            var lowerHit = Physics.Raycast(lowerOrigin, direction, stepCheckDistance);
            var upperHit = Physics.Raycast(upperOrigin, direction, stepCheckDistance);
            if (lowerHit && !upperHit)
                playerRigidbody.position += Vector3.up * (stepHeight * 5f * Time.fixedDeltaTime);
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
