using TwoDoors.Characters;
using UnityEngine;

namespace TwoDoors.Items
{
    [RequireComponent(typeof(Collider))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemKind kind;
        [SerializeField, Min(1)] private int amount = 1;
        private bool collected;
        private TMPro.TMP_Text label;
        private Camera viewCamera;
        public ItemKind Kind => kind;
        public int Amount => amount;
        public string Prompt => $"[E]  {PlayerInventory.Label(kind)}  x{amount}";

        private void Awake()
        {
            label = GetComponentInChildren<TMPro.TMP_Text>();
        }

        private void LateUpdate()
        {
            if (!viewCamera) viewCamera = Camera.main;
            if (!label || !viewCamera) return;
            label.transform.rotation = viewCamera.transform.rotation;
            label.text = $"{PlayerInventory.Label(kind)} x{amount}";
        }

        public bool TryCollect(PlayerCharacter player)
        {
            if (collected || !player || player.IsDead || player.IsPaused || !player.ViewCamera) return false;
            var camera = player.ViewCamera.transform;
            // Revalidate range and line of sight at collection, including calls from other scripts.
            if (!Physics.Raycast(camera.position, camera.forward, out var hit, 2.8f, ~0, QueryTriggerInteraction.Ignore)
                || hit.collider.GetComponentInParent<ItemPickup>() != this) return false;
            int added = player.Inventory.Add(kind, amount);
            if (added == 0) { player.Inventory.Notify("Inventory full / already owned"); return false; }
            amount -= added;
            if (kind == ItemKind.Pistol) player.OnPistolCollected();
            player.Inventory.Notify($"Picked up {PlayerInventory.Label(kind)} x{added}");
            if (amount == 0)
            {
                collected = true;
                gameObject.SetActive(false);
                Destroy(gameObject);
            }
            return true;
        }
    }
}
