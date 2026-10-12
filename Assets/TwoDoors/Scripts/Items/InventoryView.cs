using TMPro;
using TwoDoors.Characters;
using UnityEngine;
using UnityEngine.UI;

namespace TwoDoors.Items
{
    public sealed class InventoryView : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text prompt;
        [SerializeField] private TMP_Text message;
        [SerializeField] private TMP_Text[] labels;
        [SerializeField] private UnityEngine.UI.Button[] buttons;
        [SerializeField] private UnityEngine.UI.Button closeButton;

        private void Start()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                var kind = (ItemKind)i;
                buttons[i].onClick.AddListener(() => player.Inventory.Use(kind));
            }
            closeButton.onClick.AddListener(() => player.Inventory.Close());
        }

        private void Update()
        {
            if (!player || !player.Inventory) return;
            var bag = player.Inventory;
            panel.SetActive(bag.IsOpen && !player.IsDead);
            prompt.text = !player.IsPaused && bag.Nearby ? bag.Nearby.Prompt : "";
            message.text = Time.unscaledTime < bag.MessageUntil ? bag.Message : "";
            for (int i = 0; i < labels.Length; i++)
            {
                var kind = (ItemKind)i;
                int count = bag.Count(kind);
                string action = kind == ItemKind.Ammo ? "RELOAD" : kind == ItemKind.Medkit ? "USE (+35 HP)" : kind == ItemKind.Flashlight ? "TOGGLE" : "EQUIP";
                labels[i].text = $"{PlayerInventory.Label(kind)}\n<size=22>{count} / {PlayerInventory.Capacity(kind)}</size>\n<size=18>{(count > 0 ? action : "NOT OWNED")}</size>";
                buttons[i].interactable = count > 0 && !(kind == ItemKind.Medkit && player.CurrentHealth == player.MaxHealth);
            }
        }
    }
}
