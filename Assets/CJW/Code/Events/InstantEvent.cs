using System.Collections.Generic;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>입장 효과만 적용하고 바로 통과. 예: 통행료(빨강), 지름길(빨강), 마지막 거래(빨강).</summary>
    [CreateAssetMenu(menuName = "TwoDoors/Events/Instant", fileName = "InstantEvent")]
    public class InstantEvent : RoomEvent
    {
        public override bool IsCleared(IRoomContext ctx, float elapsed) => EnemyCount > 0 && ctx.AliveEnemyCount == 0;

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add("입장 비용 적용 후 적 전원 처치해야 통과");
        }
    }
}
