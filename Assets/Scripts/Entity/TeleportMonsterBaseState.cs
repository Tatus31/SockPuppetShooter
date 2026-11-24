using Entity.StateMachine;
using UnityEngine;

namespace Entity {
    public abstract class TeleportMonsterBaseState : IState {
        protected readonly TeleportMonster teleportMonster;
        protected readonly Animator animator;
        
        protected static readonly int IdleHash = Animator.StringToHash("Idle");
        protected static readonly int AttackHash = Animator.StringToHash("Attack");
        protected static readonly int TeleportHash = Animator.StringToHash("Teleport");

        protected const float CROSS_FADE_DURATION = 0.1f;

        protected TeleportMonsterBaseState(TeleportMonster teleportMonster, Animator animator) {
            this.teleportMonster = teleportMonster;
            this.animator = animator;
        }

        protected void PlayAnimation(int hash) {
            //TODO: when animations are done change it
            
            // if (animator != null) 
            //     animator.CrossFade(hash, CROSS_FADE_DURATION);
        }

        public virtual void OnEnter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void OnExit() { }
    }
}