using UnityEngine;
using UnityEngine.Events;

namespace TwoDoors.Events
{
    /// <summary>플레이어가 트리거에 닿으면 눌리는 버튼. Collider(isTrigger) 필요.</summary>
    [RequireComponent(typeof(Collider))]
    public class RoomButton : MonoBehaviour
    {
        [SerializeField] UnityEvent onPressed;

        public bool IsPressed { get; private set; }

        public void SetActive(bool active)
        {
            IsPressed = false;
            gameObject.SetActive(active);
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsPressed || !other.CompareTag("Player")) return;
            IsPressed = true;
            onPressed.Invoke();
        }
    }
}
