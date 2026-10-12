using System;
using TwoDoors.Characters;
using TwoDoors.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TwoDoors.Stages
{
    public enum DoorColor { Red, Blue }

    public enum StagePhase
    {
        /// <summary>문이 열려 있고 플레이어의 선택을 기다리는 중.</summary>
        Choosing,
        /// <summary>선택한 이벤트 진행 중. 문은 잠겨 있다.</summary>
        Running,
        Escaped,
        GameOver,
    }

    /// <summary>
    /// 단계 진행 전체를 관리한다.
    /// 문 통과 → 플레이어를 방 시작 위치로 이동 → 이벤트 실행 → 클리어되면 다음 단계 문 열기.
    /// 방은 하나를 재사용하므로 "다음 방"은 같은 방에 이벤트를 새로 적용하는 것이다.
    /// </summary>
    public class StageManager : MonoBehaviour
    {
        [Tooltip("진행 순서대로. 마지막 단계의 이벤트를 살아서 클리어하면 탈출 성공.")]
        [SerializeField] StageChoice[] stages;

        [SerializeField] RoomController room;
        [SerializeField] RoomEventRunner runner;
        [SerializeField] StageDoor redDoor;
        [SerializeField] StageDoor blueDoor;
        [Tooltip("문을 통과하면 플레이어가 이동할 방 시작 위치.")]
        [SerializeField] Transform playerSpawnPoint;

        public StagePhase Phase { get; private set; }
        public int StageIndex { get; private set; }
        /// <summary>HUD 표시용 1부터 시작하는 단계 번호.</summary>
        public int StageNumber => StageIndex + 1;
        public int TotalStages => stages.Length;
        public StageChoice CurrentStage => StageIndex < stages.Length ? stages[StageIndex] : null;

        /// <summary>새 단계의 문이 열렸을 때. (단계 번호, 선택지)</summary>
        public event Action<int, StageChoice> StageOpened;
        /// <summary>플레이어가 문을 골랐을 때. (고른 문, 적용될 이벤트)</summary>
        public event Action<DoorColor, RoomEvent> DoorChosen;
        public event Action Escaped;
        public event Action GameOver;

        void Start()
        {
            if (stages == null || stages.Length == 0)
            {
                Debug.LogError("StageManager: stages가 비어 있습니다.", this);
                enabled = false;
                return;
            }

            runner.Completed += OnEventCompleted;
            room.Player.Died += OnPlayerDied;
            OpenStage(0);
        }

        void OnDestroy()
        {
            if (runner) runner.Completed -= OnEventCompleted;
            if (room && room.Player) room.Player.Died -= OnPlayerDied;
        }

        /// <summary>StageDoor가 플레이어 통과 시 호출한다.</summary>
        public void ChooseDoor(DoorColor color)
        {
            if (Phase != StagePhase.Choosing) return;

            var roomEvent = color == DoorColor.Red ? CurrentStage.RedDoor : CurrentStage.BlueDoor;
            Phase = StagePhase.Running;
            CloseDoors();
            MovePlayerToSpawn();
            DoorChosen?.Invoke(color, roomEvent);
            runner.Run(roomEvent);
        }

        /// <summary>게임오버·탈출 화면의 "다시 시작" 버튼용. 체력까지 처음 상태로 돌아간다.</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnEventCompleted(RoomEvent finished)
        {
            if (Phase != StagePhase.Running) return;

            int next = StageIndex + (finished.SkipNextStage ? 2 : 1);
            if (finished.EscapeOnClear || next >= stages.Length)
            {
                Phase = StagePhase.Escaped;
                Escaped?.Invoke();
                return;
            }

            OpenStage(next);
        }

        void OnPlayerDied(CharacterBase _)
        {
            if (Phase == StagePhase.Escaped) return;

            Phase = StagePhase.GameOver;
            CloseDoors();
            GameOver?.Invoke();
        }

        void OpenStage(int index)
        {
            StageIndex = index;
            Phase = StagePhase.Choosing;
            redDoor.Open(CurrentStage.RedDoor.GetDescription());
            blueDoor.Open(CurrentStage.BlueDoor.GetDescription());
            StageOpened?.Invoke(StageNumber, CurrentStage);
        }

        void CloseDoors()
        {
            redDoor.Close();
            blueDoor.Close();
        }

        void MovePlayerToSpawn()
        {
            if (!playerSpawnPoint) return;

            // CharacterController가 켜져 있으면 transform 이동이 덮어씌워진다.
            var controller = room.Player.GetComponent<CharacterController>();
            controller.enabled = false;
            room.Player.transform.SetPositionAndRotation(playerSpawnPoint.position, playerSpawnPoint.rotation);
            controller.enabled = true;
        }
    }
}
