using UnityEngine;

namespace Player.Weapons {
    public class HitScanPistol : PlayerBaseWeapon {
        void Update() {
            GetEntityAtCrosshair(out var hit);
            if (playerInputManager.GetAttackActionPressedThisFrame() && CanShoot())
                TryShoot(hit);
        }

        void TryShoot(RaycastHit hit) {
            ChangeToShotSprite();
            ApplyDamage(hit);
            ResetShotCooldown();
        }
    }
}
