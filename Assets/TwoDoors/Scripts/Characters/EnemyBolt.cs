using System;
using UnityEngine;

namespace TwoDoors.Characters
{
    public sealed class EnemyBolt : MonoBehaviour
    {
        EnemyCharacter owner;
        PlayerCharacter target;
        Vector3 direction;
        int damage;
        float remaining = 2f;

        public static void Launch(EnemyCharacter owner, PlayerCharacter target, Vector3 origin, Vector3 direction, int damage)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "MannequinBolt";
            go.transform.SetParent(owner.transform, true);
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 0.18f;
            go.GetComponent<Collider>().enabled = false;
            var bolt = go.AddComponent<EnemyBolt>();
            bolt.owner = owner; bolt.target = target; bolt.direction = direction; bolt.damage = damage;
            bolt.worldPosition = origin; bolt.worldPositionInitialized = true;
            var material = Resources.Load<Material>("Combat/EnemyBolt");
            if (material) go.GetComponent<Renderer>().sharedMaterial = material;
        }

        void Update()
        {
            if (!owner || owner.IsDead || !target || target.IsDead) { Destroy(gameObject); return; }
            if (target.IsPaused) return;
            // Compensate for owner movement: keep the projectile's flight in world space.
            transform.position = worldPositionInitialized ? worldPosition : transform.position;
            float step = 12f * Time.deltaTime;
            var hits = Physics.SphereCastAll(transform.position, 0.09f, direction, step, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<EnemyCharacter>() == owner) continue;
                if (hit.collider.GetComponentInParent<PlayerCharacter>() == target) target.TakeDamage(damage);
                Destroy(gameObject);
                return;
            }
            transform.position += direction * step;
            worldPosition = transform.position; worldPositionInitialized = true;
            remaining -= Time.deltaTime;
            if (remaining <= 0) Destroy(gameObject);
        }

        Vector3 worldPosition;
        bool worldPositionInitialized;
    }
}
