using System.Collections.Generic;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>
    /// 문을 통과했을 때 다음 방에 적용되는 이벤트의 베이스.
    /// 모든 이벤트에 공통인 "입장 효과"(체력, 조명, 좀비, 단계 스킵, 탈출)는 여기서 처리하고,
    /// 하위 클래스는 "클리어 조건"만 정의한다.
    /// ScriptableObject 에셋은 여러 번 재사용되므로 런타임 상태를 필드에 저장하지 않는다.
    /// 경과 시간 등 상태는 RoomEventRunner가 들고 있다.
    /// </summary>
    public abstract class RoomEvent : ScriptableObject
    {
        [System.Serializable]
        public struct EnemyGroup { public EnemyCharacter prefab; [Min(1)] public int count; }
        [System.Serializable]
        public struct ItemGrant { public ItemKind kind; [Min(1)] public int amount; }
        [SerializeField] EnemyGroup[] enemyGroups = new EnemyGroup[0];
        [SerializeField] ItemGrant[] entryItems = new ItemGrant[0];
        [SerializeField] ItemGrant[] clearItems = new ItemGrant[0];
        public int EnemyCount
        {
            get { int count = zombieCount; foreach (var group in enemyGroups) count += group.count; return count; }
        }
        [Header("입장 효과")]
        [Tooltip("양수 = 회복, 음수 = 감소. 방에 들어가는 즉시 적용되고 다음 단계에도 유지된다.")]
        [SerializeField] int healthChange;
        [Tooltip("이 방에서만 조명이 꺼진다.")]
        [SerializeField] bool lightsOff;
        [SerializeField, Min(0)] int zombieCount;
        [Tooltip("비워두면 방의 기본 좀비 프리팹을 사용한다. 특수 좀비(시선 등)는 여기에 지정.")]
        [SerializeField] EnemyCharacter zombiePrefab;

        [Header("클리어 후 결과")]
        [SerializeField] bool skipNextStage;
        [SerializeField] bool escapeOnClear;

        [Header("문 위에 표시할 문구")]
        [Tooltip("비워두면 위 설정으로 자동 생성된다.")]
        [SerializeField, TextArea(2, 6)] string customDescription;

        public int HealthChange => healthChange;
        public bool LightsOff => lightsOff;
        public int ZombieCount => zombieCount;
        public EnemyCharacter ZombiePrefab => zombiePrefab;
        public bool SkipNextStage => skipNextStage;
        public bool EscapeOnClear => escapeOnClear;

        /// <summary>HUD에 남은 시간을 표시할 제한 시간. 0이면 타이머 없음.</summary>
        public virtual float Duration => 0f;

        // Only runtime clones are adjusted; catalog assets remain unchanged between runs.
        public RoomEvent CreateForDifficulty(int clearedRooms)
        {
            var copy = Instantiate(this);
            copy.name = name;
            copy.hideFlags = HideFlags.DontSave;
            copy.enemyGroups = (EnemyGroup[])enemyGroups.Clone();
            copy.customDescription = "";
            copy.ApplyDifficulty(Mathf.Clamp(clearedRooms, 0, 8));
            return copy;
        }

        protected virtual void ApplyDifficulty(int clearedRooms)
        {
            if (healthChange < 0) healthChange = -(20 + clearedRooms * 4 + (entryItems.Length > 0 ? 5 : 0));
            if (enemyGroups.Length == 0) return;
            bool kill = this is SurvivalEvent survival && survival.RequireAllKills;
            float Weight(EnemyCharacter enemy) => enemy is GolemEnemy ? (kill ? 3.5f : 1.8f) : enemy is ZombieEnemy ? 1.6f : 1f;
            float original = 0;
            foreach (var group in enemyGroups) original += group.count * Weight(group.prefab);
            float budget = (6f + clearedRooms * 1.3f) * (lightsOff ? 0.85f : 1f);
            if (this is HealZoneEvent) budget *= 1.2f;
            if (clearItems.Length > 0) budget *= 1.1f;
            for (int i = 0; i < enemyGroups.Length; i++)
                enemyGroups[i].count = Mathf.Max(1, Mathf.RoundToInt(enemyGroups[i].count * budget / Mathf.Max(1f, original)));
            // Keep every archetype present while respecting the arena's tested spawn capacity.
            while (EnemyCount > 14)
            {
                int largest = 0;
                for (int i = 1; i < enemyGroups.Length; i++)
                    if (enemyGroups[i].count > enemyGroups[largest].count) largest = i;
                if (enemyGroups[largest].count <= 1) break;
                enemyGroups[largest].count--;
            }
        }

        public virtual void Begin(IRoomContext ctx)
        {
            if (healthChange != 0) ctx.ChangeHealth(healthChange);
            if (!ctx.IsPlayerAlive) return;
            ctx.SetLights(!lightsOff);
            if (zombieCount > 0) ctx.SpawnZombies(zombiePrefab, zombieCount);
            foreach (var group in enemyGroups) if (group.count > 0) ctx.SpawnZombies(group.prefab, group.count);
            foreach (var item in entryItems) ctx.GiveItem(item.kind, item.amount);
        }

        public virtual void OnCleared(IRoomContext ctx)
        {
            foreach (var item in clearItems) ctx.GiveItem(item.kind, item.amount);
        }

        public virtual void Tick(IRoomContext ctx, float elapsed, float deltaTime) { }

        public abstract bool IsCleared(IRoomContext ctx, float elapsed);

        /// <summary>클리어/중단 시 호출. 조명·좀비 등 방 한정 효과를 되돌린다. 체력은 유지된다.</summary>
        public virtual void End(IRoomContext ctx)
        {
            ctx.ResetRoom();
        }

        public string GetDescription()
        {
            if (!string.IsNullOrWhiteSpace(customDescription)) return customDescription;

            var lines = new List<string>();
            if (healthChange > 0) lines.Add($"체력 {healthChange} 회복");
            else if (healthChange < 0) lines.Add($"체력 {-healthChange} 감소");
            if (lightsOff) lines.Add("방의 조명이 꺼짐");
            if (zombieCount > 0) lines.Add($"좀비 {zombieCount}마리 등장");
            foreach (var group in enemyGroups)
                lines.Add($"{(group.prefab is GolemEnemy ? "골렘" : group.prefab is MannequinEnemy ? "목각인형" : "좀비")} {group.count}마리 등장");
            foreach (var item in entryItems) lines.Add($"입장 보급: {ItemName(item.kind)} {item.amount}개");
            AppendConditionDescription(lines);
            foreach (var item in clearItems) lines.Add($"통과 보상: {ItemName(item.kind)} {item.amount}개 (보유 한도까지)");
            if (skipNextStage) lines.Add("다음 선택 단계 하나 건너뛰기");
            if (escapeOnClear) lines.Add("탈출 성공");
            return string.Join("\n", lines);
        }

        protected abstract void AppendConditionDescription(List<string> lines);
        public static string ItemName(ItemKind kind) => kind switch
        {
            ItemKind.Ammo => "예비 탄약", ItemKind.Medkit => "회복약", ItemKind.Pistol => "권총",
            ItemKind.Flashlight => "손전등", _ => "칼"
        };
    }
}
