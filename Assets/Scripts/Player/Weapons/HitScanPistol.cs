namespace Player.Weapons {
    public class HitScanPistol : PlayerBaseWeapon {
        void Update() {
            if (playerInputManager.GetAttackActionPressedThisFrame())
                GetEntityAtCrosshair();
        }

    }
}
