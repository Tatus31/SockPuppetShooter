using UnityEngine;

namespace Entity {
    public class TeleportMonsterIdleState : TeleportMonsterBaseState {
        public TeleportMonsterIdleState(TeleportMonster teleportMonster, Animator animator) : base(teleportMonster, animator) { }

        public override void OnEnter() {
            PlayAnimation(IdleHash);
        }
    }
}