using UnityEngine;

namespace Entity.Data {
    
    [CreateAssetMenu(fileName = "DamageableData", menuName = "Data/Entity/CreateDamageableData")]
    public class DamageableData : ScriptableObject {
        [Header("Health")]
        public float maxHealth = 100f;

        [Header("Hit Visuals")]
        public Color hitColor = Color.white;
        public float hitIntensity = 5f;
        public float hitEffectDuration = 0.2f;
    }
}
