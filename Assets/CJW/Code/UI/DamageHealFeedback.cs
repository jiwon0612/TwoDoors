using TwoDoors.Characters;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TwoDoors.UI
{
    /// <summary>
    /// 피해·회복 화면 피드백. 피해 = 붉은 가장자리 + 카메라 흔들림, 회복 = 초록 가장자리.
    /// 플레이어 코드(김우진)는 수정하지 않고 HealthChanged 이벤트와 카메라 Transform만 사용한다.
    /// 플레이어 카메라의 URP 후처리도 실행 중에 켠다.
    /// </summary>
    public class DamageHealFeedback : MonoBehaviour
    {
        [SerializeField] Color damageColor = new(0.75f, 0.02f, 0.02f, 1f);
        [SerializeField] Color healColor = new(0.15f, 0.85f, 0.35f, 1f);
        [SerializeField] float shakeStrength = 0.006f;

        PlayerCharacter player;
        Transform cameraTransform;
        Image vignette;
        int previousHealth;
        float damageGlow, healGlow, shake;
        Vector3 shakeOffset;

        void Start()
        {
            player = FindFirstObjectByType<PlayerCharacter>();
            if (!player) { enabled = false; return; }

            previousHealth = player.CurrentHealth;
            player.HealthChanged += OnHealthChanged;
            if (player.ViewCamera)
            {
                cameraTransform = player.ViewCamera.transform;
                player.ViewCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
            vignette = CreateVignette();
        }

        void OnDestroy()
        {
            if (player) player.HealthChanged -= OnHealthChanged;
        }

        void OnHealthChanged(int current, int max)
        {
            int delta = current - previousHealth;
            previousHealth = current;
            if (delta < 0)
            {
                damageGlow = Mathf.Clamp01(damageGlow + 0.45f + -delta / 30f);
                shake = Mathf.Min(1f, shake + -delta / 15f);
            }
            else if (delta > 0)
            {
                // 회복 구역처럼 조금씩 자주 오르는 회복도 보이도록 최소 세기를 둔다
                healGlow = Mathf.Clamp01(Mathf.Max(healGlow, 0.3f) + delta / 30f);
            }
        }

        void LateUpdate()
        {
            damageGlow = Mathf.MoveTowards(damageGlow, 0f, Time.deltaTime * 1.4f);
            healGlow = Mathf.MoveTowards(healGlow, 0f, Time.deltaTime * 0.9f);
            // 체력이 낮으면 붉은 가장자리가 은은하게 남는다
            float lowHealth = player.IsDead ? 0f : Mathf.InverseLerp(35f, 0f, player.CurrentHealth) * 0.35f;

            float red = Mathf.Max(damageGlow, lowHealth * (0.75f + 0.25f * Mathf.Sin(Time.time * 4f)));
            vignette.color = red >= healGlow
                ? new Color(damageColor.r, damageColor.g, damageColor.b, red * 0.85f)
                : new Color(healColor.r, healColor.g, healColor.b, healGlow * 0.6f);

            if (!cameraTransform) return;
            // 플레이어 코드는 카메라 localPosition을 건드리지 않으므로 오프셋만 더했다 뺀다
            cameraTransform.localPosition -= shakeOffset;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 3f);
            shakeOffset = Random.insideUnitSphere * (shake * shake * shakeStrength * 10f);
            cameraTransform.localPosition += shakeOffset;
        }

        Image CreateVignette()
        {
            var canvasGo = new GameObject("DamageHealVignette", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var imageGo = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvasGo.transform, false);
            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            var image = imageGo.GetComponent<Image>();
            image.sprite = RadialSprite();
            image.raycastTarget = false;
            image.color = Color.clear;
            return image;
        }

        // 가운데는 투명하고 가장자리로 갈수록 진해지는 원형 그라데이션
        static Sprite RadialSprite()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, Mathf.Sqrt(dx * dx + dy * dy)));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
