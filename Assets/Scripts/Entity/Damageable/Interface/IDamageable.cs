namespace Entity.Damageable.Interface {
    public interface IDamageable {
        void TakeDamage(float damage, bool isCritical);
        void TakeHeal(float healAmount);
        void Die();
    }
}