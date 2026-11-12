using UnityEngine;

namespace Player.Weapons.Data {
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Data/WeaponData/CreateWeaponData")]
    public class WeaponData : ScriptableObject {
        [Header("Weapon Data"), Space(5)]
        public bool enableAimAssist;
        public bool forceSpriteChangeOnEnter = true;
        public float aimAssistRadius = 0.15f;
        public float damage = 10f;
        public float shotCooldown = 0.25f;
        [Header("Weapon Sprites"), Space(5)]
        public Sprite shotSprite;
        public Sprite OnTargetSprite;
        public Sprite IdleSprite;
        [Space(1)]
        public float spriteChangeCooldown = 1f;
        public float shotSpriteDisplayTime = 0.12f;
        [Header("Scale Effects"), Space(5)]
        public WeaponScaleEffect idleToTargetScale;
        public WeaponScaleEffect targetToIdleScale;
        public WeaponScaleEffect shotScale;
        [Header("Sway Effects"), Space(5)] 
        public WeaponSwayEffect weaponSwayEffect;
        
        [System.Serializable]
        public class WeaponScaleEffect {
            public Vector3 punchScale = new Vector3(0.1f, 0.1f, 0f);
            public float duration = 0.2f;
            public int vibrato = 5;
            [Range(0f, 1f)] 
            public float elasticity = 0.5f;
        }
        
        [System.Serializable]
        public class WeaponSwayEffect {
            public float swayAmount = 0.05f;
            public float smoothness = 10f;
        }
    }
}
