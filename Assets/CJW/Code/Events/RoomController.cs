using System.Collections;
using System.Collections.Generic;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEngine.AI;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>재사용하는 기본 방 하나에 붙이는 IRoomContext 구현.</summary>
    public class RoomController : MonoBehaviour, IRoomContext
    {
        [Tooltip("비워두면 씬에서 자동으로 찾는다.")]
        [SerializeField] PlayerCharacter player;

        [Header("조명")]
        [SerializeField] Light[] roomLights;
        [Tooltip("조명이 꺼졌을 때 주변광 배율.")]
        [SerializeField, Range(0f, 1f)] float darkAmbientScale = 0.05f;

        [Header("좀비")]
        [Tooltip("NavMesh가 구워진 방이어야 추적한다.")]
        [SerializeField] EnemyCharacter defaultZombiePrefab;
        [SerializeField] Transform[] zombieSpawnPoints;

        [Header("기믹")]
        [SerializeField] RoomButton[] buttons;
        [SerializeField] HealZone healZone;

        readonly List<EnemyCharacter> spawnedZombies = new();
        float defaultAmbientIntensity;
        Color defaultAmbientLight;
        // CharacterBase 체력은 정수라서, 회복 구역처럼 프레임마다 들어오는 소수 변화량을 모았다가 적용한다.
        float pendingHealth;

        public PlayerCharacter Player => player;
        public int AliveEnemyCount
        {
            get { int count = 0; foreach (var enemy in spawnedZombies) if (enemy && !enemy.IsDead) count++; return count; }
        }
        public bool IsPlayerAlive => player && !player.IsDead;

        public bool SpendItem(ItemKind kind, int amount) => player.Inventory.Consume(kind, amount);

        public void GiveItem(ItemKind kind, int amount)
        {
            if (!IsPlayerAlive || amount <= 0) return;
            int added = player.Inventory.Add(kind, amount);
            if (PlayerInventory.IsWeapon(kind) && added > 0) player.OnWeaponCollected(kind);
            player.Inventory.Notify(added > 0 ? $"{PlayerInventory.Label(kind)} +{added}" : "Inventory full / already owned");
        }

        void Awake()
        {
            if (!player) player = FindFirstObjectByType<PlayerCharacter>();
            defaultAmbientIntensity = RenderSettings.ambientIntensity;
            defaultAmbientLight = RenderSettings.ambientLight;
            ResetRoom();
        }

        public void ChangeHealth(float amount)
        {
            pendingHealth += amount;
            int whole = (int)pendingHealth;
            if (whole == 0) return;

            pendingHealth -= whole;
            if (whole > 0) player.Heal(whole);
            else player.TakeDamage(-whole);
        }

        public void SetLights(bool on)
        {
            foreach (var l in roomLights) l.enabled = on;
            // Skybox 모드는 ambientIntensity, Flat(단색) 모드는 ambientLight 색을 쓰므로 둘 다 조절한다.
            float scale = on ? 1f : darkAmbientScale;
            RenderSettings.ambientIntensity = defaultAmbientIntensity * scale;
            RenderSettings.ambientLight = defaultAmbientLight * scale;
        }

        public void SpawnZombies(EnemyCharacter prefab, int count)
        {
            if (prefab == null) prefab = defaultZombiePrefab;
            if (prefab == null || zombieSpawnPoints.Length == 0)
            {
                Debug.LogWarning("RoomController: 좀비 프리팹 또는 스폰 위치가 없습니다.", this);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                int index = spawnedZombies.Count;
                var point = zombieSpawnPoints[index % zombieSpawnPoints.Length];
                int ring = index / zombieSpawnPoints.Length;
                // Keep larger waves inside the side walls instead of widening every row.
                float lateralOffset = ring == 0 ? 0 : (ring % 2 == 0 ? 1.6f : -1.6f);
                Vector3 position = point.position + new Vector3(lateralOffset, 0, -ring * 1.8f);
                if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas)) position = hit.position;
                var zombie = Instantiate(prefab, position, point.rotation, transform);
                zombie.SetTarget(player);
                spawnedZombies.Add(zombie);
            }
        }

        public void ActivateButtons(int count)
        {
            if (count > buttons.Length)
                Debug.LogWarning($"RoomController: 버튼이 {count}개 필요하지만 {buttons.Length}개만 배치되어 있습니다.", this);

            for (int i = 0; i < buttons.Length; i++) buttons[i].SetActive(i < count);
        }

        public int PressedButtonCount
        {
            get
            {
                int pressed = 0;
                foreach (var b in buttons) if (b.IsPressed) pressed++;
                return pressed;
            }
        }

        public void SetHealZoneActive(bool active)
        {
            if (healZone != null) healZone.gameObject.SetActive(active);
        }

        public bool IsPlayerInHealZone => healZone != null && healZone.isActiveAndEnabled && healZone.PlayerInside;

        public void ResetRoom()
        {
            SetLights(true);
            foreach (var z in spawnedZombies) if (z != null) StartCoroutine(DestroyNextFrame(z.gameObject));
            spawnedZombies.Clear();
            foreach (var b in buttons) b.SetActive(false);
            SetHealZoneActive(false);
            pendingHealth = 0f;
        }

        // 적의 공격(그 적의 Update 안)으로 플레이어가 죽으면 같은 프레임에 ResetRoom이 불린다.
        // 바로 Destroy하면 그 적의 NavMeshAgent가 즉시 무효가 되어, 이어지는 EnemyCharacter.Update(김우진)가
        // isStopped를 호출하다 오류가 난다. 그래서 한 프레임 뒤에 지운다.
        static IEnumerator DestroyNextFrame(GameObject enemy)
        {
            yield return null;
            if (enemy) Destroy(enemy);
        }
    }
}
