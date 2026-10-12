using TwoDoors.Characters;
using TwoDoors.Events;
using TwoDoors.Stages;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TwoDoors.UI
{
    /// <summary>
    /// 정식 HUD가 붙기 전 테스트용 표시. 단계, 남은 시간, 결과를 보여준다.
    /// 결과 화면에서 R = 재시작, 일시정지(Esc)·결과 화면에서 Q = 시작 화면.
    /// 정식 UI가 들어오면 이 컴포넌트는 제거한다.
    /// </summary>
    public class StageDebugHud : MonoBehaviour
    {
        [SerializeField] StageManager stages;
        [SerializeField] RoomEventRunner runner;

        PlayerCharacter player;
        GUIStyle style;

        void Start() => player = FindFirstObjectByType<PlayerCharacter>();

        bool Finished => stages.Phase is StagePhase.GameOver or StagePhase.Escaped;
        bool Paused => player && player.IsPaused;

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (Finished && keyboard.rKey.wasPressedThisFrame) stages.Restart();
            if ((Finished || Paused) && keyboard.qKey.wasPressedThisFrame) TitleScreen.LoadTitle();
        }

        void OnGUI()
        {
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };

            string text = $"단계 {stages.StageNumber} / {stages.TotalStages}";
            if (stages.CurrentStage != null) text += $"  ·  {stages.CurrentStage.Title}";
            if (runner.HasTimer) text += $"\n생존 {runner.RemainingTime:0.0}초";
            else if (runner.Current is SurvivalEvent combat && combat.RequireAllKills) text += "\n섬멸 · 적 전원 처치";
            else if (stages.Phase == StagePhase.Running) text += "\n진행 중";
            else if (stages.Phase == StagePhase.Choosing) text += "\n문을 선택하세요";
            if (runner.IsRunning && runner.Current.EnemyCount > 0)
                text += $"\n남은 적 {runner.AliveEnemyCount}마리" +
                    (runner.Current is SurvivalEvent fight && fight.RequireAllKills ? " · 전원 처치 필수" : " · 전원 처치로 조기 통과");

            if (stages.Phase == StagePhase.Escaped) text = "탈출 성공!\nR  다시 시작    Q  시작 화면";
            if (stages.Phase == StagePhase.GameOver) text = "사망\nR  다시 시작    Q  시작 화면";
            if (Paused && !Finished) text += "\n\nQ  시작 화면으로";

            GUI.Label(new Rect(20, 20, 700, 160), text, style);
        }
    }
}
