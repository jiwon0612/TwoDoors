using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace TwoDoors.Stages
{
    /// <summary>
    /// 빨간 문 / 파란 문.
    /// 선택 가능(IsOpen) 상태에서 플레이어가 가까이 오면 문짝이 경첩을 축으로 열리고,
    /// 문 뒤 통로의 isTrigger Collider에 들어오면 선택이 확정된다.
    /// 잠긴 동안에는 문짝이 닫힌 채 어둡게 표시된다.
    /// 효과음 등은 onOpened(잠금 해제) / onClosed(잠김)에 연결.
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

        [Header("자동 개폐")]
        [Tooltip("경첩 위치의 Transform. 문짝은 이 아래 자식으로 둔다 (선택).")]
        [SerializeField] Transform hinge;
        [Tooltip("열렸을 때 경첩 Y 회전. 음수면 문 뒤쪽으로 열린다.")]
        [SerializeField] float openAngle = -90f;
        [SerializeField] float openDistance = 3.5f;
        [SerializeField] float swingSpeed = 3f;
        [Tooltip("잠김 표시용으로 발광을 줄일 문짝 렌더러 (선택).")]
        [SerializeField] Renderer leaf;
        [SerializeField, Range(0f, 1f)] float lockedGlow = 0.2f;

        [SerializeField] UnityEvent onOpened;
        [SerializeField] UnityEvent onClosed;

        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        Transform player;
        Quaternion closedRotation;
        float openAmount;
        float glow = 1f;
        Color leafEmission;
        MaterialPropertyBlock block;

        public DoorColor Color => color;
        /// <summary>선택 가능 여부 (잠금 해제). 문짝이 실제로 열려 있는지와는 별개.</summary>
        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (hinge) closedRotation = hinge.localRotation;
            if (leaf)
            {
                block = new MaterialPropertyBlock();
                leafEmission = leaf.sharedMaterial.GetColor(EmissionColor);
            }
        }

        void Start()
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged) player = tagged.transform;
        }

        void Update()
        {
            bool near = IsOpen && player && (player.position - transform.position).sqrMagnitude < openDistance * openDistance;
            openAmount = Mathf.MoveTowards(openAmount, near ? 1f : 0f, Time.deltaTime * swingSpeed);
            if (hinge)
            {
                float eased = openAmount * openAmount * (3f - 2f * openAmount);
                hinge.localRotation = closedRotation * Quaternion.Euler(0f, openAngle * eased, 0f);
            }

            glow = Mathf.MoveTowards(glow, IsOpen ? 1f : lockedGlow, Time.deltaTime * 2f);
            if (leaf)
            {
                block.SetColor(EmissionColor, leafEmission * glow);
                leaf.SetPropertyBlock(block);
            }
        }

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
