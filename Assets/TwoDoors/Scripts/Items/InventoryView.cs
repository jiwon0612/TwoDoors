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
            if (panel) panel.SetActive(false);
        }

        private void Update()
        {
            if (!player || !player.Inventory) return;
            var bag = player.Inventory;
            if (panel) panel.SetActive(false);
            prompt.text = !player.IsPaused && bag.Nearby ? bag.Nearby.Prompt : "";
            message.text = !bag.IsOpen && Time.unscaledTime < bag.MessageUntil ? bag.Message : "";
        }
    }
}
