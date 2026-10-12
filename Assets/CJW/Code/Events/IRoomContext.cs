using TwoDoors.Characters;

namespace TwoDoors.Events
{
    /// <summary>
    /// 이벤트가 방/플레이어에 영향을 주는 통로.
    /// 이벤트는 이 인터페이스만 알기 때문에 좀비·조명·UI 구현이 바뀌어도 이벤트 코드는 그대로 둘 수 있다.
    /// </summary>
    public interface IRoomContext
    {
        void ChangeHealth(float amount);
        void SetLights(bool on);
        /// <param name="prefab">null이면 방의 기본 좀비 프리팹 사용.</param>
        void SpawnZombies(EnemyCharacter prefab, int count);
        void ActivateButtons(int count);
        int PressedButtonCount { get; }
        void SetHealZoneActive(bool active);
        bool IsPlayerInHealZone { get; }
        /// <summary>조명 복구, 좀비 제거, 버튼·회복 구역 비활성화.</summary>
        void ResetRoom();
    }
}
