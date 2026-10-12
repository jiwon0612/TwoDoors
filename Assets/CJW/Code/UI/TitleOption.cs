using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TwoDoors.UI
{
    /// <summary>시작 화면의 문 선택지 하나. 선택되면 밑줄이 펼쳐지고 글자가 밝아진다.</summary>
    public class TitleOption : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_Text caption;
        [SerializeField] Image underline;
        [SerializeField] Color accent = Color.red;

        public event Action Hovered;
        public event Action Clicked;

        bool selected;
        float amount;

        public void SetSelected(bool value) => selected = value;

        void Update()
        {
            amount = Mathf.MoveTowards(amount, selected ? 1f : 0f, Time.unscaledDeltaTime * 5f);
            float e = amount * amount * (3f - 2f * amount);

            label.color = Color.Lerp(new Color(0.55f, 0.55f, 0.58f, 0.75f), Color.Lerp(accent, Color.white, 0.35f), e);
            label.characterSpacing = Mathf.Lerp(4f, 12f, e);
            caption.alpha = Mathf.Lerp(0.25f, 0.85f, e);
            underline.color = accent;
            underline.rectTransform.localScale = new Vector3(e, 1f, 1f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.06f, e);
        }

        public void OnPointerEnter(PointerEventData eventData) => Hovered?.Invoke();
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();
    }
}
