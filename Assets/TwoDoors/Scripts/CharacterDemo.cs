using TMPro;
using TwoDoors.Characters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TwoDoors
{
    public sealed class CharacterDemo : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter player;
        [SerializeField] private TMP_Text health;
        [SerializeField] private TMP_Text ammo;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text targetLabel;
        [SerializeField] private UnityEngine.UI.Image hitMarker;
        [SerializeField] private UnityEngine.UI.Image damageOverlay;
        [SerializeField] private UnityEngine.UI.Image[] slots;
        private int previousHealth;
        private float damageUntil;

        private void Start() => previousHealth = player.CurrentHealth;

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
            health.text = player.CurrentHealth.ToString();
            ammo.text = player.IsReloading ? "RELOADING" : $"<color=#80c6d9>{player.Ammo}</color> / {player.ReserveAmmo}";
            status.text = player.IsDead ? "YOU DIED\n<size=20>F5  Restart</size>" : player.IsPaused ? "PAUSED\n<size=20>ESC  Resume</size>" : "";
            for (int i = 0; i < slots.Length; i++)
                slots[i].color = i == player.SelectedSlot ? new Color(0.35f, 0.4f, 0.42f, 0.8f) : new Color(0, 0, 0, 0.55f);
            hitMarker.enabled = Time.time - player.LastHitTime < 0.12f;
            damageOverlay.color = new Color(0.6f, 0, 0, Mathf.Clamp01((damageUntil - Time.unscaledTime) / 0.35f) * 0.2f);
            targetLabel.text = "";
            if (!player.IsDead && Physics.Raycast(player.ViewCamera.transform.position, player.ViewCamera.transform.forward,
                out RaycastHit hit, 25, ~0, QueryTriggerInteraction.Ignore))
            {
                var enemy = hit.collider.GetComponentInParent<EnemyCharacter>();
                if (enemy) targetLabel.text = $"{enemy.gameObject.name}   {enemy.CurrentHealth} / {enemy.MaxHealth}";
            }
        }
    }
}
