using DG.Tweening;
using UnityEngine;

namespace Player.Data {
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Data/PlayerData/CreatePlayerData", order = 1)]
    public class PlayerData : ScriptableObject {
        [Header("Mouse Data")]
        public float mouseSensitivityX = 0.5f;
        public float mouseSensitivityY = 0.5f;
        [Header("Movement Data")]
        public float moveSpeed = 10f;
        public float acceleration = 45f;
        public float stepHeight = 0.3f;
        public float stepCheckDistance = 0.4f;
        public float stepClimbSpeed = 1f;
        public float groundedFriction = 2.5f;
        [Header("Jump Data")]
        public float jumpDuration = 0.8f;
        public float jumpMultiplier = 10f;
        public float gravityControlMultiplier = 2f;
        public float fallingGravityControlMultiplier = 6f;
        public float jumpCutOfForce = 50f;
        public float minimalJumpVelocity = 15f;
        public AnimationCurve jumpCurve;
        [Header("Slope Handling Data")] 
        public float maxSlopeAngle = 35f;
        [Header("Sliding")] 
        public float allowSlideVelocity = 5f;
        public float slideForce = 250f;
        public float slideTime = 0.5f;
        public float slidingCameraTweenDuration = 0.5f;
        public Ease slidingCameraTweenEase =  Ease.InQuad;
        public float slideCameraPositionOffset = 1f;
    }
}
