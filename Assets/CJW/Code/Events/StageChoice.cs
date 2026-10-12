using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>한 단계의 빨간 문 / 파란 문 선택지 쌍.</summary>
    [CreateAssetMenu(menuName = "TwoDoors/Stage Choice", fileName = "StageChoice")]
    public class StageChoice : ScriptableObject
    {
        [SerializeField] string title;
        [SerializeField] RoomEvent redDoor;
        [SerializeField] RoomEvent blueDoor;

        public string Title => title;
        public RoomEvent RedDoor => redDoor;
        public RoomEvent BlueDoor => blueDoor;
    }
}
