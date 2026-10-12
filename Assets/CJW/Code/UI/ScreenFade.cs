using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TwoDoors.UI
{
    /// <summary>화면 전체 암전. 실행 시 최상단 오버레이 캔버스를 스스로 만든다.</summary>
    public class ScreenFade : MonoBehaviour
    {
        Image overlay;

        void Awake()
        {
            var canvasGo = new GameObject("ScreenFade", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            var imageGo = new GameObject("Overlay", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            overlay = imageGo.GetComponent<Image>();
            overlay.raycastTarget = false;
            overlay.color = new Color(0f, 0f, 0f, 0f);
        }

        public IEnumerator FadeTo(float alpha, float duration)
        {
            float start = overlay.color.a;
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                SetAlpha(Mathf.Lerp(start, alpha, t / duration));
                yield return null;
            }
            SetAlpha(alpha);
        }

        void SetAlpha(float alpha) => overlay.color = new Color(0f, 0f, 0f, alpha);
    }
}
