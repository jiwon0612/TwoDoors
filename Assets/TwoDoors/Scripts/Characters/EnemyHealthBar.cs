using UnityEngine;

namespace TwoDoors.Characters
{
    /// <summary>Camera-facing health bar, visible above living enemies without aiming at them.</summary>
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        EnemyCharacter owner;
        Canvas canvas;
        RectTransform fill;
        Camera view;

        void Awake()
        {
            owner = GetComponent<EnemyCharacter>();
            var root = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(transform, false);
            var capsule = GetComponent<CapsuleCollider>();
            root.transform.localPosition = Vector3.up * (capsule.center.y + capsule.height * 0.5f + 0.25f);
            root.transform.localScale = Vector3.one * 0.008f;
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)root.transform).sizeDelta = new Vector2(150, 14);
            var background = root.AddComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
            background.raycastTarget = false;
            var front = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            front.transform.SetParent(root.transform, false);
            fill = (RectTransform)front.transform;
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = new Vector2(2, 2); fill.offsetMax = new Vector2(-2, -2);
            front.GetComponent<UnityEngine.UI.Image>().color = new Color(0.9f, 0.15f, 0.12f);
            front.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            owner.HealthChanged += Refresh;
            Refresh(owner.CurrentHealth, owner.MaxHealth);
        }

        void Refresh(int current, int maximum)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01((float)current / maximum), 1);
            canvas.gameObject.SetActive(current > 0);
        }

        void LateUpdate()
        {
            if (!view) view = Camera.main;
            if (view) canvas.transform.rotation = view.transform.rotation;
        }

        void OnDestroy()
        {
            if (owner) owner.HealthChanged -= Refresh;
        }
    }
}
