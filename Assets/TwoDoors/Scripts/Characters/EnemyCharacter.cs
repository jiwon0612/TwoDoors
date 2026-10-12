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
        private float recoverUntil, attackStarted;
        private Transform visual;
        private Quaternion visualRotation;
        private Vector3 visualPosition;
        float hitFlashUntil, staggerUntil, nextStagger;
        Renderer[] hitRenderers;
        MaterialPropertyBlock hitBlock;

        public void ReactToHit()
        {
            hitFlashUntil = Time.time + 0.09f;
            if (Time.time >= nextStagger) { staggerUntil = Time.time + 0.1f; nextStagger = Time.time + 0.4f; }
        }
        private bool Melee => this is ZombieEnemy || this is GolemEnemy;
        public bool IsRecovering => Melee && Time.time < recoverUntil;
        public bool IsAttacking => attacking;
        public PlayerCharacter Target => target;

        protected override void Awake()
        {
            base.Awake();
            agent = GetComponent<NavMeshAgent>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            visual = transform.Find("Visual");
            if (visual) { visualRotation = visual.localRotation; visualPosition = visual.localPosition; }
            hitRenderers = GetComponentsInChildren<Renderer>();
            hitBlock = new MaterialPropertyBlock();
            if (!GetComponent<EnemyHealthBar>()) gameObject.AddComponent<EnemyHealthBar>();
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
            if (target.IsPaused) { agent.isStopped = true; return; }
            if (!attacking && Time.time < staggerUntil) { agent.isStopped = true; SetMoving(false); return; }
            if (IsRecovering) { agent.isStopped = true; SetMoving(false); return; }
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (attacking && !CanAttackWhilePositioned(distance))
            {
                attacking = false;
                nextPath = 0f;
                if (animator) animator.ResetTrigger("Attack");
            }
            if (attacking)
            {
                if (Time.time >= hitAt)
                {
                    attacking = false;
                    var direction = target.transform.position - transform.position;
                    direction.y = 0;
                    // Keep the close-range attack trigger, but use a wider impact sweep.
                    // During windup walking covers 2.28/3.15m, sprinting covers 6.37/8.82m.
                    float impactRange = this is ZombieEnemy ? 4.6f : this is GolemEnemy ? 4.5f : attackRange + 0.3f;
                    if (distance <= impactRange && (!Melee || Vector3.Dot(transform.forward, direction.normalized) > -0.5f) && HasLineOfSight()) PerformAttack(attackDamage);
                    if (Melee)
                    {
                        recoverUntil = Time.time + 1f;
                        nextAttack = Mathf.Max(nextAttack, recoverUntil);
                        agent.isStopped = true;
                    }
                }
                return;
            }
            if (Time.time >= nextAttack && distance <= attackRange && CanAttackWhilePositioned(distance) && HasLineOfSight())
            {
                agent.isStopped = true;
                Vector3 facing = target.transform.position - transform.position;
                facing.y = 0;
                if (facing.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(facing);
                SetMoving(false);
                attacking = true;
                attackStarted = Time.time;
                hitAt = Time.time + windup;
                nextAttack = Time.time + attackCooldown;
                if (animator) animator.SetTrigger("Attack");
            }
            else
            {
                agent.isStopped = false;
                if (Time.time >= nextPath)
                {
                    agent.SetDestination(MovementDestination(distance));
                    nextPath = Time.time + 0.1f;
                }
                SetMoving(agent.velocity.sqrMagnitude > 0.01f);
            }
        }

        protected virtual void PerformAttack(int damage) => target.TakeDamage(damage);

        private void LateUpdate()
        {
            foreach (var renderer in hitRenderers)
            {
                if (!renderer || !renderer.sharedMaterial || !renderer.sharedMaterial.HasProperty("_BaseColor")) continue;
                renderer.GetPropertyBlock(hitBlock);
                hitBlock.SetColor("_BaseColor", Time.time < hitFlashUntil ? new Color(3, 1.4f, 0.6f) : renderer.sharedMaterial.GetColor("_BaseColor"));
                renderer.SetPropertyBlock(hitBlock);
            }
            if (!Melee || !visual || IsDead) return;
            float tilt = 0;
            if (attacking)
            {
                float progress = Mathf.Clamp01((Time.time - attackStarted) / windup);
                tilt = progress < 0.8f ? Mathf.Lerp(0, -22, progress / 0.8f) : Mathf.Lerp(-22, 28, (progress - 0.8f) / 0.2f);
            }
            else if (IsRecovering)
            {
                float elapsed = 1f - (recoverUntil - Time.time);
                tilt = Mathf.Lerp(28, 0, elapsed);
            }
            // Tilt in the agent's forward direction, independent of imported model orientation.
            visual.localRotation = Quaternion.Euler(tilt, 0, 0) * visualRotation;
            visual.localPosition = Quaternion.Euler(tilt, 0, 0) * visualPosition;
        }

        protected virtual bool CanAttackWhilePositioned(float distance) => true;

        protected virtual Vector3 MovementDestination(float distance) => target.transform.position;

        protected bool HasLineOfSight()
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
