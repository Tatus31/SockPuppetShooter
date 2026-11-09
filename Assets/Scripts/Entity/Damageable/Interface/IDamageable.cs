namespace Entity.Damageable.Interface {
    public interface IDamageable {
        void TakeDamage(float damage);
        void TakeHeal(float healAmount);
        void Die();
    }
}