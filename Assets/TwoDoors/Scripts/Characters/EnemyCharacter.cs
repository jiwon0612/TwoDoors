using UnityEngine;
using UnityEngine.AI;

namespace TwoDoors.Characters
{
    [RequireComponent(typeof(NavMeshAgent), typeof(CapsuleCollider))]
    public class EnemyCharacter : CharacterBase
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float detectionRadius = 14;
        [SerializeField] private float attackRange = 1.7f;
        [SerializeField] private int attackDamage = 12;
        [SerializeField] private float windup = 0.55f;
        [SerializeField] private float attackCooldown = 1.6f;
        private NavMeshAgent agent;
        private PlayerCharacter target;
        private float nextPath, nextAttack, hitAt;
        private bool attacking;
        public bool IsAttacking => attacking;
        public PlayerCharacter Target => target;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            if (!animator) animator = GetComponentInChildren<Animator>();
        }

        private void Start() => target = FindFirstObjectByType<PlayerCharacter>();
        public void SetTarget(PlayerCharacter player) => target = player;

        private void Update()
        {
            if (IsDead || !agent.enabled || !agent.isOnNavMesh) return;
            if (!target || target.IsDead)
            {
                agent.isStopped = true;
                SetMoving(false);
                attacking = false;
                return;
            }
            if (target.IsPaused) return;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (attacking)
            {
                if (Time.time >= hitAt)
                {
                    attacking = false;
                    if (distance <= attackRange + 0.3f && HasLineOfSight()) target.TakeDamage(attackDamage);
                }
                return;
            }
            if (Time.time < nextAttack) { agent.isStopped = true; SetMoving(false); return; }
            if (distance > detectionRadius) { agent.isStopped = true; SetMoving(false); return; }
            if (distance <= attackRange && HasLineOfSight())
            {
                agent.isStopped = true;
                Vector3 facing = target.transform.position - transform.position;
                facing.y = 0;
                if (facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(facing);
                SetMoving(false);
                attacking = true;
                hitAt = Time.time + windup;
                nextAttack = Time.time + attackCooldown;
                if (animator) animator.SetTrigger("Attack");
            }
            else
            {
                agent.isStopped = false;
                if (Time.time >= nextPath)
                {
                    agent.SetDestination(target.transform.position);
                    nextPath = Time.time + 0.2f;
                }
                SetMoving(agent.velocity.sqrMagnitude > 0.01f);
            }
        }

        private bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 destination = target.transform.position + Vector3.up * 1.1f;
            Vector3 direction = destination - origin;
            return Physics.Raycast(origin, direction.normalized, out RaycastHit hit, direction.magnitude + 0.1f,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<PlayerCharacter>() == target;
        }

        private void SetMoving(bool moving)
        {
            if (animator) animator.SetBool("Moving", moving);
        }

        protected override void OnDeath()
        {
            attacking = false;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            agent.enabled = false;
            GetComponent<CapsuleCollider>().enabled = false;
            if (animator) { animator.SetBool("Dead", true); animator.ResetTrigger("Attack"); }
        }
    }
}
