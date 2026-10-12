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
        GUIStyle gameOverTitle, gameOverOption;
        bool gameOverShown;

        void Start() => player = FindFirstObjectByType<PlayerCharacter>();

        bool Finished => stages.Phase is StagePhase.GameOver or StagePhase.Escaped;
        bool Paused => player && player.IsPaused;

        void Update()
        {
            if (stages.Phase == StagePhase.GameOver && !gameOverShown)
            {
                gameOverShown = true;
                // The result screen owns the entire display; hide all gameplay UI.
                foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    canvas.enabled = false;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (Finished && keyboard.rKey.wasPressedThisFrame) stages.Restart();
            if ((Finished || Paused) && keyboard.qKey.wasPressedThisFrame) TitleScreen.LoadTitle();
        }

        void OnGUI()
        {
            if (stages.Phase == StagePhase.GameOver)
            {
                DrawGameOver();
                return;
            }
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };

            string text = $"단계 {stages.StageNumber} / {stages.TotalStages}";
            if (stages.CurrentStage != null) text += $"  ·  {stages.CurrentStage.Title}";
            if (runner.HasTimer) text += $"\n생존 {runner.RemainingTime:0.0}초";
            else if (runner.Current is SurvivalEvent combat && combat.RequireAllKills) text += "\n섬멸 · 적 전원 처치";
            else if (stages.Phase == StagePhase.Running) text += "\n섬멸 · 적 전원 처치";
            else if (stages.Phase == StagePhase.Choosing) text += "\n문을 선택하세요";
            if (runner.IsRunning && runner.Current.EnemyCount > 0)
                text += $"\n남은 적 {runner.AliveEnemyCount}마리" +
                    " · 전원 처치 필수";

            if (stages.Phase == StagePhase.Escaped) text = "탈출 성공!\nR  다시 시작    Q  시작 화면";
            if (Paused && !Finished) text += "\n\nQ  시작 화면으로";

            GUI.Label(new Rect(20, 20, 700, 160), text, style);
        }

        void DrawGameOver()
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.025f, 0.03f, 0.04f, 1f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            gameOverTitle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            gameOverOption ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            gameOverTitle.fontSize = Mathf.RoundToInt(52 * scale);
            gameOverOption.fontSize = Mathf.RoundToInt(26 * scale);
            gameOverTitle.normal.textColor = gameOverOption.normal.textColor = Color.white;
            float center = Screen.height * 0.5f;
            GUI.Label(new Rect(0, center - 105 * scale, Screen.width, 76 * scale), "게임오버", gameOverTitle);
            GUI.Label(new Rect(0, center + 2 * scale, Screen.width, 40 * scale), "R  다시 시작하기", gameOverOption);
            GUI.Label(new Rect(0, center + 52 * scale, Screen.width, 40 * scale), "Q  타이틀 가기", gameOverOption);
            GUI.color = oldColor;
        }
    }
}
