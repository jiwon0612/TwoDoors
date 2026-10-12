using TMPro;
using TwoDoors.Characters;
using TwoDoors.Items;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TwoDoors
{
    public sealed class CharacterDemo : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private TMP_Text health;
        [SerializeField] private UnityEngine.UI.Image healthBar;
        [SerializeField] private UnityEngine.UI.Image staminaBar;
        [SerializeField] private TMP_Text ammo;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text targetLabel;
        [SerializeField] private UnityEngine.UI.Image hitMarker;
        [SerializeField] private UnityEngine.UI.Image damageOverlay;
        [SerializeField] private UnityEngine.UI.Image[] slots;
        private int previousHealth;
        private float damageUntil;
        private TMP_Text[] slotLabels;

        private void Start()
        {
            SetupHealthBar();
            SetupStaminaBar();
            RefreshHealthBar();
            previousHealth = player.CurrentHealth;
            slotLabels = new TMP_Text[slots.Length];
            for (int i = 0; i < slots.Length; i++) slotLabels[i] = slots[i].GetComponentInChildren<TMP_Text>();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame)
            {
                Time.timeScale = 1;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                return;
            }
            if (!player) return;
            if (player.CurrentHealth < previousHealth) damageUntil = Time.unscaledTime + 0.35f;
            previousHealth = player.CurrentHealth;
            RefreshHealthBar();
            ammo.text = player.IsReloading ? "RELOADING" : player.IsFirearm ? $"<size=13>{PlayerInventory.Label(player.EquippedWeapon)}</size>\n<color=#80c6d9>{player.Ammo}</color> / {player.ReserveAmmo}" : PlayerInventory.Label(player.EquippedWeapon);
            status.text = player.IsDead ? "YOU DIED\n<size=20>F5  Restart</size>" : player.IsPaused && !player.Inventory.IsOpen ? "PAUSED\n<size=20>ESC  Resume</size>" : "";
            for (int i = 0; i < slots.Length; i++)
                slots[i].color = i == player.SelectedSlot ? new Color(0.35f, 0.4f, 0.42f, 0.8f) : new Color(0, 0, 0, 0.55f);
            if (slotLabels.Length == 4)
            {
                slotLabels[1].text = player.Inventory.Count(ItemKind.Pistol) > 0 ? "2\nPISTOL" : "2\nEMPTY";
                slotLabels[2].text = $"3\nHEAL x{player.Medkits}";
                slotLabels[3].text = player.Inventory.Count(ItemKind.Flashlight) > 0 ? "4\nLIGHT" : "4\nEMPTY";
            }
            hitMarker.enabled = Time.time - player.LastHitTime < 0.12f;
            damageOverlay.color = new Color(0.6f, 0, 0, Mathf.Clamp01((damageUntil - Time.unscaledTime) / 0.35f) * 0.2f);
            targetLabel.text = "";
        }

        public void SetupHealthBar()
        {
            if (!health) return;
            health.text = "";
            health.enabled = false;
            if (healthBar) return;
            var root = new GameObject("PlayerHealthBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            root.transform.SetParent(health.transform.parent, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(200, 18);
            var background = root.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.08f, 0.1f, 0.1f, 1);
            background.raycastTarget = false;
            var inner = new GameObject("Inset", typeof(RectTransform));
            inner.transform.SetParent(root.transform, false);
            var inset = (RectTransform)inner.transform;
            inset.anchorMin = Vector2.zero; inset.anchorMax = Vector2.one;
            inset.offsetMin = new Vector2(2, 2); inset.offsetMax = new Vector2(-2, -2);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            fill.transform.SetParent(inner.transform, false);
            healthBar = fill.GetComponent<UnityEngine.UI.Image>();
            healthBar.raycastTarget = false;
            healthBar.rectTransform.anchorMin = Vector2.zero;
            healthBar.rectTransform.anchorMax = Vector2.one;
            healthBar.rectTransform.offsetMin = healthBar.rectTransform.offsetMax = Vector2.zero;
            RefreshHealthBar();
        }

        void RefreshHealthBar()
        {
            if (staminaBar && player)
            {
                staminaBar.rectTransform.anchorMax = new Vector2(Application.isPlaying ? player.Stamina : 1f, 1);
                staminaBar.color = player.Stamina < 0.2f ? new Color(1f, 0.65f, 0.15f) : new Color(0.2f, 0.65f, 1f);
            }
            if (!healthBar || !player) return;
            float ratio = Application.isPlaying ? Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth) : 1f;
            healthBar.rectTransform.anchorMax = new Vector2(ratio, 1);
            healthBar.color = Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(0.2f, 0.85f, 0.55f), ratio);
        }

        public void SetupStaminaBar()
        {
            SetupHealthBar();
            if (staminaBar || !healthBar) return;
            var healthRoot = healthBar.transform.parent.parent;
            var copy = Instantiate(healthRoot.gameObject, healthRoot.parent);
            copy.name = "PlayerStaminaBar";
            var rect = (RectTransform)copy.transform;
            rect.anchoredPosition = new Vector2(0, -14);
            rect.sizeDelta = new Vector2(200, 10);
            ((RectTransform)healthRoot).anchoredPosition = new Vector2(0, 4);
            staminaBar = copy.transform.Find("Inset/Fill").GetComponent<UnityEngine.UI.Image>();
            RefreshHealthBar();
        }
    }
}
