using System;
using TwoDoors.Characters;
using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>
    /// 현재 방의 이벤트 하나를 실행한다. 문 통과 시 Run(choice.RedDoor) 처럼 호출.
    /// 클리어되면 Completed가 발생하고, 단계 진행 쪽에서 SkipNextStage / EscapeOnClear를 보고 다음 단계를 정한다.
    /// </summary>
    public class RoomEventRunner : MonoBehaviour
    {
        [SerializeField] RoomController room;

        public RoomEvent Current { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsRunning => Current != null;
        public bool HasTimer => IsRunning && Current.Duration > 0f;
        public float RemainingTime => HasTimer ? Mathf.Max(0f, Current.Duration - Elapsed) : 0f;
        public int AliveEnemyCount => room.AliveEnemyCount;

        public event Action<RoomEvent> Started;
        public event Action<RoomEvent> Completed;

        bool beginning;

        // RoomController.Awake에서 플레이어를 찾은 뒤에 구독해야 하므로 Start에서 한다.
        void Start() => room.Player.Died += OnPlayerDied;

        void OnDestroy()
        {
            if (room && room.Player) room.Player.Died -= OnPlayerDied;
        }

        // Begin() 도중 사망하면 남은 입장 효과가 리셋 이후에 적용되므로, Begin이 끝난 뒤에 정리한다.
        void OnPlayerDied(CharacterBase _)
        {
            if (!beginning) Stop();
        }

        public void Run(RoomEvent roomEvent)
        {
            Stop();
            Current = roomEvent;
            Elapsed = 0f;

            beginning = true;
            roomEvent.Begin(room);
            beginning = false;

            if (room.Player.IsDead)
            {
                Stop();
                return;
            }

            Started?.Invoke(roomEvent);
            if (roomEvent.IsCleared(room, 0f)) Complete();
        }

        /// <summary>사망·일시정지 후 재시작 등으로 이벤트를 중단. Completed는 발생하지 않는다.</summary>
        public void Stop()
        {
            if (Current == null) return;
            Current.End(room);
            Current = null;
        }

        void Update()
        {
            if (Current == null) return;

            Elapsed += Time.deltaTime;
            Current.Tick(room, Elapsed, Time.deltaTime);
            if (Current != null && Current.IsCleared(room, Elapsed)) Complete();
        }

        void Complete()
        {
            var finished = Current;
            finished.OnCleared(room);
            finished.End(room);
            Current = null;
            Completed?.Invoke(finished);
        }
    }
}
