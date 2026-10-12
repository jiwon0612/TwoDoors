using UnityEngine;
using UnityEngine.Events;

namespace TwoDoors.Events
{
    /// <summary>플레이어가 트리거에 닿으면 눌리는 버튼. Collider(isTrigger) 필요.</summary>
    [RequireComponent(typeof(Collider))]
    public class RoomButton : MonoBehaviour
    {
        [Tooltip("눌리면 색이 바뀔 렌더러 (선택).")]
        [SerializeField] Renderer indicator;
        [SerializeField] Color pressedColor = Color.green;
        [SerializeField] UnityEvent onPressed;

        Color? idleColor;

        public bool IsPressed { get; private set; }

        public void SetActive(bool active)
        {
            IsPressed = false;
            if (indicator && idleColor.HasValue) indicator.material.color = idleColor.Value;
            gameObject.SetActive(active);
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsPressed || !other.CompareTag("Player")) return;
            IsPressed = true;
            if (indicator)
            {
                idleColor ??= indicator.material.color;
                indicator.material.color = pressedColor;
            }
            onPressed.Invoke();
        }
    }
}
