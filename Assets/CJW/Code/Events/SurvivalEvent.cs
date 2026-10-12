using System.Collections.Generic;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>
    /// 정해진 시간 동안 살아남으면 클리어. 대부분의 이벤트가 이 클래스 하나로 표현된다.
    /// 조명/좀비 수/특수 좀비 프리팹 조합으로 정전, 짧고 굵게, 시선 등을 구성한다.
    /// </summary>
    [CreateAssetMenu(menuName = "TwoDoors/Events/Survival", fileName = "SurvivalEvent")]
    public class SurvivalEvent : RoomEvent
    {
        [Header("클리어 조건")]
        [SerializeField, Range(0f, 40f)] float surviveTime = 15f;
        [SerializeField] bool requireAllKills;

        public bool RequireAllKills => true;
        protected override void ApplyDifficulty(int clearedRooms)
        {
            base.ApplyDifficulty(clearedRooms);
            requireAllKills = true;
        }
        public override float Duration => 0f;

        public override bool IsCleared(IRoomContext ctx, float elapsed) =>
            EnemyCount > 0 && ctx.AliveEnemyCount == 0;

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add("적 전원 처치해야 통과");
        }
    }
}
