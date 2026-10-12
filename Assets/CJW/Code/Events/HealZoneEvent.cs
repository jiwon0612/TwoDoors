using System.Collections.Generic;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>생존 이벤트 + 방 중앙 회복 구역 활성화. 예: 회복 구역(빨강).</summary>
    [CreateAssetMenu(menuName = "TwoDoors/Events/Heal Zone Survival", fileName = "HealZoneEvent")]
    public class HealZoneEvent : SurvivalEvent
    {
        [Header("회복 구역")]
        [SerializeField, Min(0f)] float healPerSecond = 3f;

        public override void Begin(IRoomContext ctx)
        {
            base.Begin(ctx);
            ctx.SetHealZoneActive(true);
        }

        public override void Tick(IRoomContext ctx, float elapsed, float deltaTime)
        {
            if (ctx.IsPlayerInHealZone) ctx.ChangeHealth(healPerSecond * deltaTime);
        }

        protected override void AppendConditionDescription(List<string> lines)
        {
            base.AppendConditionDescription(lines);
            lines.Add($"회복 구역에 서 있으면 초당 체력 {healPerSecond:0.#} 회복");
        }
    }
}
