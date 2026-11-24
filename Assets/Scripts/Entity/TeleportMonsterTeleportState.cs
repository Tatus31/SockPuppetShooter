using UnityEngine;

namespace Entity {
    public class TeleportMonsterTeleportState : TeleportMonsterBaseState {
        float timer;
        bool hasTeleported;

        public bool IsFinished { get; private set; }

        public TeleportMonsterTeleportState(TeleportMonster teleportMonster, Animator animator) : base(teleportMonster, animator) { }

        public override void OnEnter() {
            PlayAnimation(TeleportHash);
            timer = 0f;
            hasTeleported = false;
            IsFinished = false;
            teleportMonster.StartTeleportCooldown(teleportMonster.TeleportWindUp + teleportMonster.TeleportRecover);
        }

        public override void Update() {
            timer += Time.deltaTime;

            if (!hasTeleported && timer >= teleportMonster.TeleportWindUp) {
                teleportMonster.TeleportToRandomPoint();
                hasTeleported = true;
            }

            if (hasTeleported && timer >= teleportMonster.TeleportWindUp + teleportMonster.TeleportRecover)
                IsFinished = true;
        }
    }
}