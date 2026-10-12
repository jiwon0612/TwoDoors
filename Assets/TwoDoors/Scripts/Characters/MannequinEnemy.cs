using UnityEngine;
using UnityEngine.AI;

namespace TwoDoors.Characters
{
    /// <summary>Tracks the player, keeps firing distance and launches dodgeable bolts.</summary>
    public sealed class MannequinEnemy : EnemyCharacter
    {
        NavMeshAgent movement;

        protected override void Awake()
        {
            base.Awake();
            movement = GetComponent<NavMeshAgent>();
        }

        protected override bool CanAttackWhilePositioned(float distance) =>
            distance >= 5f && distance <= 11f && movement.velocity.sqrMagnitude < 0.01f;

        protected override void PerformAttack(int damage)
        {
            var origin = transform.position + Vector3.up * 1.2f;
            var direction = (Target.transform.position + Vector3.up * 1.1f - origin).normalized;
            EnemyBolt.Launch(this, Target, origin + direction * 0.6f, direction, damage);
        }

        protected override Vector3 MovementDestination(float distance)
        {
            if (!HasLineOfSight() || distance > 11f) return Target.transform.position;
            if (distance >= 5f) return transform.position;
            var away = transform.position - Target.transform.position;
            away.y = 0;
            if (NavMesh.SamplePosition(transform.position + away.normalized * 3f, out var hit, 1.5f, NavMesh.AllAreas))
                return hit.position;
            return transform.position;
        }
    }
}
