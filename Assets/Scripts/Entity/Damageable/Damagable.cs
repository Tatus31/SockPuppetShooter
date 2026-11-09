using System.Collections;
using Entity.Damageable.Interface;
using Entity.Data;
using UnityEngine;

namespace Entity.Damageable {
    public abstract class Damageable : MonoBehaviour, IDamageable
    {
        [Header("Configuration")]
        [SerializeField] protected DamageableData damageableData;

        protected float currentHealth;
        protected Material currentMaterial;
        protected Coroutine hitCoroutine;
        Color originalEmissionColor;

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        protected virtual void Awake() {
            currentHealth = damageableData.maxHealth;
            currentMaterial = GetComponentInChildren<MeshRenderer>().material;
            originalEmissionColor = currentMaterial.GetColor(EmissionColor);
        }

        public virtual void TakeDamage(float damage) {
            currentHealth -= damage;
            VisualTakeHit();
            if (currentHealth <= 0)
                Die();
        }

        public virtual void TakeHeal(float healAmount) =>
            currentHealth = Mathf.Min(currentHealth + healAmount, damageableData.maxHealth);
        
        protected virtual void VisualTakeHit() {
            if (hitCoroutine != null)
                StopCoroutine(hitCoroutine);

            hitCoroutine = StartCoroutine(GoBackToOriginal());
        }

        IEnumerator GoBackToOriginal() {
            var hitColor = damageableData.hitColor * damageableData.hitIntensity;
            var elapsedTime = 0f;
            currentMaterial.SetColor(EmissionColor, hitColor);
            while (elapsedTime < damageableData.hitEffectDuration) {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / damageableData.hitEffectDuration;
                currentMaterial.SetColor(EmissionColor, Color.Lerp(hitColor, originalEmissionColor, t));

                yield return null;
            }

            currentMaterial.SetColor(EmissionColor, originalEmissionColor);
            hitCoroutine = null;
        }

        public virtual void Die() => Debug.Log("Its dead"); //gameObject.SetActive(false);
    }
}