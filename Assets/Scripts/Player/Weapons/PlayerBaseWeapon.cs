using DG.Tweening;
using Entity.Damageable.Interface;
using Managers;
using Player.Weapons.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Player.Weapons {
    public abstract class PlayerBaseWeapon : MonoBehaviour {
        [Header("Refs")]
        [SerializeField] WeaponData weaponData;
        [SerializeField] Camera camera;
        [SerializeField] Image weaponImage;
        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] float rayDistance = 100f;
        
        readonly RaycastHit[] entitiesAtCrosshair = new RaycastHit[16];
        
        LayerManager layerManager;
        protected PlayerInputManager playerInputManager;
        
        float nextShotTime;
        float nextSpriteChangeTime;
        float shotSpriteEndTime;
        
        Tween weaponScaleTween;
        Vector3 defaultWeaponScale;
        
        void Awake() {
            defaultWeaponScale = weaponImage.transform.localScale;
        }

        void Start() {
            var gameManager = GameManager.TryGetInstance();
            playerInputManager = gameManager.playerInputManager;
            layerManager = gameManager.layerManager;
        }

        protected void GetEntityAtCrosshair(out RaycastHit currentHit) {
            var count = GetHitCount();
            ChangeWeaponSprite();
            if (count == 0) {
                currentHit = default;
                return;
            }
            
            currentHit = GetClosestHit(count);
            return;

            int GetHitCount() {
                var origin = camera.transform.position;
                var direction = camera.transform.forward;
                var hitCount = Physics.RaycastNonAlloc(
                    origin, direction, 
                    entitiesAtCrosshair, rayDistance, 
                    layerManager.GetDamageableEntityLayerMask);
                
                if (hitCount > 0 || !weaponData.enableAimAssist)
                    return hitCount;

                return Physics.SphereCastNonAlloc(
                    origin, weaponData.aimAssistRadius, 
                    direction, entitiesAtCrosshair, 
                    rayDistance, layerManager.GetDamageableEntityLayerMask);
            }

            RaycastHit GetClosestHit(int hitCount) {
                var closestHit = entitiesAtCrosshair[0];
                for (var i = 1; i < hitCount; i++) {
                    if (entitiesAtCrosshair[i].distance < closestHit.distance)
                        closestHit = entitiesAtCrosshair[i];
                }

                return closestHit;
            }
            
            void ChangeWeaponSprite() {
                var isOnTarget = count > 0;
                var scaleEffect = isOnTarget ? weaponData.idleToTargetScale : weaponData.targetToIdleScale;
                if (Time.time < shotSpriteEndTime)
                    return;
                
                var targetSprite = isOnTarget ? weaponData.OnTargetSprite : weaponData.IdleSprite;
                SetWeaponSprite(targetSprite, scaleEffect);
            }
        }
        
        protected void ApplyDamage(RaycastHit hit) {
            if(!hit.collider)
                return;
            
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(weaponData.damage);
        }
        
        protected void ChangeToShotSprite() {
            shotSpriteEndTime = Time.time + weaponData.shotSpriteDisplayTime;
            SetWeaponSprite(weaponData.shotSprite, weaponData.shotScale, weaponData.forceSpriteChangeOnEnter);
        }
        
        protected bool CanShoot() => Time.time >= nextShotTime;
        
        protected void ResetShotCooldown() =>  nextShotTime = Time.time + weaponData.shotCooldown;
        
        void SetWeaponSprite(Sprite sprite, WeaponData.WeaponScaleEffect effect, bool force = false) {
            if (weaponImage.sprite == sprite && !force)
                return;

            if (!force && Time.time < nextSpriteChangeTime)
                return;

            weaponImage.sprite = sprite;
            ResetSpriteChangeCooldown();
            PlayScaleEffect(effect);
        }
            
        void ResetSpriteChangeCooldown() => nextSpriteChangeTime = Time.time + weaponData.spriteChangeCooldown;
        
        void PlayScaleEffect(WeaponData.WeaponScaleEffect effect) {
            weaponScaleTween?.Kill();
            weaponImage.transform.localScale = defaultWeaponScale;
            weaponScaleTween = weaponImage.transform.DOPunchScale(
                effect.punchScale, effect.duration, 
                effect.vibrato, effect.elasticity);
        }
        
        void OnDrawGizmosSelected() {
            if (!drawGizmos || !camera)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(camera.transform.position, camera.transform.forward * rayDistance);
        }
    }
}
