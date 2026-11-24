using System;
using System.Collections;
using System.Collections.Generic;
using Entity.Damageable.Interface;
using Entity.Data;
using UnityEngine;

namespace Entity.Damageable {
    public abstract class Damageable : MonoBehaviour, IDamageable
    {
        [Serializable]
        public class DamageableMaterialData {
            public Material currentMaterial;
            public Color originalEmissionColor;
        }
        
        [Header("Configuration")]
        [SerializeField] protected DamageableData damageableData;
        [SerializeField] protected List<DamageableMaterialData> currentDamageableMaterialData;
        
        protected float currentHealth;
        protected Coroutine hitCoroutine;

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        protected virtual void Awake() {
            currentHealth = damageableData.maxHealth;
        }

        public virtual void TakeDamage(float damage, bool isCritical) {
            currentHealth -= damage;
            if (currentHealth <= 0) {
                ResetHitVisuals();
                Die();
                return;
            }
            
            VisualTakeHit(isCritical);
        }
        
        void ResetMaterialsToOriginal() {
            foreach (var materialData in currentDamageableMaterialData) {
                if (!materialData.currentMaterial)
                    continue;

                materialData.currentMaterial.SetColor(EmissionColor, materialData.originalEmissionColor);
            }
        }
        
        void ResetHitVisuals() {
            if (hitCoroutine != null) {
                StopCoroutine(hitCoroutine);
                hitCoroutine = null;
            }

            ResetMaterialsToOriginal();
        }

        public virtual void TakeHeal(float healAmount) =>
            currentHealth = Mathf.Min(currentHealth + healAmount, damageableData.maxHealth);
        
        protected virtual void VisualTakeHit(bool isCritical) {
            if (hitCoroutine != null)
                StopCoroutine(hitCoroutine);

            hitCoroutine = StartCoroutine(GoBackToOriginal(isCritical));
        }

        IEnumerator GoBackToOriginal(bool isCritical) {
            var hitColor = isCritical ? damageableData.critHitColor : damageableData.hitColor;
            var finalHitColor = hitColor * damageableData.hitIntensity;
            foreach (var materialData in currentDamageableMaterialData) {
                if (!materialData.currentMaterial)
                    continue;
                
                materialData.currentMaterial.SetColor(EmissionColor, finalHitColor);
            }
            
            var elapsedTime = 0f;
            while (elapsedTime < damageableData.hitEffectDuration) {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / damageableData.hitEffectDuration;
                foreach (var materialData in currentDamageableMaterialData) {
                    if (!materialData.currentMaterial)
                        continue;
                    
                    materialData.currentMaterial.SetColor(EmissionColor, Color.Lerp(finalHitColor, materialData.originalEmissionColor, t));
                }
                
                yield return null;
            }

            foreach (var materialData in currentDamageableMaterialData) {
                if (!materialData.currentMaterial)
                    continue;
                
                materialData.currentMaterial.SetColor(EmissionColor, materialData.originalEmissionColor);
            }
            
            ResetMaterialsToOriginal();
            hitCoroutine = null;
        }

        public virtual void Die() => gameObject.SetActive(false);
    }
}