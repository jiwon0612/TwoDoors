using UnityEngine;

namespace TwoDoors.Events
{
    /// <summary>플레이어가 안에 있는지만 추적. 회복량은 HealZoneEvent가 결정한다. Collider(isTrigger) 필요.</summary>
    [RequireComponent(typeof(Collider))]
    public class HealZone : MonoBehaviour
    {
        public bool PlayerInside { get; private set; }

        void OnDisable() => PlayerInside = false;

        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) PlayerInside = true;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) PlayerInside = false;
        }
    }
}
