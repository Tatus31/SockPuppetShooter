using Entity.Damageable.Interface;
using Managers;
using UnityEngine;

namespace Player.Weapons {
    public abstract class PlayerBaseWeapon : MonoBehaviour {
        [Header("Refs")]
        [SerializeField] Camera camera;
        [Header("Gizmos")]
        [SerializeField] bool drawGizmos = true;
        [SerializeField] float rayDistance = 100f;
        
        readonly RaycastHit[] entitiesAtCrosshair = new RaycastHit[16];
        
        LayerManager layerManager;
        protected PlayerInputManager playerInputManager;

        void Start() {
            var gameManager = GameManager.TryGetInstance();
            playerInputManager = gameManager.playerInputManager;
            layerManager = gameManager.layerManager;
        }

        protected void GetEntityAtCrosshair() {
            var hitCount = Physics.RaycastNonAlloc(camera.transform.position, camera.transform.forward, 
                entitiesAtCrosshair, rayDistance, layerManager.GetDamageableEntityLayerMask);

            if (hitCount == 0)
                return;

            var closestHit = entitiesAtCrosshair[0];
            for (var i = 1; i < hitCount; i++) {
                if (entitiesAtCrosshair[i].distance < closestHit.distance)
                    closestHit = entitiesAtCrosshair[i];
            }

            var damageable = closestHit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(10);
        }

        void OnDrawGizmosSelected() {
            if (!drawGizmos || !camera)
                return;

            Gizmos.color = Color.red;
            Gizmos.DrawRay(camera.transform.position, camera.transform.forward * rayDistance);
        }
    }
}
