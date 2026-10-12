using System.Collections.Generic;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>방 안의 버튼을 모두 누르면 클리어. 시간 제한 없음. 예: 비상 스위치(빨강).</summary>
    [CreateAssetMenu(menuName = "TwoDoors/Events/Buttons", fileName = "ButtonEvent")]
    public class ButtonEvent : RoomEvent
    {
        [Header("클리어 조건")]
        [SerializeField, Min(1)] int buttonCount = 3;

        public override void Begin(IRoomContext ctx)
        {
            base.Begin(ctx);
        }

        public override bool IsCleared(IRoomContext ctx, float elapsed) => EnemyCount > 0 && ctx.AliveEnemyCount == 0;

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add("적 전원 처치해야 통과");
        }
    }
}
