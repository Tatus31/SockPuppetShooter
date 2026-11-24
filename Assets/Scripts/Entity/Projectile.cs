using System;
using UnityEngine;
using Utils;

namespace Entity {
    public class Projectile : MonoBehaviour {
        [SerializeField] float speed = 12f;
        [SerializeField] float lifetime = 5f;
        [SerializeField] float damage = 10f;

        Action<Projectile> releaseAction;
        float despawnTime;
        bool isReleased;
        
        public void Init(Vector3 position, Quaternion rotation, Action<Projectile> release) {
            releaseAction = release;
            transform.SetPositionAndRotation(position, rotation);
            despawnTime = Time.time + lifetime;
            isReleased = false;
        }

        void Update() {
            transform.position += transform.forward * (speed * Time.deltaTime);

            if (Time.time >= despawnTime)
                Release();
        }
        
        void OnTriggerEnter(Collider other) {
            if (other.isTrigger) 
                return;
            
            if (other.CompareTag(Consts.ENEMY_TAG))
                return; 

            if (other.CompareTag(Consts.PLAYER_TAG)) {
                // TODO: do damage 
                Debug.Log("Player git hit ");
            }
            Release();
        }

        void Release() {
            if (isReleased)
                return; 
            
            isReleased = true;
            releaseAction?.Invoke(this);
        }
    }
}