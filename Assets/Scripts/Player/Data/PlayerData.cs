using UnityEngine;

namespace Player.Data {
    [CreateAssetMenu(fileName = "PlayerData", menuName = "Data/PlayerData/CreatePlayerData", order = 1)]
    public class PlayerData : ScriptableObject {
        [Header("Mouse Data")]
        public float mouseSensitivityX = 0.5f;
        public float mouseSensitivityY = 0.5f;
        [Header("Movement Data")]
        public float moveSpeed = 10f;
        public float acceleration = 25f;
        public float stepHeight = 0.3f;
        public float stepCheckDistance = 0.4f;
        public float stepClimbSpeed = 15f;
        [Header("Jump Data")]
        public float jumpPower = 200f;
    }
}
