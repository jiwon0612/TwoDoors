using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TwoDoors.Stages
{
    /// <summary>
    /// 빨간 문 / 파란 문. 열려 있을 때 플레이어가 트리거에 들어오면 선택이 확정된다.
    /// 문틀 안쪽에 isTrigger Collider를 두고, 잠금용 막는 오브젝트는 blocker에 연결한다.
    /// 여닫는 애니메이션·효과음은 onOpened / onClosed에 연결.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StageDoor : MonoBehaviour
    {
        [SerializeField] DoorColor color;
        [SerializeField] StageManager manager;
        [Tooltip("문 위에 조건을 표시할 텍스트 (선택).")]
        [SerializeField] TMP_Text label;
        [Tooltip("잠겨 있을 때 통로를 막는 오브젝트 (선택).")]
        [SerializeField] GameObject blocker;
        [SerializeField] UnityEvent onOpened;
        [SerializeField] UnityEvent onClosed;

        public DoorColor Color => color;
        public bool IsOpen { get; private set; }

        public void Open(string description)
        {
            IsOpen = true;
            if (label) label.text = description;
            if (blocker) blocker.SetActive(false);
            onOpened.Invoke();
        }

        public void Close()
        {
            IsOpen = false;
            if (label) label.text = "";
            if (blocker) blocker.SetActive(true);
            onClosed.Invoke();
        }

        void OnTriggerEnter(Collider other)
        {
            if (IsOpen && other.CompareTag("Player")) manager.ChooseDoor(color);
        }
    }
}
