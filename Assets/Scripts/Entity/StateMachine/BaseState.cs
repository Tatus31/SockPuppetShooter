using Player;
using UnityEngine;

namespace Entity.StateMachine {
    public abstract class BaseState : IState {
        protected readonly PlayerController playerController;
        protected readonly Animator animator;
        
        //Anim Hash here
        
        protected const float CROSS_FADE_DURATION = 0.1f;

        protected BaseState(PlayerController playerController, Animator animator) {
            this.playerController =  playerController;
            this.animator = animator;
        }
        
        public virtual void OnEnter() {
            
        }

        public virtual void Update() {

        }

        public virtual void FixedUpdate() {

        }

        public virtual void OnExit() {

        }
    }
}