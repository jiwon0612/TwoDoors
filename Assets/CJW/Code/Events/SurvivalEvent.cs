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

        public bool RequireAllKills => requireAllKills;
        protected override void ApplyDifficulty(int clearedRooms)
        {
            base.ApplyDifficulty(clearedRooms);
            if (!requireAllKills) surviveTime = Mathf.Min(40f, 20f + clearedRooms * 2.5f);
        }
        public override float Duration => requireAllKills ? 0f : Mathf.Clamp(surviveTime, 0f, 40f);

        public override bool IsCleared(IRoomContext ctx, float elapsed) =>
            (EnemyCount > 0 && ctx.AliveEnemyCount == 0) || (!requireAllKills && elapsed >= Duration);

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add(requireAllKills ? "[섬멸] 적 전원 처치해야 통과 (시간 제한 없음)" :
                EnemyCount > 0 ? $"[생존] {Duration:0.#}초 버티기 (전원 처치 시 즉시 통과)" : $"[생존] {Duration:0.#}초 동안 생존");
        }
    }
}
