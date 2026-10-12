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
        [SerializeField, Min(0f)] float surviveTime = 15f;

        public override float Duration => surviveTime;

        public override bool IsCleared(IRoomContext ctx, float elapsed) => elapsed >= surviveTime;

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add($"{surviveTime:0.#}초 동안 생존");
        }
    }
}
