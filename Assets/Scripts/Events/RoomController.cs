using System.Collections.Generic;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>재사용하는 기본 방 하나에 붙이는 IRoomContext 구현.</summary>
    public class RoomController : MonoBehaviour, IRoomContext
    {
        [Header("조명")]
        [SerializeField] Light[] roomLights;
        [SerializeField, Range(0f, 1f)] float darkAmbientIntensity = 0.05f;

        [Header("좀비")]
        [SerializeField] GameObject defaultZombiePrefab;
        [SerializeField] Transform[] zombieSpawnPoints;

        [Header("기믹")]
        [SerializeField] RoomButton[] buttons;
        [SerializeField] HealZone healZone;

        readonly List<GameObject> spawnedZombies = new();
        float defaultAmbientIntensity;

        void Awake()
        {
            defaultAmbientIntensity = RenderSettings.ambientIntensity;
            ResetRoom();
        }

        public void ChangeHealth(float amount)
        {
            // TODO: 플레이어 체력 컴포넌트가 들어오면 연결
        }

        public void SetLights(bool on)
        {
            foreach (var l in roomLights) l.enabled = on;
            RenderSettings.ambientIntensity = on ? defaultAmbientIntensity : darkAmbientIntensity;
        }

        public void SpawnZombies(GameObject prefab, int count)
        {
            if (prefab == null) prefab = defaultZombiePrefab;
            if (prefab == null || zombieSpawnPoints.Length == 0)
            {
                Debug.LogWarning("RoomController: 좀비 프리팹 또는 스폰 위치가 없습니다.", this);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                var point = zombieSpawnPoints[i % zombieSpawnPoints.Length];
                spawnedZombies.Add(Instantiate(prefab, point.position, point.rotation, transform));
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
            foreach (var z in spawnedZombies) if (z != null) Destroy(z);
            spawnedZombies.Clear();
            foreach (var b in buttons) b.SetActive(false);
            SetHealZoneActive(false);
        }
    }
}
