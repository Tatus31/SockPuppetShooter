using UnityEngine;

namespace Entity {
    public class TeleportMonsterAttackState : TeleportMonsterBaseState {
        float timeSinceSeenPlayer;   
        float continuousSeenTime;    
        float nextShotTime;

        public bool LostPlayer => timeSinceSeenPlayer >= teleportMonster.LoseSightTime;

        public TeleportMonsterAttackState(TeleportMonster teleportMonster, Animator animator) : base(teleportMonster, animator) { }

        public override void OnEnter() {
            PlayAnimation(AttackHash);
            timeSinceSeenPlayer = 0f;
            continuousSeenTime = 0f;
            nextShotTime = 0f;
        }

        public override void Update() {
            if (teleportMonster.CanSeePlayer()) {
                timeSinceSeenPlayer = 0f;
                continuousSeenTime += Time.deltaTime;
                teleportMonster.FacePlayer();
                if (continuousSeenTime >= teleportMonster.ReactionTime && Time.time >= nextShotTime) {
                    teleportMonster.ShootAtPlayer();
                    nextShotTime = Time.time + teleportMonster.FireInterval;
                }
            } 
            else {
                continuousSeenTime = 0f;
                timeSinceSeenPlayer += Time.deltaTime;
            }
        }
    }
}