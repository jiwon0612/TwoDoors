using System.Collections;
using TMPro;
using TwoDoors.Events;
using TwoDoors.Stages;
using UnityEngine;

namespace TwoDoors.UI
{
    /// <summary>
    /// 화면 가운데 안내 카드.
    /// 방에 들어서면 "2단계 · 정전" + 고른 문의 효과를 띄우고, 생존 이벤트를 버티면 "생존"을 띄운다.
    /// </summary>
    public class StageBanner : MonoBehaviour
    {
        [SerializeField] StageManager stages;
        [SerializeField] RoomEventRunner runner;
        [SerializeField] Color redColor = new(1f, 0.35f, 0.35f);
        [SerializeField] Color blueColor = new(0.45f, 0.6f, 1f);

        CanvasGroup group;
        TextMeshProUGUI heading;
        TextMeshProUGUI detail;
        DoorColor chosen;
        Coroutine showing;

        void Awake() => CreateUi();

        void OnEnable()
        {
            stages.DoorChosen += OnDoorChosen;
            stages.Escaped += OnEscaped;
            runner.Started += OnEventStarted;
            runner.Completed += OnEventCompleted;
        }

        void OnDisable()
        {
            stages.DoorChosen -= OnDoorChosen;
            stages.Escaped -= OnEscaped;
            runner.Started -= OnEventStarted;
            runner.Completed -= OnEventCompleted;
        }

        void OnDoorChosen(DoorColor color, RoomEvent _) => chosen = color;

        void OnEventStarted(RoomEvent roomEvent)
        {
            string door = chosen == DoorColor.Red ? "빨간 문" : "파란 문";
            var color = chosen == DoorColor.Red ? redColor : blueColor;
            Show($"{stages.StageNumber}단계  ·  {stages.CurrentStage.Title}",
                $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{door}</color>\n{roomEvent.GetDescription()}", 2.8f);
        }

        void OnEventCompleted(RoomEvent finished)
        {
            // 즉시 통과하는 방은 입장 카드가 이미 떠 있으므로 생존 이벤트만 알린다
            if (finished.Duration > 0f && !finished.EscapeOnClear) Show("생존", "다음 문이 열렸다", 1.8f);
        }

        void OnEscaped() => Show("탈출", "살아서 마지막 문을 통과했다", 4f);

        void Show(string title, string body, float duration)
        {
            if (showing != null) StopCoroutine(showing);
            heading.text = title;
            detail.text = body;
            showing = StartCoroutine(Play(duration));
        }

        IEnumerator Play(float duration)
        {
            const float fadeIn = 0.35f, fadeOut = 0.6f;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                group.alpha = Mathf.Min(1f, t / fadeIn, (duration - t) / fadeOut);
                // 살짝 위로 떠오르며 등장
                group.transform.localPosition = new Vector3(0f, Mathf.Lerp(-12f, 0f, Mathf.Clamp01(t / fadeIn)), 0f);
                yield return null;
            }
            group.alpha = 0f;
            showing = null;
        }

        void CreateUi()
        {
            var canvasGo = new GameObject("StageBanner", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var root = new GameObject("Card", typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(canvasGo.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.62f);
            rect.sizeDelta = new Vector2(1400, 400);
            group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            heading = Label(root.transform, "Heading", 72, new Vector2(0, 70), FontStyles.Bold);
            heading.characterSpacing = 8;
            detail = Label(root.transform, "Detail", 30, new Vector2(0, -60), FontStyles.Normal);
            detail.color = new Color(0.85f, 0.85f, 0.88f);
            detail.lineSpacing = 10;
        }

        static TextMeshProUGUI Label(Transform parent, string name, float size, Vector2 position, FontStyles style)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(1400, size * 5f);
            rect.anchoredPosition = position;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.outlineWidth = 0.15f;
            text.outlineColor = new Color32(0, 0, 0, 200);
            return text;
        }
    }
}
