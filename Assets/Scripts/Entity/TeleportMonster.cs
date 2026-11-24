using Entity.StateMachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Pool;

namespace Entity {
    [RequireComponent(typeof(NavMeshAgent))]
    public class TeleportMonster : Entity {
        [Header("References")]
        [SerializeField] NavMeshAgent navMeshAgent;
        [SerializeField] Animator animator;
        [SerializeField] Transform player;            
        [SerializeField] Transform firePoint;
        [SerializeField] Projectile projectilePrefab;

        [Header("Detection")]
        [SerializeField] float attackRadius = 15f;
        [SerializeField] float tooCloseRadius = 3f;
        [SerializeField] float eyeHeight = 1.5f;
        [SerializeField] float playerTargetHeight = 1f;
        [SerializeField] LayerMask sightMask = ~0;    

        [Header("Attack")]
        [SerializeField] float reactionTime = 0.4f;   
        [SerializeField] float fireInterval = 1.5f;
        [SerializeField] float loseSightTime = 4f;    
        [SerializeField] float turnSpeed = 8f;

        [Header("Teleport")]
        [SerializeField] float teleportWindUp = 0.5f;   
        [SerializeField] float teleportRecover = 0.5f;  
        [SerializeField] float teleportCooldown = 3f;   
        [SerializeField] float minTeleportDistance = 3f;

        StateMachine.StateMachine stateMachine;
        ObjectPool<Projectile> projectilePool;
        float nextTeleportTime;

        public float LoseSightTime => loseSightTime;
        public float ReactionTime => reactionTime;
        public float FireInterval => fireInterval;
        public float TeleportWindUp => teleportWindUp;
        public float TeleportRecover => teleportRecover;

        bool PlayerInAttackRadius => player != null && DistanceToPlayer() <= attackRadius;
        bool PlayerTooClose => player != null && DistanceToPlayer() <= tooCloseRadius;

        void Start() {
            if (navMeshAgent == null) 
                navMeshAgent = GetComponent<NavMeshAgent>();
            if (player == null) {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) 
                    player = p.transform;
            }

            if (projectilePrefab == null) {
#if UNITY_EDITOR
                Debug.LogError($"{name}: Projectile Prefab is null", this);
#endif
            }

            projectilePool = new ObjectPool<Projectile>(
                createFunc: () => Instantiate(projectilePrefab),
                actionOnGet: p => p.gameObject.SetActive(true),
                actionOnRelease: p => p.gameObject.SetActive(false),
                actionOnDestroy: p => { if (p) Destroy(p.gameObject); },
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 30);

            stateMachine = new StateMachine.StateMachine();

            var idleState = new TeleportMonsterIdleState(this, animator);
            var attackState = new TeleportMonsterAttackState(this, animator);
            var teleportState = new TeleportMonsterTeleportState(this, animator);

            At(idleState, attackState, new FuncPredicate(() => PlayerInAttackRadius));
            At(attackState, teleportState, new FuncPredicate(() => attackState.LostPlayer));
            At(teleportState, attackState, new FuncPredicate(() => teleportState.IsFinished));
            Any(teleportState, new FuncPredicate(() => PlayerTooClose && Time.time >= nextTeleportTime));

            stateMachine.SetState(idleState);
        }

        void At(IState from, IState to, IPredicate condition) => stateMachine.AddTransition(from, to, condition);
        void Any(IState to, IPredicate condition) => stateMachine.AddAnyTransition(to, condition);

        void Update() {
            stateMachine.Update();
        }

        void FixedUpdate() {
            stateMachine.FixedUpdate();
        }
        
        float DistanceToPlayer() => Vector3.Distance(transform.position, player.position);

        Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
        Vector3 PlayerTargetPosition => player.position + Vector3.up * playerTargetHeight;

        public bool CanSeePlayer() {
            if (player == null || DistanceToPlayer() > attackRadius) 
                return false;

            Vector3 toPlayer = PlayerTargetPosition - EyePosition;
            if (Physics.Raycast(EyePosition, toPlayer.normalized, out var hit, attackRadius, sightMask, QueryTriggerInteraction.Ignore))
                return hit.transform == player || hit.transform.IsChildOf(player);

            return false;
        }

        public void FacePlayer() {
            if (player == null) 
                return;
            
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) 
                return;
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
        }

        public void ShootAtPlayer() {
            if (projectilePrefab == null || player == null) 
                return;

            var projectile = projectilePool.Get();
            if (projectile == null) 
                return; 

            Vector3 origin = firePoint != null ? firePoint.position : EyePosition;
            Vector3 dir = (PlayerTargetPosition - origin).normalized;
            projectile.Init(origin, Quaternion.LookRotation(dir), projectilePool.Release);
        }

        public void StartTeleportCooldown(float teleportDuration) {
            nextTeleportTime = Time.time + teleportDuration + teleportCooldown;
        }

        public void TeleportToRandomPoint() {
            const int maxAttempts = 20;
            float minDistFromPlayer = tooCloseRadius * 1.5f;

            for (int i = 0; i < maxAttempts; i++) {
                Vector2 offset = Random.insideUnitCircle * attackRadius;
                Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);

                if (!NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
                    continue;
                
                if (Vector3.Distance(hit.position, transform.position) < minTeleportDistance) 
                    continue;
                
                if (player != null && Vector3.Distance(hit.position, player.position) < minDistFromPlayer)
                    continue;

                navMeshAgent.Warp(hit.position);
                return;
            }
        }

        void OnDestroy() {
            projectilePool?.Dispose();
        }

        void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, tooCloseRadius);
        }
    }
}