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

        public StageChoice CreateForRun(int clearedRooms, bool swapColors)
        {
            var copy = CreateInstance<StageChoice>();
            copy.hideFlags = HideFlags.DontSave;
            copy.title = title;
            copy.redDoor = (swapColors ? blueDoor : redDoor).CreateForDifficulty(clearedRooms);
            copy.blueDoor = (swapColors ? redDoor : blueDoor).CreateForDifficulty(clearedRooms);
            return copy;
        }
    }
}
