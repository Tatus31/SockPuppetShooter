using DG.Tweening;
using UnityEngine;

namespace Player.Data {
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Data/PlayerData/CreatePlayerData", order = 1)]
    public class PlayerData : ScriptableObject {
        [Header("Mouse Data")]
        public float mouseSensitivityX = 0.5f;
        public float mouseSensitivityY = 0.5f;
        [Header("Movement Data")]
        public float moveSpeed = 20f;
        public float inAirMoveSpeed = 10f;
        public float acceleration = 300f;
        public float stepHeight = 0.3f;
        public float stepCheckDistance = 0.4f;
        public float stepClimbSpeed = 1f;
        public float groundedFriction = 300f;
        [Header("Jump Data")]
        public float jumpDuration = 0.8f;
        public float jumpMultiplier = 10f;
        public float gravityControlMultiplier = 2f;
        public float fallingGravityControlMultiplier = 6f;
        public float jumpCutOfForce = 105f;
        public float minimalJumpVelocity = 15f;
        public float inAirAcceleration = 55f;
        public AnimationCurve jumpCurve;
        [Header("Slope Handling Data")] 
        public float maxSlopeAngle = 35f;
        public float frictionOnSlope = 2.5f;
        [Header("Sliding")] 
        public float allowSlideVelocity = 5f;
        public float slideForce = 250f;
        public float slideTime = 0.5f;
        public float slidingFriction = 10f;
        public float slidingCameraTweenDuration = 0.05f;
        public float slideCameraPositionOffset = 0.35f;
        public Ease slidingCameraTweenEase =  Ease.InQuad;
        public AnimationCurve slidingForceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}
